using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins.GUI;

/// <summary>
/// Form for finding Generation 9 Tera Raid seeds that match specific criteria.
/// </summary>
public partial class Gen9SeedFinderForm : Form
{
    private readonly ISaveFileProvider _saveFileEditor;
    private readonly IPKMView _pkmEditor;
    private CancellationTokenSource? _searchCts;
    private readonly List<SeedResult> _results = new();
    private readonly Random _random = new();

    private const int SearchProgressInterval = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="Gen9SeedFinderForm"/> class.
    /// </summary>
    /// <param name="saveFileEditor">Save file provider for trainer information.</param>
    /// <param name="pkmEditor">PKM editor for displaying results.</param>
    public Gen9SeedFinderForm(ISaveFileProvider saveFileEditor, IPKMView pkmEditor)
    {
        _saveFileEditor = saveFileEditor ?? throw new ArgumentNullException(nameof(saveFileEditor));
        _pkmEditor = pkmEditor ?? throw new ArgumentNullException(nameof(pkmEditor));
        InitializeComponent();
        LoadSpeciesList();

        FormClosing += (s, e) => _searchCts?.Cancel();
        FormClosed += (s, e) => _searchCts?.Dispose();
    }

    /// <summary>
    /// Loads the species list with only species present in Scarlet/Violet.
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

