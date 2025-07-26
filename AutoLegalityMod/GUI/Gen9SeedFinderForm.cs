using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins.GUI;

/// <summary>
/// Form for searching Generation 9 Tera Raid seeds that match specific criteria
/// </summary>
public partial class Gen9SeedFinderForm : Form
{
    private readonly ISaveFileProvider _saveFileEditor;
    private readonly IPKMView _pkmEditor;
    private CancellationTokenSource? _searchCts;
    private List<SeedResult> _results = new();

    /// <summary>
    /// Initializes a new instance of the Gen9SeedFinderForm
    /// </summary>
    /// <param name="saveFileEditor">Save file provider for trainer info</param>
    /// <param name="pkmEditor">PKM editor for loading results</param>
    public Gen9SeedFinderForm(ISaveFileProvider saveFileEditor, IPKMView pkmEditor)
    {
        _saveFileEditor = saveFileEditor;
        _pkmEditor = pkmEditor;
        InitializeComponent();
        LoadSpeciesList();
    }

    /// <summary>
    /// Loads the species list into the combo box
    /// </summary>
    private void LoadSpeciesList()
    {
        var species = new List<ComboItem>();
        var names = GameInfo.Strings.specieslist;

        for (int i = 1; i < names.Length; i++)
        {
            if (PersonalTable.SV.IsPresentInGame((ushort)i, 0))
                species.Add(new ComboItem(names[i], i));
        }

        speciesCombo.DisplayMember = "Text";
        speciesCombo.ValueMember = "Value";
        speciesCombo.DataSource = species;
    }

    /// <summary>
    /// Handles species selection change
    /// </summary>
    private void SpeciesCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (speciesCombo.SelectedValue is not int species)
            return;

