using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins;

/// <summary>
/// Simplified plugin - just the seed finder without other Tera tools
/// </summary>
public class Gen9SeedFinderPlugin : AutoModPlugin
{
    public override string Name => "Gen 9 Seed Finder";
    public override int Priority => 1;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var ctrl = new ToolStripMenuItem(Name)
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.F,
            ToolTipText = "Search for Gen 9 RNG seeds that produce specific Pokémon"
        };

        ctrl.Click += OpenSeedFinderForm;
        ctrl.Name = "Menu_Gen9SeedFinder";
        modmenu.DropDownItems.Add(ctrl);
    }

    private void OpenSeedFinderForm(object? sender, EventArgs e)
    {
        var sav = SaveFileEditor.SAV;
        if (sav.Generation != 9)
        {
            WinFormsUtil.Alert("This tool only works with Generation 9 (Scarlet/Violet) save files!");
            return;
        }

        try
        {
            using var form = new GUI.Gen9SeedFinderForm(SaveFileEditor, PKMEditor);
            form.ShowDialog();
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Error opening seed finder: {ex.Message}");
        }
    }
}