using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using static PKHeX.Core.AutoMod.APILegality;

namespace AutoModPlugins.GUI;

public partial class AIAnalysisForm : Form
{
    private readonly PluginSettings _settings;
    private readonly SaveFile _sav;
    private readonly AIService _aiService;
    private CancellationTokenSource? _analysisCts;

    public AIAnalysisForm(PluginSettings settings, SaveFile sav)
    {
        _settings = settings;
        _sav = sav;
        _aiService = new AIService(settings.OpenAIApiKey, settings.AIModel, settings.MaxTokens, settings.Temperature);

        InitializeComponent();

        TB_Input.PlaceholderText = "Paste your Showdown set here...";
        TB_Output.ReadOnly = true;

        this.Text = "AI Analysis - AutoLegalityMod";
        this.CenterToParent();
    }

    private async void B_Analyze_Click(object sender, EventArgs e)
    {
        var input = TB_Input.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            WinFormsUtil.Alert("Please paste a Showdown set to analyze.");
            return;
        }

        _analysisCts?.Cancel();
        _analysisCts?.Dispose();
        _analysisCts = new CancellationTokenSource();

        B_Analyze.Enabled = false;
        B_Clear.Text = "Cancel";
        TB_Output.Text = "Analyzing...";
        progressBar.Visible = true;

