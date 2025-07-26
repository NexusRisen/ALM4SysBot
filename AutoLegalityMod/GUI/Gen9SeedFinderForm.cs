using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins.GUI;
/// <summary>
/// Gen 9 Seed Finder Plugin for PKHeX
/// </summary>
/// <author>hexbyt3</author>
/// <description>Tool for searching Generation 9 Tera Raid seeds that match specific criteria</description>

/// <summary>
/// Form for searching Generation 9 Tera Raid seeds that match specific criteria
/// </summary>
public partial class Gen9SeedFinderForm : Form
{
    private readonly ISaveFileProvider _saveFileEditor;
    private readonly IPKMView _pkmEditor;
    private CancellationTokenSource? _searchCts;
    private List<SeedResult> _results = [];

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
            ITeraRaid9? encounter;
            if (specificEncounter != null)
            {
                // If user selected a specific encounter, check if it's form-compatible
                if (!IsFormCompatible(specificEncounter, species, form))
                    continue;

                // Check if this seed can generate this encounter
                if (!specificEncounter.CanBeEncountered(seed))
                    continue;

                encounter = specificEncounter;
            }
            else
            {
                // Find any matching encounter for this species/form
                encounter = FindMatchingEncounter(seed, species, form);
                if (encounter == null)
                    continue;
            }

            // Generate a Pokemon from this seed
            var pk = GenerateRaidPokemon(encounter, seed, criteria);
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

