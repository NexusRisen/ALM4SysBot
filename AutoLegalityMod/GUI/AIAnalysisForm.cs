using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
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

        B_Analyze.Enabled = false;
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
            var context = BuildAnalysisContext(template, almres, la, legalityReport, timeInfo);

            // Get AI analysis
            var aiResponse = await _aiService.AnalyzeShowdownSetAsync(input, context);

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
            progressBar.Visible = false;
        }
    }

    private string BuildAnalysisContext(RegenTemplate template, AsyncLegalizationResult pk, LegalityAnalysis la, string legalityReport, string timeInfo)
    {
        var context = $"Game Version: {_sav.Version} (Generation {_sav.Generation})\n";
        context += $"Species: {SpeciesName.GetSpeciesName(template.Species, (int)LanguageID.English)}\n";
        context += $"Legalization Status: {pk.Status}\n";
        context += $"Is Legal: {la.Valid}\n";
        context += $"{timeInfo}\n\n";

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

    private void DisplayFormattedResponse(string aiResponse, bool isLegal, LegalizationResult status)
    {
        TB_Output.Clear();

        // Add header with color
        var isSuccess = isLegal && status == LegalizationResult.Regenerated;
        var headerSymbol = isSuccess ? "✓" : "✗";
        var headerText = isSuccess ? "Your Pokémon is legal!" : "Issues found with your Pokémon:";
        var headerColor = isSuccess ? Color.Green : Color.Red;

        // Add colored header
        TB_Output.SelectionColor = headerColor;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 10, FontStyle.Bold);
        TB_Output.AppendText($"{headerSymbol} {headerText}");

        // Add status
        TB_Output.SelectionColor = Color.Gray;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
        TB_Output.AppendText($" (Status: {status})\r\n");

        // Add separator
        TB_Output.SelectionColor = Color.Black;
        TB_Output.AppendText(new string('═', 60) + "\r\n\r\n");

        // Add AI response with cleaned formatting
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
        var cleanedResponse = CleanMarkdownForDisplay(aiResponse);
        TB_Output.AppendText(cleanedResponse);
    }

    private string FormatAIResponse(string aiResponse, bool isLegal, LegalizationResult status)
    {
        var header = (isLegal && status == LegalizationResult.Regenerated)
            ? "✓ Your Pokémon is legal!"
            : "✗ Issues found with your Pokémon:";
        header += $" (Status: {status})\r\n";
        header += new string('=', 60) + "\r\n\r\n";

        // Clean up markdown formatting for better display in TextBox
        var cleanedResponse = CleanMarkdownForDisplay(aiResponse);

        return header + cleanedResponse;
    }

    private string CleanMarkdownForDisplay(string markdown)
    {
        // Remove or replace common markdown syntax
        var cleaned = markdown;

        // Replace headers
        cleaned = Regex.Replace(cleaned, @"^#{1,6}\s+(.+)$", "【 $1 】", RegexOptions.Multiline);

        // Replace bold
        cleaned = cleaned.Replace("**", "");

        // Replace code blocks
        cleaned = Regex.Replace(cleaned, @"```[^\n]*\n(.*?)\n```", "$1", RegexOptions.Singleline);

        // Replace inline code
        cleaned = cleaned.Replace("`", "");

        // Replace bullet points
        cleaned = Regex.Replace(cleaned, @"^\s*[-*]\s+", "• ", RegexOptions.Multiline);

        // Ensure proper line endings for Windows
        cleaned = cleaned.Replace("\n", "\r\n");

        return cleaned;
    }

    private void B_Clear_Click(object sender, EventArgs e)
    {
        TB_Input.Clear();
        TB_Output.Clear();
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