        UpdateFormList(species);
        UpdateEncounterList(species);
    }

    /// <summary>
    /// Updates the form list for the selected species
    /// </summary>
    private void UpdateFormList(int species)
    {
        var pi = PersonalTable.SV[species];
        var forms = FormConverter.GetFormList((ushort)species, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);

        formCombo.DisplayMember = "Text";
        formCombo.ValueMember = "Value";
        formCombo.DataSource = forms.Select((f, i) => new ComboItem(f, i)).ToList();
    }

    /// <summary>
    /// Updates the encounter list for the selected species
    /// </summary>
    private void UpdateEncounterList(int species)
    {
        var encounters = new List<ITeraRaid9>();

        // Get all possible Tera Raid encounters for this species
        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraBase, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraDLC1, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraDLC2, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.Dist, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.Might, species));

        var items = encounters.Select(e => new EncounterItem(e)).ToList();
        items.Insert(0, new EncounterItem(null)); // Any encounter

        encounterCombo.DisplayMember = "Text";
        encounterCombo.ValueMember = "Value";
        encounterCombo.DataSource = items;
    }

    /// <summary>
    /// Filters encounters by species
    /// </summary>
    private static List<T> GetEncountersForSpecies<T>(T[] encounters, int species) where T : ITeraRaid9
    {
        return encounters.Where(e => e.Species == species).ToList();
    }

    /// <summary>
    /// Handles the search button click
    /// </summary>
    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (_searchCts != null)
        {
            _searchCts.Cancel();
            return;
        }

        if (speciesCombo.SelectedValue is not int species)
        {
            WinFormsUtil.Alert("Please select a species!");
            return;
        }

        var form = (byte)(formCombo.SelectedValue as int? ?? 0);
        var criteria = GetCriteria();
        var selectedEncounter = (encounterCombo.SelectedItem as EncounterItem)?.Encounter;

        _results.Clear();
        resultsGrid.Rows.Clear();

        searchButton.Text = "Stop";
        progressBar.Visible = true;
        statusLabel.Text = "Searching...";

        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => SearchSeeds(species, form, criteria, selectedEncounter, _searchCts.Token));
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Search cancelled";
        }
        finally
        {
            searchButton.Text = "Search";
            progressBar.Visible = false;
            _searchCts?.Dispose();
            _searchCts = null;
        }
    }

    /// <summary>
    /// Represents an IV range for searching
    /// </summary>
    private record struct IVRange(int Min, int Max);

    /// <summary>
    /// Gets the search criteria from the form controls
    /// </summary>
    private EncounterCriteria GetCriteria()
    {
        var criteria = new EncounterCriteria
        {
            Gender = (Gender)genderCombo.SelectedIndex,
            Ability = GetAbilityPermission(),
            Nature = natureCombo.SelectedIndex == 0 ? Nature.Random : (Nature)(natureCombo.SelectedIndex - 1),
            Shiny = (Shiny)shinyCombo.SelectedIndex,
        };

        return criteria;
    }

    /// <summary>
    /// Gets the IV ranges from the form controls
    /// </summary>
    private IVRange[] GetIVRanges()
    {
        return new[]
        {
            new IVRange((int)ivHpMin.Value, (int)ivHpMax.Value),
            new IVRange((int)ivAtkMin.Value, (int)ivAtkMax.Value),
            new IVRange((int)ivDefMin.Value, (int)ivDefMax.Value),
            new IVRange((int)ivSpaMin.Value, (int)ivSpaMax.Value),
            new IVRange((int)ivSpdMin.Value, (int)ivSpdMax.Value),
            new IVRange((int)ivSpeMin.Value, (int)ivSpeMax.Value),
        };
    }

    /// <summary>
    /// Gets the ability permission from the form control
    /// </summary>
    private AbilityPermission GetAbilityPermission()
    {
        return abilityCombo.SelectedIndex switch
        {
            0 => AbilityPermission.Any12H,
            1 => AbilityPermission.OnlyFirst,
            2 => AbilityPermission.OnlySecond,
            3 => AbilityPermission.OnlyHidden,
            4 => AbilityPermission.Any12,
            _ => AbilityPermission.Any12H
        };
    }

    /// <summary>
    /// Searches for seeds that match the criteria
    /// </summary>
    private void SearchSeeds(int species, byte form, EncounterCriteria criteria, ITeraRaid9? specificEncounter, CancellationToken token)
    {
        var rand = new Random();
        var maxSeeds = (int)maxSeedsNum.Value;
        var seedsChecked = 0;
        var results = new List<SeedResult>();
        var ivRanges = GetIVRanges();

        while (results.Count < maxSeeds && !token.IsCancellationRequested)
        {
            var seed = (uint)rand.Next();
            seedsChecked++;

            if (seedsChecked % 1000 == 0)
            {
                this.Invoke(() =>
                {
                    progressBar.Value = (results.Count * 100) / maxSeeds;
                    statusLabel.Text = $"Checked {seedsChecked:N0} seeds, found {results.Count}";
                });
            }

            // Check if this seed produces a matching encounter
            var encounter = specificEncounter ?? FindMatchingEncounter(seed, species, form);
            if (encounter == null)
                continue;

            // Generate a Pokemon from this seed
            var pk = GeneratePokemon(encounter, seed, criteria);
            if (pk == null)
                continue;

            // Check if the generated Pokemon matches our shiny criteria
            bool matchesShiny = criteria.Shiny switch
            {
                Shiny.Never => !pk.IsShiny,
                Shiny.Always => pk.IsShiny,
                Shiny.AlwaysSquare => pk.IsShiny && pk.ShinyXor == 0,
                Shiny.AlwaysStar => pk.IsShiny && pk.ShinyXor != 0,
                _ => true // Random accepts any
            };

            if (!matchesShiny)
                continue;

            // Check other criteria
            if (criteria.Gender != Gender.Random && pk.Gender != (int)criteria.Gender)
                continue;

            if (criteria.Nature != Nature.Random && pk.Nature != criteria.Nature)
                continue;

            if (!CheckIVRanges(pk, ivRanges))
                continue;

            // Add to results
            var result = new SeedResult
            {
                Seed = seed,
                Encounter = encounter,
                Pokemon = pk
            };

            results.Add(result);
            AddResultToGrid(result);
        }

        _results = results;

        this.Invoke(() =>
        {
            statusLabel.Text = $"Found {results.Count} matches after checking {seedsChecked:N0} seeds";
            progressBar.Value = 100;
        });
    }

    /// <summary>
    /// Checks if the Pokemon's IVs are within the specified ranges
    /// </summary>
    private static bool CheckIVRanges(PK9 pk, IVRange[] ranges)
    {
        return pk.IV_HP >= ranges[0].Min && pk.IV_HP <= ranges[0].Max &&
               pk.IV_ATK >= ranges[1].Min && pk.IV_ATK <= ranges[1].Max &&
               pk.IV_DEF >= ranges[2].Min && pk.IV_DEF <= ranges[2].Max &&
               pk.IV_SPA >= ranges[3].Min && pk.IV_SPA <= ranges[3].Max &&
               pk.IV_SPD >= ranges[4].Min && pk.IV_SPD <= ranges[4].Max &&
               pk.IV_SPE >= ranges[5].Min && pk.IV_SPE <= ranges[5].Max;
    }

    /// <summary>
    /// Finds a matching encounter for the given seed
    /// </summary>
    private static ITeraRaid9? FindMatchingEncounter(uint seed, int species, byte form)
    {
        // Check all encounter types and return the first valid one
        var allEncounters = new List<ITeraRaid9>();

        // Add encounters in priority order
        allEncounters.AddRange(Encounters9.Dist.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)));
        allEncounters.AddRange(Encounters9.Might.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)));
        allEncounters.AddRange(Encounters9.TeraBase.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)));
        allEncounters.AddRange(Encounters9.TeraDLC1.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)));
        allEncounters.AddRange(Encounters9.TeraDLC2.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic)));

        // Return the first encounter that can be encountered with this seed
        return allEncounters.FirstOrDefault(e => e.CanBeEncountered(seed));
    }

    /// <summary>
    /// Generates a Pokemon from the encounter and seed
    /// </summary>
    private PK9? GeneratePokemon(ITeraRaid9 encounter, uint seed, EncounterCriteria criteria)
    {
        // First, verify this encounter can actually be encountered with this seed
        if (!encounter.CanBeEncountered(seed))
            return null;

        // Get safe language
        int language = (int)Language.GetSafeLanguage(9, (LanguageID)_saveFileEditor.SAV.Language);

        var pk = new PK9
        {
            Species = encounter.Species,
            Form = encounter.Form < EncounterUtil.FormDynamic ? encounter.Form : (byte)0,
            CurrentLevel = encounter.LevelMin,
            MetLocation = Locations.TeraCavern9,
            MetLevel = encounter.LevelMin,
            MetDate = EncounterDate.GetDateSwitch(),
            Version = _saveFileEditor.SAV.Version,
            Ball = (byte)Ball.Poke,
            ID32 = _saveFileEditor.SAV.ID32,
            OriginalTrainerName = _saveFileEditor.SAV.OT,
            OriginalTrainerGender = _saveFileEditor.SAV.Gender,
            Language = language,
            ObedienceLevel = encounter.LevelMin,
        };

        var pi = PersonalTable.SV[encounter.Species, encounter.Form];
        pk.OriginalTrainerFriendship = pi.BaseFriendship;
        pk.Nickname = SpeciesName.GetSpeciesNameGeneration(encounter.Species, language, 9);

        // Create generation parameters using the encounter's natural settings
        var param = new GenerateParam9(
            encounter.Species,
            pi.Gender,
            encounter.FlawlessIVCount,
            1, // roll count - important for shiny calculation
            0, // height
            0, // weight
            SizeType9.RANDOM, // scale type
            0, // scale
            encounter.Ability,
            encounter.Shiny, // Use encounter's shiny setting - this is key!
            encounter is IFixedNature fn ? fn.Nature : Nature.Random,
            encounter is EncounterDist9 dist && dist.IVs.IsSpecified ? dist.IVs : default
        );

        // Generate using the exact seed
        if (!Encounter9RNG.GenerateData(pk, param, EncounterCriteria.Unrestricted, seed))
            return null;

        // Set Tera Type
        var teraType = Tera9RNG.GetTeraType(seed, encounter.TeraType, encounter.Species, encounter.Form);
        pk.TeraTypeOriginal = (MoveType)teraType;

        // Set moves if specified
        if (encounter is IMoveset ms && ms.Moves.HasMoves)
            pk.SetMoves(ms.Moves);

        // For 7-star raids, set the Mightiest Mark
        if (encounter is EncounterMight9)
        {
            pk.SetRibbonIndex(RibbonIndex.MarkMightiest, true);
        }

        pk.ResetPartyStats();

        return pk;
    }

    /// <summary>
    /// Adds a result to the data grid
    /// </summary>
    private void AddResultToGrid(SeedResult result)
    {
        this.Invoke(() =>
        {
            var row = resultsGrid.Rows.Add(
                $"{result.Seed:X8}",
                $"{result.Encounter.Stars}★",
                result.Pokemon.IsShiny ? "★" : "",
                result.Pokemon.Nature.ToString(),
                GetAbilityName(result.Pokemon),
                GetIVString(result.Pokemon),
                result.Pokemon.TeraTypeOriginal.ToString()
            );

            if (result.Pokemon.IsShiny)
                resultsGrid.Rows[row].DefaultCellStyle.BackColor = Color.LightYellow;
        });
    }

    /// <summary>
    /// Gets the ability name for the Pokemon
    /// </summary>
    private static string GetAbilityName(PK9 pk)
    {
        var abilities = PersonalTable.SV[pk.Species, pk.Form];
        return pk.AbilityNumber switch
        {
            1 => GameInfo.Strings.abilitylist[abilities.Ability1],
            2 => GameInfo.Strings.abilitylist[abilities.Ability2],
            4 => GameInfo.Strings.abilitylist[abilities.AbilityH],
            _ => "?"
        };
    }

    /// <summary>
    /// Gets the IV string representation
    /// </summary>
    private static string GetIVString(PK9 pk)
    {
        return $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
    }

    /// <summary>
    /// Handles double-clicking a result in the grid
    /// </summary>
    private void ResultsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _results.Count)
            return;

        var result = _results[e.RowIndex];
        _pkmEditor.PopulateFields(result.Pokemon);

        WinFormsUtil.Alert($"Loaded {result.Pokemon.Nickname}!\nSeed: {result.Seed:X8}");
    }

    /// <summary>
    /// Handles exporting results to CSV
    /// </summary>
    private void ExportButton_Click(object? sender, EventArgs e)
    {
        if (_results.Count == 0)
        {
            WinFormsUtil.Alert("No results to export!");
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = $"Gen9_Seeds_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var writer = new System.IO.StreamWriter(sfd.FileName);
            writer.WriteLine("Seed,Stars,Shiny,Nature,Ability,IVs,TeraType");

            foreach (var result in _results)
            {
                writer.WriteLine($"{result.Seed:X8},{result.Encounter.Stars}★,{(result.Pokemon.IsShiny ? "Yes" : "No")}," +
                               $"{result.Pokemon.Nature},{GetAbilityName(result.Pokemon)},{GetIVString(result.Pokemon)}," +
                               $"{result.Pokemon.TeraTypeOriginal}");
            }

            WinFormsUtil.Alert("Export successful!");
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Represents a seed search result
    /// </summary>
    private class SeedResult
    {
        public uint Seed { get; set; }
        public ITeraRaid9 Encounter { get; set; } = null!;
        public PK9 Pokemon { get; set; } = null!;
    }

    /// <summary>
    /// Combo box item for display
    /// </summary>
    private class ComboItem(string text, int value)
    {
        public string Text { get; } = text;
        public int Value { get; } = value;
    }

    /// <summary>
    /// Encounter item for display
    /// </summary>
    private class EncounterItem(ITeraRaid9? encounter)
    {
        public string Text { get; } = encounter == null ? "Any Encounter" : $"{encounter.Stars}★ {GetEncounterType(encounter)}";
        public ITeraRaid9? Encounter { get; } = encounter;

        /// <summary>
        /// Gets the encounter type display name
        /// </summary>
        private static string GetEncounterType(ITeraRaid9 encounter)
        {
            return encounter switch
            {
                EncounterTera9 => "Base",
                EncounterDist9 => "Event",
                EncounterMight9 => "7★ Mighty",
                _ => "Unknown"
            };
        }
    }
}