        try
        {
            // Parse the showdown set
            var set = new ShowdownSet(input);
            var template = new RegenTemplate(set, _sav.Generation);

            // Generate a PKM to analyze using the same logic as paste importer
            var timer = Stopwatch.StartNew();
            var almres = _sav.GetLegalFromSet(template);
            timer.Stop();

            var pk = almres.Created;
            var la = new LegalityAnalysis(pk);

            // Check if a specific version was requested in the set
            var targetVersion = _sav.Version;
            if (template.Regen.TryGetBatchValue(".Version", out var versionStr) && int.TryParse(versionStr, out var version))
                targetVersion = (GameVersion)version;

            // Get the legality analysis
            string legalityReport = "";
            if (almres.Status != LegalizationResult.Regenerated)
            {
                legalityReport = template.SetAnalysis(_sav, pk);
            }
            else if (!la.Valid)
            {
                legalityReport = la.Report();
            }

            // Add timing info
            var timeInfo = $"Generation time: {timer.Elapsed.TotalSeconds:F2} seconds";

            // Create context for AI
            var context = BuildAnalysisContext(template, almres, la, legalityReport, timeInfo, targetVersion);

            // Get AI analysis
            var aiResponse = await _aiService.AnalyzeShowdownSetAsync(input, context, _analysisCts.Token);

            // Display result with formatting
            DisplayFormattedResponse(aiResponse, la.Valid, almres.Status);
        }
        catch (Exception ex)
        {
            TB_Output.Text = $"Error during analysis:\r\n{ex.Message}";
        }
        finally
        {
            B_Analyze.Enabled = true;
            B_Clear.Text = "Clear";
            progressBar.Visible = false;
            _analysisCts?.Dispose();
            _analysisCts = null;
        }
    }

    private string BuildAnalysisContext(RegenTemplate template, AsyncLegalizationResult pk, LegalityAnalysis la, string legalityReport, string timeInfo, GameVersion targetVersion)
    {
        var context = $"Game Version: {targetVersion} (Generation {_sav.Generation})\n";
        context += $"Species: {SpeciesName.GetSpeciesName(template.Species, (int)LanguageID.English)}\n";
        context += $"Form: {template.Form}\n";
        context += $"Level: {template.Level}\n";
        context += $"Legalization Status: {pk.Status}\n";
        context += $"Is Legal: {la.Valid}\n";
        context += $"{timeInfo}\n\n";

        // Emphasize if the Pokémon is already legal
        if (la.Valid && pk.Status == LegalizationResult.Regenerated)
        {
            context += "** THIS POKÉMON IS LEGAL - No changes needed! **\n\n";
        }

        // Add specific validation data from PKHeX
        if (!la.Valid || pk.Status != LegalizationResult.Regenerated)
        {
            context += "== DETAILED ANALYSIS ==\n";

            // Get valid data for this species/form
            var validData = GetValidDataForSpecies(template, _sav, targetVersion);
            context += validData + "\n";
        }
        else
        {
            // Still show valid data for reference even if legal
            context += "== REFERENCE DATA ==\n";
            var validData = GetValidDataForSpecies(template, _sav, targetVersion);
            context += validData + "\n";
        }

        if (!string.IsNullOrWhiteSpace(legalityReport))
        {
            context += $"Legality Analysis:\n{legalityReport}\n\n";
        }

        if (!la.Valid)
        {
            context += "Invalid Checks:\n";
            foreach (var check in la.Results)
            {
                if (!check.Valid)
                    context += $"- {check.Comment}\n";
            }
        }

        return context;
    }

    private string GetValidDataForSpecies(RegenTemplate template, SaveFile sav, GameVersion targetVersion)
    {
        var sb = new StringBuilder();
        var species = template.Species;
        var form = template.Form;
        var gen = sav.Generation;
        var version = targetVersion;

        try
        {
            // Get personal info
            var pi = sav.Personal.GetFormEntry(species, form);
            if (pi == null)
                return "Unable to find species data.";

            // Get valid abilities
            sb.AppendLine("VALID ABILITIES:");
            var abilities = GetValidAbilities(pi, gen);
            foreach (var ability in abilities)
                sb.AppendLine($"- {ability}");

            // Get valid moves with level information
            sb.AppendLine("\nVALID MOVES WITH LEVELS:");
            var movesWithLevels = GetValidMovesWithLevels(species, form, gen, version);
            if (movesWithLevels.Count > 0)
            {
                sb.AppendLine($"Total valid moves: {movesWithLevels.Count}");
                // Show a sample of moves if there are many
                var moveSample = movesWithLevels.Take(20).ToList();
                foreach (var (move, level) in moveSample)
                {
                    if (level > 0)
                        sb.AppendLine($"- {move} (Level {level})");
                    else
                        sb.AppendLine($"- {move} (TM/TR/Tutor/Egg)");
                }
                if (movesWithLevels.Count > 20)
                    sb.AppendLine($"... and {movesWithLevels.Count - 20} more moves");
            }

            // Add specific move validation with level requirements
            sb.AppendLine("\nMOVE VALIDATION:");
            var movelist = GameInfo.Strings.movelist;
            for (int i = 0; i < template.Moves.Length; i++)
            {
                var move = template.Moves[i];
                if (move == 0) continue;

                var moveName = movelist[move];
                var moveInfo = movesWithLevels.FirstOrDefault(m => m.Move == moveName);

                if (moveInfo.Move != null)
                {
                    if (moveInfo.Level > 0)
                    {
                        if (moveInfo.Level <= template.Level)
                        {
                            sb.AppendLine($"- {moveName}: Valid (learns at level {moveInfo.Level})");
                        }
                        else
                        {
                            sb.AppendLine($"- {moveName}: INVALID at current level {template.Level} (learns at level {moveInfo.Level})");
                            sb.AppendLine($"  → Suggestion: Either increase Pokémon level to {moveInfo.Level}+ or replace the move");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"- {moveName}: Valid (TM/TR/Tutor/Egg move)");
                    }
                }
                else
                {
                    sb.AppendLine($"- {moveName}: INVALID for this species/generation");
                }
            }

            // Get valid balls
            sb.AppendLine("\nVALID BALLS:");
            var validBalls = GetValidBalls(species, form, gen, version);
            foreach (var ball in validBalls)
                sb.AppendLine($"- {ball}");

            // Get encounter locations
            sb.AppendLine("\nVALID ENCOUNTER TYPES:");
            var encounters = GetValidEncounterTypes(species, form, version);
            foreach (var enc in encounters)
                sb.AppendLine($"- {enc}");

            // Check if Hidden Ability is available
            if (pi is IPersonalAbility12H pah && pah.AbilityH != 0)
            {
                sb.AppendLine($"\nHIDDEN ABILITY: {GameInfo.Strings.abilitylist[pah.AbilityH]} - ");
                sb.Append(CanHaveHiddenAbility(species, form, gen) ? "Available" : "Not available in this generation");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting valid data: {ex.Message}";
        }
    }

    private static List<string> GetValidAbilities(IPersonalInfo pi, int generation)
    {
        var abilities = new List<string>();
        var strings = GameInfo.Strings;

        // Check if it implements IPersonalAbility12
        if (pi is IPersonalAbility12 pa12)
        {
            if (pa12.Ability1 != 0)
                abilities.Add($"{strings.abilitylist[pa12.Ability1]} (1)");
            if (pa12.Ability2 != 0)
                abilities.Add($"{strings.abilitylist[pa12.Ability2]} (2)");

            // Check for hidden ability
            if (pa12 is IPersonalAbility12H pah && pah.AbilityH != 0)
                abilities.Add($"{strings.abilitylist[pah.AbilityH]} (Hidden)");
        }
        else
        {
            // Fallback for older generation formats that might not implement IPersonalAbility12
            // Try to get abilities through the interface methods
            for (int i = 0; i < pi.AbilityCount; i++)
            {
                var abilityId = pi.GetAbilityAtIndex(i);
                if (abilityId != 0)
                    abilities.Add($"{strings.abilitylist[abilityId]} ({i + 1})");
            }
        }

        // Remove duplicates while preserving order and labels
        var uniqueAbilities = new List<string>();
        var seenNames = new HashSet<string>();

        foreach (var ability in abilities)
        {
            var abilityName = ability.Split(' ')[0]; // Get just the ability name
            if (!seenNames.Contains(abilityName))
            {
                uniqueAbilities.Add(ability);
                seenNames.Add(abilityName);
            }
        }

        return uniqueAbilities;
    }

    private static List<(string Move, int Level)> GetValidMovesWithLevels(ushort species, byte form, int generation, GameVersion version)
    {
        var movesWithLevels = new Dictionary<ushort, int>(); // moveId -> minimum level
        var strings = GameInfo.Strings;

        try
        {
            // Get all possible moves
            var learnSource = GameData.GetLearnSource(version);
            var learnset = learnSource.GetLearnset(species, form);

            // Get all moves the Pokémon can learn
            var allMoves = learnset.GetAllMoves();

            // For each move, check if it's learned by level up and get its level
            foreach (var moveId in allMoves)
            {
                if (moveId == 0) continue;

                if (learnset.TryGetLevelLearnMove(moveId, out byte level))
                {
                    // Store the minimum level for each move (though Learnset should already have unique moves)
                    if (!movesWithLevels.TryGetValue(moveId, out int value) || level < value)
                    {
                        value = level;
                        movesWithLevels[moveId] = value;
                    }
                }
            }

            // Get all moves available at level 100 (includes TM/TR/Tutor/Egg moves)
            var allAvailableMoves = learnset.GetMoveRange(100);
            foreach (var moveId in allAvailableMoves)
            {
                if (moveId != 0 && !movesWithLevels.ContainsKey(moveId))
                {
                    // This is a TM/TR/Tutor/Egg move (not learned by level up)
                    movesWithLevels[moveId] = 0;
                }
            }

            // Convert to list with move names
            var result = new List<(string Move, int Level)>();
            foreach (var kvp in movesWithLevels)
            {
                var moveName = strings.movelist[kvp.Key];
                result.Add((moveName, kvp.Value));
            }

            return [.. result.OrderBy(m => m.Move)];
        }
        catch (Exception)
        {
            // Fallback to simpler method if the above doesn't work
            var result = new List<(string Move, int Level)>();

            try
            {
                var learnSource = GameData.GetLearnSource(version);
                var learnset = learnSource.GetLearnset(species, form);

                // Get all moves up to level 100
                var allMoves = learnset.GetMoveRange(100);

                foreach (var moveId in allMoves)
                {
                    if (moveId == 0) continue;

                    var moveName = strings.movelist[moveId];

                    // Check if it's a level-up move
                    if (learnset.TryGetLevelLearnMove(moveId, out byte level))
                    {
                        result.Add((moveName, level));
                    }
                    else
                    {
                        // It's available but not by level-up (TM/TR/Tutor/Egg)
                        result.Add((moveName, 0));
                    }
                }
            }
            catch
            {
                // If all else fails, at least return the moves without level info
                var moves = GetValidMoves(species, form, generation, version);
                foreach (var move in moves)
                {
                    result.Add((move, 0));
                }
            }

            return [.. result.OrderBy(m => m.Move)];
        }
    }

    private static List<string> GetValidMoves(ushort species, byte form, int generation, GameVersion version, byte level = 100)
    {
        var moves = new HashSet<string>();
        var strings = GameInfo.Strings;

        // Get a dummy PKM for move validation
        var pk = EntityBlank.GetBlank((byte)generation);
        pk.Species = species;
        pk.Form = form;
        pk.CurrentLevel = level;

        // Get all possible moves
        var learnSource = GameData.GetLearnSource(version);
        var learnset = learnSource.GetLearnset(species, form);
        foreach (var move in learnset.GetMoveRange(level))
        {
            if (move != 0)
                moves.Add(strings.movelist[move]);
        }

        foreach (var move in learnset.GetMoveRange(100))
        {
            if (move != 0)
                moves.Add(strings.movelist[move]);
        }

        return [.. moves.OrderBy(m => m)];
    }

    private static List<string> GetValidBalls(ushort species, byte form, int generation, GameVersion version)
    {
        var balls = new List<string>();

        // Get valid wild balls for the generation
        var wildBalls = BallUseLegality.GetWildBalls((byte)generation, version);

        for (byte ballId = 1; ballId < 64; ballId++)
        {
            if (BallUseLegality.IsBallPermitted(wildBalls, ballId))
            {
                var ball = (Ball)ballId;
                balls.Add(ball.ToString());
            }
        }

        // Check breedable balls for eggs
        if (generation >= 6) // Ball inheritance started in Gen 6
        {
            for (byte ballId = 1; ballId < 64; ballId++)
            {
                var ball = (Ball)ballId;
                if (ball != Ball.Master && ball != Ball.Cherish &&
                    BallContextHOME.Instance.CanBreedWithBall(species, form, ball))
                {
                    if (!balls.Contains(ball.ToString()))
                        balls.Add($"{ball} (Breeding)");
                }
            }
        }

        return balls;
    }

    private List<string> GetValidEncounterTypes(ushort species, byte form, GameVersion version)
    {
        var types = new HashSet<string>();

        // Get encounters for this species
        var blank = EntityBlank.GetBlank((byte)_sav.Generation);
        blank.Species = species;
        blank.Form = form;

        // Create a LegalityAnalysis to get the LegalInfo
        var la = new LegalityAnalysis(blank);
        var encounters = EncounterGenerator.GetEncounters(blank, la.Info);

        foreach (var enc in encounters)
        {
            // Use the encounter's Name property or check specific interfaces/types
            var type = enc switch
            {
                IEncounterEgg => "Egg",
                _ when enc.Name.Contains("Egg") => "Egg",
                _ when enc.Name.Contains("Static") => "Static/Gift",
                _ when enc.Name.Contains("Trade") => "In-Game Trade",
                _ when enc.Name.Contains("Wild") || enc.Name.Contains("Slot") => "Wild",
                _ => enc.Name // Use the encounter's name directly
            };
            types.Add(type);
        }

        return [.. types.OrderBy(t => t)];
    }

    private static bool CanHaveHiddenAbility(ushort species, byte form, int generation)
    {
        // Hidden abilities were introduced in Gen 5
        if (generation < 5)
            return false;

        // In Gen 5-6, not all methods could yield hidden abilities
        if (generation <= 6)
        {
            // Would need to check specific encounter types
            // For simplicity, we'll say it's possible
            return true;
        }

        // Gen 7+ most encounters can have hidden abilities
        return true;
    }

    private void DisplayFormattedResponse(string aiResponse, bool isLegal, LegalizationResult status)
    {
        TB_Output.Clear();

        // Detect if using dark theme
        bool isDarkTheme = IsDarkTheme();

        // Define color schemes
        var successColor = Color.FromArgb(0, 255, 127); // Spring Green - visible on both themes
        var errorColor = Color.FromArgb(255, 99, 71); // Tomato - softer than pure red
        var headerColor = isDarkTheme ? Color.FromArgb(100, 221, 255) : Color.FromArgb(0, 100, 200); // Light blue for dark, darker blue for light
        var statusColor = isDarkTheme ? Color.FromArgb(192, 192, 192) : Color.FromArgb(96, 96, 96);
        var textColor = isDarkTheme ? Color.FromArgb(240, 240, 240) : Color.Black;
        var codeColor = isDarkTheme ? Color.FromArgb(152, 251, 152) : Color.FromArgb(0, 128, 0); // Pale green for dark, dark green for light

        // Add header with color
        var isSuccess = isLegal && status == LegalizationResult.Regenerated;
        var headerSymbol = isSuccess ? "✓" : "✗";
        var headerText = isSuccess ? "Your Pokémon is legal!" : "Issues found with your Pokémon:";

        // Add colored header
        TB_Output.SelectionColor = isSuccess ? successColor : errorColor;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 10, FontStyle.Bold);
        TB_Output.AppendText($"{headerSymbol} {headerText}");

        // Add status
        TB_Output.SelectionColor = statusColor;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
        TB_Output.AppendText($" (Status: {status})\r\n");

        // Add separator
        TB_Output.SelectionColor = textColor;
        TB_Output.AppendText(new string('═', 60) + "\r\n\r\n");

        // Process and format the AI response
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);

        // Split response into sections for better formatting
        var sections = SplitIntoSections(aiResponse);

        foreach (var section in sections)
        {
            if (section.IsHeader)
            {
                // Format headers
                TB_Output.SelectionColor = headerColor;
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 10, FontStyle.Bold);
                TB_Output.AppendText(section.Content + "\r\n");
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
            }
            else if (section.IsShowdownSet)
            {
                // Format showdown sets with monospace font
                TB_Output.SelectionColor = codeColor;
                TB_Output.SelectionFont = new Font("Consolas", 9, FontStyle.Regular);
                TB_Output.AppendText(section.Content);
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
            }
            else
            {
                // Regular text
                TB_Output.SelectionColor = textColor;
                TB_Output.AppendText(section.Content);
            }
        }
    }

    private bool IsDarkTheme()
    {
        // Check if the output textbox background is dark
        var bgColor = TB_Output.BackColor;
        if (bgColor == SystemColors.Window)
        {
            // If using system colors, check the form background
            bgColor = this.BackColor;
        }

        var brightness = (bgColor.R * 0.299 + bgColor.G * 0.587 + bgColor.B * 0.114);
        return brightness < 128;
    }

    private static List<TextSection> SplitIntoSections(string aiResponse)
    {
        var sections = new List<TextSection>();
        var cleaned = CleanMarkdownForDisplay(aiResponse);

        // Split by double newlines to find sections
        var parts = cleaned.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Check if this is a header (starts with == or ends with ==)
            if (trimmed.StartsWith("==") || trimmed.EndsWith("==") || trimmed.StartsWith("【"))
            {
                sections.Add(new TextSection { Content = trimmed, IsHeader = true });
            }
            // Check if this looks like a showdown set (has @ or multiple lines with specific patterns)
            else if (IsShowdownSet(trimmed))
            {
                sections.Add(new TextSection { Content = trimmed + "\r\n", IsShowdownSet = true });
            }
            else
            {
                // Process regular text - ensure proper formatting
                var formatted = FormatParagraph(trimmed);
                sections.Add(new TextSection { Content = formatted + "\r\n\r\n", IsHeader = false });
            }
        }

        return sections;
    }

    private static bool IsShowdownSet(string text)
    {
        // Check if text looks like a showdown set
        return text.Contains(" @ ") ||
               (text.Contains("Level:") && text.Contains("Nature")) ||
               (text.Contains("EVs:") || text.Contains("IVs:")) ||
               (text.StartsWith("-") && text.Contains("\n-"));
    }

    private static string FormatParagraph(string text)
    {
        // Ensure bullet points are on new lines
        text = Regex.Replace(text, @"([.!?])\s*•", "$1\r\n•");
        text = Regex.Replace(text, @"([.!?])\s*-\s+", "$1\r\n- ");

        // Fix numbered lists
        text = Regex.Replace(text, @"(\d+)\.\s+", "\r\n$1. ");

        // Ensure there's a space after punctuation
        text = Regex.Replace(text, @"([.!?])([A-Z])", "$1 $2");

        // Clean up excessive whitespace
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        return text;
    }

    private class TextSection
    {
        public string Content { get; set; } = "";
        public bool IsHeader { get; set; }
        public bool IsShowdownSet { get; set; }
    }

    private static string CleanMarkdownForDisplay(string markdown)
    {
        // Remove or replace common markdown syntax
        var cleaned = markdown;

        // Replace headers - make them more prominent
        cleaned = Regex.Replace(cleaned, @"^#{1,6}\s+(.+)$", "\r\n== $1 ==\r\n", RegexOptions.Multiline);

        // Replace bold
        cleaned = Regex.Replace(cleaned, @"\*\*(.+?)\*\*", "$1");

        // Replace code blocks - preserve formatting
        cleaned = Regex.Replace(cleaned, @"```[^\n]*\n(.*?)\n```", "\r\n$1\r\n", RegexOptions.Singleline);

        // Replace inline code
        cleaned = Regex.Replace(cleaned, @"`(.+?)`", "$1");

        // Replace bullet points with proper spacing (but preserve - in showdown sets)
        // First, protect showdown set moves by temporarily replacing them
        cleaned = Regex.Replace(cleaned, @"^(\s*)-(\s+\w+.*?)$", "SHOWDOWNMOVE$1-$2", RegexOptions.Multiline);

        // Now replace other bullet points
        cleaned = Regex.Replace(cleaned, @"^\s*[-*]\s+", "\r\n• ", RegexOptions.Multiline);

        // Restore showdown moves
        cleaned = cleaned.Replace("SHOWDOWNMOVE", "");

        // Fix numbered lists
        cleaned = Regex.Replace(cleaned, @"^(\d+)\.\s+", "\r\n$1. ", RegexOptions.Multiline);

        // Ensure double line breaks between major sections
        cleaned = Regex.Replace(cleaned, @"(==\s*[^=]+\s*==)", "\r\n\r\n$1\r\n", RegexOptions.Singleline);

        // Clean up excessive newlines
        cleaned = Regex.Replace(cleaned, @"(\r\n){3,}", "\r\n\r\n");

        // Ensure proper line endings for Windows
        cleaned = cleaned.Replace("\n", "\r\n");

        return cleaned.Trim();
    }

    private void B_Clear_Click(object sender, EventArgs e)
    {
        if (B_Clear.Text == "Cancel")
        {
            _analysisCts?.Cancel();
        }
        else
        {
            TB_Input.Clear();
            TB_Output.Clear();
        }
    }

    private void B_Copy_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TB_Output.Text))
        {
            TB_Output.SelectAll();
            TB_Output.Copy();
            WinFormsUtil.Alert("Analysis copied to clipboard!");
        }
    }
}