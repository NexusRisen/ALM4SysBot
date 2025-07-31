using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using AutoModPlugins.GUI;

namespace AutoModPlugins;

public class AIAnalysisPlugin : AutoModPlugin
{
    public override string Name => "AI Analysis";
    public override int Priority => 1;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var ctrl = new ToolStripMenuItem(Name)
        {
            Image = Resources.ai, 
            ShortcutKeys = Keys.Control | Keys.Q,
        };
        ctrl.Click += OpenAIAnalysis;
        ctrl.Name = "Menu_AIAnalysis";
        modmenu.DropDownItems.Add(ctrl);
    }

    private void OpenAIAnalysis(object? sender, EventArgs e)
    {
        if (!_settings.EnableAIAnalysis)
        {
            var result = WinFormsUtil.Prompt(MessageBoxButtons.YesNo,
                "AI Analysis is disabled. Would you like to enable it?");

            if (result == DialogResult.Yes)
            {
                _settings.EnableAIAnalysis = true;
                _settings.Save();
            }
            else
            {
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.OpenAIApiKey))
        {
            using var keyDialog = new APIKeyDialog();
            if (keyDialog.ShowDialog() == DialogResult.OK)
            {
                _settings.OpenAIApiKey = keyDialog.ApiKey;
                _settings.Save();
            }
            else
            {
                return;
            }
        }

        using var form = new AIAnalysisForm(_settings, SaveFileEditor.SAV);
        form.ShowDialog();
    }
}