            // Check ability if specified
            if (criteria.Ability != AbilityPermission.Any12H && !CheckAbilityCriteria(pk, criteria.Ability))
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
    /// Checks if the Pokemon matches the ability criteria
    /// </summary>
    private static bool CheckAbilityCriteria(PK9 pk, AbilityPermission criteria)
    {
        var pi = PersonalTable.SV[pk.Species, pk.Form];
        var abilityNumber = pk.AbilityNumber;

        return criteria switch
        {
            AbilityPermission.OnlyFirst => abilityNumber == 1 && pk.Ability == pi.Ability1,
            AbilityPermission.OnlySecond => abilityNumber == 2 && pk.Ability == pi.Ability2,
            AbilityPermission.OnlyHidden => abilityNumber == 4 && pk.Ability == pi.AbilityH,
            AbilityPermission.Any12 => abilityNumber <= 2,
            _ => true
        };
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
    /// Checks if an encounter is compatible with the desired form
    /// </summary>
    private static bool IsFormCompatible(ITeraRaid9 encounter, int species, byte form)
    {
        // Check if the encounter matches the species
        if (encounter.Species != species)
            return false;

        // Check if forms match or if the encounter has a random form
        if (encounter.Form == form)
            return true;

        // Check for random form encounters
        if (encounter is IEncounterFormRandom efr && efr.IsRandomUnspecificForm)
            return true;

        // Check if form can change between the encounter form and desired form
        return FormInfo.IsFormChangeable((ushort)species, encounter.Form, form, EntityContext.Gen9, EntityContext.Gen9);
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
    /// Generates a Pokemon from the encounter and seed using the exact raid generation method
    /// </summary>
    private PK9? GenerateRaidPokemon(ITeraRaid9 encounter, uint seed, EncounterCriteria criteria)
    {
        // First, verify this encounter can actually be encountered with this seed
        if (!encounter.CanBeEncountered(seed))
            return null;

        // Get generation parameters
        var pi = PersonalTable.SV[encounter.Species, encounter.Form];
        var param = new GenerateParam9(
            encounter.Species,
            pi.Gender,
            encounter.FlawlessIVCount,
            1, // roll count
            0, // height
            0, // weight
            SizeType9.RANDOM, // scale type
            0, // scale
            encounter.Ability,
            encounter.Shiny,
            encounter is IFixedNature fn ? fn.Nature : Nature.Random,
            encounter is EncounterDist9 dist && dist.IVs.IsSpecified ? dist.IVs : default
        );

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
            OriginalTrainerFriendship = pi.BaseFriendship,
            Nickname = SpeciesName.GetSpeciesNameGeneration(encounter.Species, language, 9),
        };

        try
        {
            // Initialize RNG with seed
            var rng = new Xoroshiro128Plus(seed);

            // Generate all Pokémon attributes in the exact order
            SetEncryptionAndPID(pk, ref rng, param, encounter.Shiny == Shiny.Always);
            SetIVs(pk, param, ref rng);
            SetAbility(pk, param, ref rng);
            SetGender(pk, param, ref rng);
            SetNature(pk, param, ref rng);
            SetScaleAndSize(pk, param, ref rng);

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

            // Ensure valid Met Date for Mighty Raid Pokemon
            pk.CheckAndSetUnrivaledDate();

            pk.ResetPartyStats();

            return pk;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating raid Pokémon: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets the Encryption Constant and PID with proper shiny handling
    /// </summary>
    private static void SetEncryptionAndPID(PK9 pk, ref Xoroshiro128Plus rng, GenerateParam9 param, bool forceShiny)
    {
        pk.EncryptionConstant = (uint)rng.NextInt(uint.MaxValue);

        uint fakeTID = (uint)rng.NextInt(uint.MaxValue);
        uint pid = (uint)rng.NextInt(uint.MaxValue);

        if (param.Shiny == Shiny.Always || forceShiny)
        {
            var tid = (ushort)fakeTID;
            var sid = (ushort)(fakeTID >> 16);
            if (!ShinyUtil.GetIsShiny6(fakeTID, pid)) // If not shiny vs fake TID
                pid = ShinyUtil.GetShinyPID(tid, sid, pid, 0);
            if (!ShinyUtil.GetIsShiny6(pk.ID32, pid)) // If not shiny vs player TID
                pid = ShinyUtil.GetShinyPID(pk.TID16, pk.SID16, pid, ShinyUtil.GetShinyXor(pid, fakeTID) == 0 ? 0u : 1u);
        }
        else if (param.Shiny == Shiny.Never)
        {
            if (ShinyUtil.GetIsShiny6(fakeTID, pid)) // battled
                pid ^= 0x10000000;
            if (ShinyUtil.GetIsShiny6(pk.ID32, pid)) // captured
                pid ^= 0x10000000;
        }
        else // Random shiny
        {
            // For random shiny, we need to check if it rolled shiny
            int rollCount = param.RollCount;
            bool isShiny = false;
            uint xor = 0;

            for (int i = 0; i < rollCount; i++)
            {
                xor = ShinyUtil.GetShinyXor(pid, fakeTID);
                isShiny = xor < 16;
                if (isShiny)
                {
                    if (xor != 0)
                        xor = 1;
                    break;
                }
                if (i < rollCount - 1)
                    pid = (uint)rng.NextInt(uint.MaxValue);
            }

            ShinyUtil.ForceShinyState(isShiny, ref pid, pk.ID32, xor);
        }

        pk.PID = pid;
    }

    /// <summary>
    /// Sets the IVs based on encounter parameters
    /// </summary>
    private static void SetIVs(PK9 pk, GenerateParam9 param, ref Xoroshiro128Plus rng)
    {
        Span<int> ivs = stackalloc int[6];
        for (int i = 0; i < 6; i++)
            ivs[i] = -1; // Initialize as unfixed

        // Handle fixed IV spreads if specified
        if (param.IVs.IsSpecified)
        {
            param.IVs.CopyToSpeedLast(ivs);
        }
        else
        {
            // Set guaranteed flawless IVs
            int flawlessCount = Math.Min((int)param.FlawlessIVs, 6);
            for (int i = 0; i < flawlessCount; i++)
            {
                int index;
                do
                {
                    index = (int)rng.NextInt(6);
                } while (ivs[index] != -1);
                ivs[index] = 31;
            }
        }

        // Generate random IVs for remaining stats
        for (int i = 0; i < 6; i++)
        {
            if (ivs[i] == -1)
                ivs[i] = (int)rng.NextInt(32);
        }

        // Apply IVs to the Pokémon
        pk.IV_HP = ivs[0];
        pk.IV_ATK = ivs[1];
        pk.IV_DEF = ivs[2];
        pk.IV_SPA = ivs[3];
        pk.IV_SPD = ivs[4];
        pk.IV_SPE = ivs[5];
    }

    /// <summary>
    /// Sets the ability based on encounter parameters
    /// </summary>
    private static void SetAbility(PK9 pk, GenerateParam9 param, ref Xoroshiro128Plus rng)
    {
        var pi = PersonalTable.SV.GetFormEntry(pk.Species, pk.Form);
        var abilityIndex = param.Ability switch
        {
            AbilityPermission.Any12H => (int)rng.NextInt(3),
            AbilityPermission.Any12 => (int)rng.NextInt(2),
            AbilityPermission.OnlyFirst => 0,
            AbilityPermission.OnlySecond => 1,
            AbilityPermission.OnlyHidden => 2,
            _ => 0,
        };

        // Apply ability based on index
        pk.RefreshAbility(abilityIndex);
    }

    /// <summary>
    /// Sets the gender based on species gender ratio and RNG
    /// </summary>
    private static void SetGender(PK9 pk, GenerateParam9 param, ref Xoroshiro128Plus rng)
    {
        byte genderRatio = param.GenderRatio;

        if (genderRatio == PersonalInfo.RatioMagicGenderless)
            pk.Gender = 2; // Genderless
        else if (genderRatio == PersonalInfo.RatioMagicFemale)
            pk.Gender = 1; // Female only
        else if (genderRatio == PersonalInfo.RatioMagicMale)
            pk.Gender = 0; // Male only
        else
        {
            // Use RNG for gender determination
            var rand100 = rng.NextInt(100);
            pk.Gender = Encounter9RNG.GetGender(genderRatio, rand100);
        }
    }

    /// <summary>
    /// Sets the nature based on encounter parameters
    /// </summary>
    private static void SetNature(PK9 pk, GenerateParam9 param, ref Xoroshiro128Plus rng)
    {
        Nature nature;

        if (param.Nature != Nature.Random)
        {
            nature = param.Nature;
        }
        else if (pk.Species == (int)Species.Toxtricity)
        {
            // Special case for Toxtricity - nature determines form
            nature = ToxtricityUtil.GetRandomNature(ref rng, pk.Form);
        }
        else
        {
            // Random nature (0-24)
            nature = (Nature)rng.NextInt(25);
        }

        pk.Nature = pk.StatNature = nature;
    }

    /// <summary>
    /// Sets the height, weight and scale based on encounter parameters
    /// </summary>
    private static void SetScaleAndSize(PK9 pk, GenerateParam9 param, ref Xoroshiro128Plus rng)
    {
        // Set height scalar
        pk.HeightScalar = param.Height != 0 ? param.Height : (byte)(rng.NextInt(0x81) + rng.NextInt(0x80));

        // Set weight scalar
        pk.WeightScalar = param.Weight != 0 ? param.Weight : (byte)(rng.NextInt(0x81) + rng.NextInt(0x80));

        // Set scale according to scale type
        pk.Scale = param.ScaleType.GetSizeValue(param.Scale, ref rng);
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
            {
                // Use a darker gold color that works better with dark themes
                resultsGrid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(64, 64, 32);
                resultsGrid.Rows[row].DefaultCellStyle.ForeColor = Color.Gold;

                // Ensure the selection colors are still visible for shiny rows
                resultsGrid.Rows[row].DefaultCellStyle.SelectionBackColor = Color.DarkGoldenrod;
                resultsGrid.Rows[row].DefaultCellStyle.SelectionForeColor = Color.White;
            }
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
        public string Text { get; } = encounter == null ? "Any Encounter" : GetEncounterText(encounter);
        public ITeraRaid9? Encounter { get; } = encounter;

        private static string GetEncounterText(ITeraRaid9 encounter)
        {
            var type = GetEncounterType(encounter);
            var formName = "";

            // Add form name if it's not the base form and not a random form
            if (encounter.Form != 0 && encounter is not IEncounterFormRandom { IsRandomUnspecificForm: true })
            {
                var forms = FormConverter.GetFormList((ushort)encounter.Species, GameInfo.Strings.types,
                    GameInfo.Strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
                if (encounter.Form < forms.Length)
                    formName = $" ({forms[encounter.Form]})";
            }

            return $"{encounter.Stars}★ {type}{formName}";
        }

        /// <summary>
        /// Gets the encounter type display name
        /// </summary>
        private static string GetEncounterType(ITeraRaid9 encounter)
        {
            return encounter switch
            {
                EncounterTera9 => "Base",
                EncounterDist9 => "Event",
                EncounterMight9 => "Mighty",
                _ => "Unknown"
            };
        }
    }
}