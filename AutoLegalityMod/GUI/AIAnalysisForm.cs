using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

namespace AutoModPlugins.GUI;

public partial class AIAnalysisForm : Form
{
    private readonly PluginSettings _settings;
    private readonly SaveFile _sav;
    private readonly AIService _aiService;
    private readonly AnalysisContextBuilder _contextBuilder;
    private CancellationTokenSource? _analysisCts;

    public AIAnalysisForm(PluginSettings settings, SaveFile sav)
    {
        _settings = settings;
        _sav = sav;
        _aiService = new AIService(settings.OpenAIApiKey, settings.AIModel, settings.MaxTokens, settings.Temperature);
        _contextBuilder = new AnalysisContextBuilder(sav);

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
            var set = new ShowdownSet(input);
            var template = new RegenTemplate(set, _sav.Generation);

            var timer = Stopwatch.StartNew();
            var almres = _sav.GetLegalFromSet(template);
            timer.Stop();

            var pk = almres.Created;
            var la = new LegalityAnalysis(pk);

            var targetVersion = _sav.Version;
            if (template.Regen.TryGetBatchValue(".Version", out var versionStr) && int.TryParse(versionStr, out var version))
                targetVersion = (GameVersion)version;

            string legalityReport = "";
            if (almres.Status != LegalizationResult.Regenerated)
            {
                legalityReport = template.SetAnalysis(_sav, pk);
            }
            else if (!la.Valid)
            {
                legalityReport = la.Report();
            }

            var timeInfo = $"Generation time: {timer.Elapsed.TotalSeconds:F2} seconds";
            var context = _contextBuilder.BuildContext(template, almres, la, legalityReport, timeInfo, targetVersion);
            var aiResponse = await _aiService.AnalyzeShowdownSetAsync(input, context, _analysisCts.Token);
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

    private void DisplayFormattedResponse(string aiResponse, bool isLegal, LegalizationResult status)
    {
        TB_Output.Clear();

        var isDarkTheme = TextFormattingHelper.IsDarkTheme(TB_Output.BackColor == SystemColors.Window ? this.BackColor : TB_Output.BackColor);

        var successColor = Color.FromArgb(0, 255, 127);
        var errorColor = Color.FromArgb(255, 99, 71);
        var headerColor = isDarkTheme ? Color.FromArgb(100, 221, 255) : Color.FromArgb(0, 100, 200);
        var statusColor = isDarkTheme ? Color.FromArgb(192, 192, 192) : Color.FromArgb(96, 96, 96);
        var textColor = isDarkTheme ? Color.FromArgb(240, 240, 240) : Color.Black;
        var codeColor = isDarkTheme ? Color.FromArgb(152, 251, 152) : Color.FromArgb(0, 128, 0);

        var isSuccess = isLegal && status == LegalizationResult.Regenerated;
        var headerSymbol = isSuccess ? "✓" : "✗";
        var headerText = isSuccess ? "Your Pokémon is legal!" : "Issues found with your Pokémon:";

        TB_Output.SelectionColor = isSuccess ? successColor : errorColor;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 10, FontStyle.Bold);
        TB_Output.AppendText($"{headerSymbol} {headerText}");

        TB_Output.SelectionColor = statusColor;
        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
        TB_Output.AppendText($" (Status: {status})\r\n");

        TB_Output.SelectionColor = textColor;
        TB_Output.AppendText(new string('═', 60) + "\r\n\r\n");

        TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);

        var sections = TextFormattingHelper.SplitIntoSections(aiResponse);

        foreach (var section in sections)
        {
            if (section.IsHeader)
            {
                TB_Output.SelectionColor = headerColor;
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 10, FontStyle.Bold);
                TB_Output.AppendText(section.Content + "\r\n");
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
            }
            else if (section.IsShowdownSet)
            {
                TB_Output.SelectionColor = codeColor;
                TB_Output.SelectionFont = new Font("Consolas", 9, FontStyle.Regular);
                TB_Output.AppendText(section.Content);
                TB_Output.SelectionFont = new Font(TB_Output.Font.FontFamily, 9, FontStyle.Regular);
            }
            else
            {
                TB_Output.SelectionColor = textColor;
                TB_Output.AppendText(section.Content);
            }
        }
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