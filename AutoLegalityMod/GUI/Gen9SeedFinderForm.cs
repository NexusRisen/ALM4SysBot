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

public partial class Gen9SeedFinderForm : Form
{
    private readonly ISaveFileProvider _saveFileEditor;
    private readonly IPKMView _pkmEditor;
    private CancellationTokenSource? _searchCts;
    private readonly List<SeedResult> _results = new();
    private readonly Random _random = new();

    private const int SearchProgressInterval = 1000;

    public Gen9SeedFinderForm(ISaveFileProvider saveFileEditor, IPKMView pkmEditor)
    {
        _saveFileEditor = saveFileEditor ?? throw new ArgumentNullException(nameof(saveFileEditor));
        _pkmEditor = pkmEditor ?? throw new ArgumentNullException(nameof(pkmEditor));
        InitializeComponent();
        LoadSpeciesList();

        FormClosing += (s, e) => _searchCts?.Cancel();
        FormClosed += (s, e) => _searchCts?.Dispose();
    }

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

    private async Task CancelSearchAsync()
    {
        _searchCts?.Cancel();
        await Task.Delay(100);
    }

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

    private void PrepareSearch()
    {
        _results.Clear();
        resultsGrid.Rows.Clear();
        searchButton.Text = "Stop";
        progressBar.Visible = true;
        progressBar.Value = 0;
        statusLabel.Text = "Searching...";
    }

    private void CleanupSearch()
    {
        searchButton.Text = "Search";
        progressBar.Visible = false;
        _searchCts?.Dispose();
        _searchCts = null;
    }

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

    private static bool CheckIVsCriteria(PK9 pk, EncounterCriteria criteria)
    {
        return (criteria.IV_HP == -1 || pk.IV_HP >= criteria.IV_HP) &&
               (criteria.IV_ATK == -1 || pk.IV_ATK >= criteria.IV_ATK) &&
               (criteria.IV_DEF == -1 || pk.IV_DEF >= criteria.IV_DEF) &&
               (criteria.IV_SPA == -1 || pk.IV_SPA >= criteria.IV_SPA) &&
               (criteria.IV_SPD == -1 || pk.IV_SPD >= criteria.IV_SPD) &&
               (criteria.IV_SPE == -1 || pk.IV_SPE >= criteria.IV_SPE);
    }

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

    private static IEnumerable<T> FilterEncounters<T>(T[] encounters, int species, byte form) where T : ITeraRaid9
    {
        return encounters.Where(e => e.Species == species && (e.Form == form || e.Form >= EncounterUtil.FormDynamic));
    }

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

        var param = new GenerateParam9(
            encounter.Species,
            pi.Gender,
            encounter.FlawlessIVCount,
            1,
            0,
            0,
            SizeType9.RANDOM,
            0,
            encounter.Ability,
            encounter.Shiny,
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

    private sealed class SeedResult
    {
        public required uint Seed { get; init; }
        public required ITeraRaid9 Encounter { get; init; }
        public required PK9 Pokemon { get; init; }
    }

    private sealed class ComboItem(string text, int value)
    {
        public string Text { get; } = text;
        public int Value { get; } = value;
    }

    private sealed class EncounterItem(ITeraRaid9? encounter)
    {
        public string Text { get; } = encounter == null ? "Any Encounter" : $"{encounter.Stars}★ {GetEncounterType(encounter)}";
        public ITeraRaid9? Value { get; } = encounter;

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