        speciesCombo.DisplayMember = nameof(ComboItem.Text);
        speciesCombo.ValueMember = nameof(ComboItem.Value);
        speciesCombo.DataSource = species;
    }

    private void SpeciesCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (speciesCombo.SelectedValue is not int species)
            return;

        UpdateFormList(species);
        UpdateEncounterList(species);
    }

    /// <summary>
    /// Updates the form list for the selected species.
    /// </summary>
    /// <param name="species">Species ID.</param>
    private void UpdateFormList(int species)
    {
        var forms = FormConverter.GetFormList(
            (ushort)species,
            GameInfo.Strings.types,
            GameInfo.Strings.forms,
            GameInfo.GenderSymbolASCII,
            EntityContext.Gen9
        );

        formCombo.DisplayMember = nameof(ComboItem.Text);
        formCombo.ValueMember = nameof(ComboItem.Value);
        formCombo.DataSource = forms.Select((f, i) => new ComboItem(f, i)).ToList();
    }

    /// <summary>
    /// Updates the encounter list for the selected species.
    /// </summary>
    /// <param name="species">Species ID.</param>
    private void UpdateEncounterList(int species)
    {
        var encounters = new List<ITeraRaid9>();

        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraBase, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraDLC1, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.TeraDLC2, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.Dist, species));
        encounters.AddRange(GetEncountersForSpecies(Encounters9.Might, species));

        var items = encounters.Select(e => new EncounterItem(e)).ToList();
        items.Insert(0, new EncounterItem(null));

        encounterCombo.DisplayMember = nameof(EncounterItem.Text);
        encounterCombo.ValueMember = nameof(EncounterItem.Value);
        encounterCombo.DataSource = items;
    }

    /// <summary>
    /// Gets encounters for a specific species from an encounter array.
    /// </summary>
    /// <typeparam name="T">Encounter type implementing ITeraRaid9.</typeparam>
    /// <param name="encounters">Array of encounters to filter.</param>
    /// <param name="species">Species ID to filter by.</param>
    /// <returns>List of encounters matching the species.</returns>
    private static List<T> GetEncountersForSpecies<T>(T[] encounters, int species) where T : ITeraRaid9
    {
        return encounters.Where(e => e.Species == species).ToList();
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (_searchCts != null)
        {
            await CancelSearchAsync();
            return;
        }

        if (speciesCombo.SelectedValue is not int species)
        {
            WinFormsUtil.Alert("Please select a species!");
            return;
        }

        await StartSearchAsync(species);
    }

    /// <summary>
    /// Cancels the current search operation.
    /// </summary>
    private async Task CancelSearchAsync()
    {
        _searchCts?.Cancel();
        await Task.Delay(100);
    }

    /// <summary>
    /// Starts the seed search operation.
    /// </summary>
    /// <param name="species">Species ID to search for.</param>
    private async Task StartSearchAsync(int species)
    {
        var form = (byte)(formCombo.SelectedValue as int? ?? 0);
        var criteria = GetCriteria();
        var selectedEncounter = encounterCombo.SelectedValue as ITeraRaid9;

        PrepareSearch();

        _searchCts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => SearchSeeds(species, form, criteria, selectedEncounter, _searchCts.Token));
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Search cancelled";
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Search failed: {ex.Message}");
            statusLabel.Text = "Search failed";
        }
        finally
        {
            CleanupSearch();
        }
    }

    /// <summary>
    /// Prepares the UI for search operation.
    /// </summary>
    private void PrepareSearch()
    {
        _results.Clear();
        resultsGrid.Rows.Clear();
        searchButton.Text = "Stop";
        progressBar.Visible = true;
        progressBar.Value = 0;
        statusLabel.Text = "Searching...";
    }

    /// <summary>
    /// Cleans up after search operation.
    /// </summary>
    private void CleanupSearch()
    {
        searchButton.Text = "Search";
        progressBar.Visible = false;
        _searchCts?.Dispose();
        _searchCts = null;
    }

    /// <summary>
    /// Gets the search criteria from the form controls.
    /// </summary>
    /// <returns>Encounter criteria based on user selections.</returns>
    private EncounterCriteria GetCriteria()
    {
        var criteria = new EncounterCriteria
        {
            Gender = (Gender)genderCombo.SelectedIndex,
            Ability = GetAbilityPermission(),
            Nature = natureCombo.SelectedIndex == 0 ? Nature.Random : (Nature)(natureCombo.SelectedIndex - 1),
            Shiny = (Shiny)shinyCombo.SelectedIndex,
        };

        if (ivHpMin.Value > 0) criteria = criteria with { IV_HP = (sbyte)ivHpMin.Value };
        if (ivAtkMin.Value > 0) criteria = criteria with { IV_ATK = (sbyte)ivAtkMin.Value };
        if (ivDefMin.Value > 0) criteria = criteria with { IV_DEF = (sbyte)ivDefMin.Value };
        if (ivSpaMin.Value > 0) criteria = criteria with { IV_SPA = (sbyte)ivSpaMin.Value };
        if (ivSpdMin.Value > 0) criteria = criteria with { IV_SPD = (sbyte)ivSpdMin.Value };
        if (ivSpeMin.Value > 0) criteria = criteria with { IV_SPE = (sbyte)ivSpeMin.Value };

        return criteria;
    }

    /// <summary>
    /// Gets the ability permission based on combo box selection.
    /// </summary>
    /// <returns>Ability permission value.</returns>
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
    /// Searches for seeds that match the criteria.
    /// </summary>
    /// <param name="species">Species ID to search for.</param>
    /// <param name="form">Form number.</param>
    /// <param name="criteria">Search criteria.</param>
    /// <param name="specificEncounter">Specific encounter to use, or null for any.</param>
    /// <param name="token">Cancellation token.</param>
    private void SearchSeeds(int species, byte form, EncounterCriteria criteria, ITeraRaid9? specificEncounter, CancellationToken token)
    {
        var maxSeeds = (int)maxSeedsNum.Value;
        var seedsChecked = 0;

        while (_results.Count < maxSeeds && !token.IsCancellationRequested)
        {
            var seed = (uint)_random.Next();
            seedsChecked++;

            if (seedsChecked % SearchProgressInterval == 0)
            {
                UpdateProgress(seedsChecked, _results.Count, maxSeeds);
            }

            var encounter = specificEncounter ?? FindMatchingEncounter(seed, species, form);
            if (encounter == null)
                continue;

            // Skip encounters that can never be shiny if we're looking for shinies
            if (criteria.Shiny.IsShiny() && encounter.Shiny == Shiny.Never)
                continue;

            // Quick check if this seed can produce a shiny before full generation
            if (criteria.Shiny.IsShiny() && !CanSeedProduceShiny(seed, encounter))
                continue;

            var pk = GeneratePokemon(encounter, seed, criteria);
            if (pk == null || !MatchesCriteria(pk, criteria))
                continue;

            var result = new SeedResult
            {
                Seed = seed,
                Encounter = encounter,
                Pokemon = pk
            };

            _results.Add(result);
            AddResultToGrid(result);
        }

        UpdateFinalStatus(seedsChecked);
    }

    /// <summary>
    /// Checks if a seed can potentially produce a shiny Pokémon.
    /// </summary>
    /// <param name="seed">Seed to check.</param>
    /// <param name="encounter">Encounter to check against.</param>
    /// <returns>True if the seed can produce a shiny.</returns>
    private bool CanSeedProduceShiny(uint seed, ITeraRaid9 encounter)
    {
        if (encounter.Shiny == Shiny.Never)
            return false;

        if (encounter.Shiny == Shiny.Always)
            return true;

        var rollCount = GetRollCount(encounter.Stars);
        var rand = new Xoroshiro128Plus(seed);
        _ = rand.NextInt(uint.MaxValue); // EC
        var fakeTID = (uint)rand.NextInt();

        for (int i = 0; i < rollCount; i++)
        {
            var pid = (uint)rand.NextInt();
            if (ShinyUtil.GetShinyXor(pid, fakeTID) < 16)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of shiny rolls based on star rating.
    /// </summary>
    /// <param name="stars">Star rating of the raid.</param>
    /// <returns>Number of shiny rolls.</returns>
    private static byte GetRollCount(byte stars)
    {
        return stars switch
        {
            1 or 2 => 1,
            3 or 4 => 2,
            5 => 3,
            6 or 7 => 4,
            _ => 1
        };
    }

    /// <summary>
    /// Updates the progress during search.
    /// </summary>
    /// <param name="seedsChecked">Number of seeds checked.</param>
    /// <param name="foundCount">Number of matches found.</param>
    /// <param name="maxSeeds">Maximum seeds to find.</param>
    private void UpdateProgress(int seedsChecked, int foundCount, int maxSeeds)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateProgress(seedsChecked, foundCount, maxSeeds));
            return;
        }

        progressBar.Value = Math.Min((foundCount * 100) / maxSeeds, 100);
        statusLabel.Text = $"Checked {seedsChecked:N0} seeds, found {foundCount}";
    }

    /// <summary>
    /// Updates the final status after search completion.
    /// </summary>
    /// <param name="seedsChecked">Total number of seeds checked.</param>
    private void UpdateFinalStatus(int seedsChecked)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateFinalStatus(seedsChecked));
            return;
        }

        statusLabel.Text = $"Found {_results.Count} matches after checking {seedsChecked:N0} seeds";
        progressBar.Value = 100;
    }

    /// <summary>
    /// Checks if a Pokémon matches the search criteria.
    /// </summary>
    /// <param name="pk">Pokémon to check.</param>
    /// <param name="criteria">Criteria to match against.</param>
    /// <returns>True if the Pokémon matches all criteria.</returns>
    private static bool MatchesCriteria(PK9 pk, EncounterCriteria criteria)
    {
        bool matchesShiny = criteria.Shiny switch
        {
            Shiny.Never => !pk.IsShiny,
            Shiny.Always => pk.IsShiny,
            Shiny.AlwaysSquare => pk.IsShiny && pk.ShinyXor == 0,
            Shiny.AlwaysStar => pk.IsShiny && pk.ShinyXor != 0,
            _ => true
        };

        if (!matchesShiny)
            return false;

        if (criteria.Gender != Gender.Random && pk.Gender != (int)criteria.Gender)
            return false;

        if (criteria.Nature != Nature.Random && pk.Nature != criteria.Nature)
            return false;

        return CheckIVsCriteria(pk, criteria);
    }

    /// <summary>
    /// Checks if a Pokémon's IVs match the criteria.
    /// </summary>
    /// <param name="pk">Pokémon to check.</param>
    /// <param name="criteria">Criteria with IV requirements.</param>
    /// <returns>True if IVs match or exceed criteria.</returns>
    private static bool CheckIVsCriteria(PK9 pk, EncounterCriteria criteria)
    {
        return (criteria.IV_HP == -1 || pk.IV_HP >= criteria.IV_HP) &&
               (criteria.IV_ATK == -1 || pk.IV_ATK >= criteria.IV_ATK) &&
               (criteria.IV_DEF == -1 || pk.IV_DEF >= criteria.IV_DEF) &&
               (criteria.IV_SPA == -1 || pk.IV_SPA >= criteria.IV_SPA) &&
               (criteria.IV_SPD == -1 || pk.IV_SPD >= criteria.IV_SPD) &&
               (criteria.IV_SPE == -1 || pk.IV_SPE >= criteria.IV_SPE);
    }

    /// <summary>
    /// Finds a matching encounter for the given seed and species.
    /// </summary>
    /// <param name="seed">Seed to check.</param>
    /// <param name="species">Species ID.</param>
    /// <param name="form">Form number.</param>
    /// <returns>First matching encounter, or null if none found.</returns>
    private static ITeraRaid9? FindMatchingEncounter(uint seed, int species, byte form)
    {
        var allEncounters = new List<ITeraRaid9>();

        // Priority order matters
        allEncounters.AddRange(FilterEncounters(Encounters9.Dist, species, form));
        allEncounters.AddRange(FilterEncounters(Encounters9.Might, species, form));
        allEncounters.AddRange(FilterEncounters(Encounters9.TeraBase, species, form));
        allEncounters.AddRange(FilterEncounters(Encounters9.TeraDLC1, species, form));
        allEncounters.AddRange(FilterEncounters(Encounters9.TeraDLC2, species, form));

        return allEncounters.FirstOrDefault(e => e.CanBeEncountered(seed));
    }

    /// <summary>
    /// Filters encounters by species and form.
    /// </summary>
    /// <typeparam name="T">Encounter type.</typeparam>
    /// <param name="encounters">Encounters to filter.</param>
    /// <param name="species">Species ID.</param>
    /// <param name="form">Form number.</param>
    /// <returns>Filtered encounters.</returns>
    private static IEnumerable<T> FilterEncounters<T>(T[] encounters, int species, byte form) where T : ITeraRaid9
    {
        return encounters.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic));
    }

    /// <summary>
    /// Generates a Pokémon from an encounter and seed.
    /// </summary>
    /// <param name="encounter">Encounter template.</param>
    /// <param name="seed">Seed for RNG.</param>
    /// <param name="criteria">Generation criteria.</param>
    /// <returns>Generated Pokémon, or null if generation fails.</returns>
    private PK9? GeneratePokemon(ITeraRaid9 encounter, uint seed, EncounterCriteria criteria)
    {
        if (!encounter.CanBeEncountered(seed))
            return null;

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

        // Determine roll count based on star rating
        byte rollCount = GetRollCount(encounter.Stars);

        // Use criteria shiny if specified, otherwise use encounter shiny
        var shinyToUse = criteria.Shiny != Shiny.Random ? criteria.Shiny : encounter.Shiny;

        var param = new GenerateParam9(
            encounter.Species,
            pi.Gender,
            encounter.FlawlessIVCount,
            rollCount,
            0,
            0,
            SizeType9.RANDOM,
            0,
            encounter.Ability,
            shinyToUse,
            encounter is IFixedNature fn ? fn.Nature : Nature.Random,
            encounter is EncounterDist9 dist && dist.IVs.IsSpecified ? dist.IVs : default
        );

        if (!Encounter9RNG.GenerateData(pk, param, EncounterCriteria.Unrestricted, seed))
            return null;

        var teraType = Tera9RNG.GetTeraType(seed, encounter.TeraType, encounter.Species, encounter.Form);
        pk.TeraTypeOriginal = (MoveType)teraType;

        if (encounter is IMoveset ms && ms.Moves.HasMoves)
            pk.SetMoves(ms.Moves);

        if (encounter is EncounterMight9)
            pk.SetRibbonIndex(RibbonIndex.MarkMightiest, true);

        pk.ResetPartyStats();

        return pk;
    }

    /// <summary>
    /// Adds a result to the results grid.
    /// </summary>
    /// <param name="result">Result to add.</param>
    private void AddResultToGrid(SeedResult result)
    {
        if (InvokeRequired)
        {
            Invoke(() => AddResultToGrid(result));
            return;
        }

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
    }

    /// <summary>
    /// Gets the ability name for a Pokémon.
    /// </summary>
    /// <param name="pk">Pokémon to get ability name for.</param>
    /// <returns>Ability name string.</returns>
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
    /// Gets a formatted IV string.
    /// </summary>
    /// <param name="pk">Pokémon to get IVs from.</param>
    /// <returns>Formatted IV string.</returns>
    private static string GetIVString(PK9 pk)
    {
        return $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
    }

    private void ResultsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _results.Count)
            return;

        var result = _results[e.RowIndex];
        _pkmEditor.PopulateFields(result.Pokemon);

        WinFormsUtil.Alert($"Loaded {result.Pokemon.Nickname}!\nSeed: {result.Seed:X8}");
    }

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
            ExportResults(sfd.FileName);
            WinFormsUtil.Alert("Export successful!");
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports results to a CSV file.
    /// </summary>
    /// <param name="filename">File path to export to.</param>
    private void ExportResults(string filename)
    {
        using var writer = new StreamWriter(filename);
        writer.WriteLine("Seed,Stars,Shiny,Nature,Ability,IVs,TeraType");

        foreach (var result in _results)
        {
            writer.WriteLine(
                $"{result.Seed:X8}," +
                $"{result.Encounter.Stars}★," +
                $"{(result.Pokemon.IsShiny ? "Yes" : "No")}," +
                $"{result.Pokemon.Nature}," +
                $"{GetAbilityName(result.Pokemon)}," +
                $"{GetIVString(result.Pokemon)}," +
                $"{result.Pokemon.TeraTypeOriginal}"
            );
        }
    }

    /// <summary>
    /// Represents a seed search result.
    /// </summary>
    private sealed class SeedResult
    {
        /// <summary>
        /// The seed that generated this result.
        /// </summary>
        public required uint Seed { get; init; }

        /// <summary>
        /// The encounter template used.
        /// </summary>
        public required ITeraRaid9 Encounter { get; init; }

        /// <summary>
        /// The generated Pokémon.
        /// </summary>
        public required PK9 Pokemon { get; init; }
    }

    /// <summary>
    /// Wrapper for combo box items.
    /// </summary>
    private sealed class ComboItem(string text, int value)
    {
        public string Text { get; } = text;
        public int Value { get; } = value;
    }

    /// <summary>
    /// Wrapper for encounter combo box items.
    /// </summary>
    private sealed class EncounterItem(ITeraRaid9? encounter)
    {
        public string Text { get; } = encounter == null ? "Any Encounter" : $"{encounter.Stars}★ {GetEncounterType(encounter)}";
        public ITeraRaid9? Value { get; } = encounter;

        /// <summary>
        /// Gets a descriptive string for the encounter type.
        /// </summary>
        /// <param name="encounter">Encounter to describe.</param>
        /// <returns>Encounter type description.</returns>
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