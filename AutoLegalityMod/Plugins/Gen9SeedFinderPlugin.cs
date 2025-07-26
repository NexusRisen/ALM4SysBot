using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoModPlugins;

/// <summary>
/// Scarlet/Violet Seed Finding Plugin
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
            ToolTipText = "Search for Gen 9 RNG seeds that produce specific Pokémon",
            Image = CreateTeraCrystalIcon()
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

    private static Bitmap CreateTeraCrystalIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var points = new Point[]
            {
                new(8, 2),
                new(13, 8),
                new(8, 14),
                new(3, 8)
            };

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 16, 16),
                Color.FromArgb(200, 138, 43, 226),
                Color.FromArgb(200, 75, 0, 130),
                45f))
            {
                g.FillPolygon(brush, points);
            }

            g.DrawPolygon(new Pen(Color.DarkViolet, 1), points);
        }
        return bmp;
    }
}