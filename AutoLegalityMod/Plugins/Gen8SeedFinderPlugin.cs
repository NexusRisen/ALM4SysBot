using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoModPlugins;

/// <summary>
/// Sword/Shield Max Raid Den Seed Finding Plugin
/// </summary>
public class Gen8SeedFinderPlugin : AutoModPlugin
{
    public override string Name => "Gen 8 Seed Finder";
    public override int Priority => 2;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var ctrl = new ToolStripMenuItem(Name)
        {
            ShortcutKeys = Keys.Control | Keys.Shift | Keys.R,
            ToolTipText = "Search for Gen 8 Max Raid Den seeds that produce specific Pokémon",
            Image = CreateRaidCrystalIcon()
        };
        ctrl.Click += OpenSeedFinderForm;
        ctrl.Name = "Menu_Gen8SeedFinder";
        modmenu.DropDownItems.Add(ctrl);
    }

    private void OpenSeedFinderForm(object? sender, EventArgs e)
    {
        var sav = SaveFileEditor.SAV;
        if (sav.Generation != 8)
        {
            WinFormsUtil.Alert("This tool only works with Generation 8 (Sword/Shield) save files!");
            return;
        }
        try
        {
            using var form = new GUI.Gen8SeedFinderForm(SaveFileEditor, PKMEditor);
            form.ShowDialog();
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"Error opening seed finder: {ex.Message}");
        }
    }

    private static Bitmap CreateRaidCrystalIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Create a diamond/crystal shape
            var points = new Point[]
            {
                new(8, 2),   // Top
                new(13, 8),  // Right
                new(8, 14),  // Bottom
                new(3, 8)    // Left
            };

            // Blue to pink gradient
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 16, 16),
                Color.FromArgb(200, 30, 144, 255),  // Dodger Blue
                Color.FromArgb(200, 255, 20, 147),  // Deep Pink
                45f))
            {
                g.FillPolygon(brush, points);
            }

            // Draw outline with darker blue
            using (var pen = new Pen(Color.FromArgb(255, 0, 100, 200), 1))
            {
                g.DrawPolygon(pen, points);
            }

            // Add inner shine effect
            var shinePoints = new Point[]
            {
                new(8, 4),
                new(10, 6),
                new(8, 8),
                new(6, 6)
            };

            using var shineBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
            g.FillPolygon(shineBrush, shinePoints);
        }
        return bmp;
    }
}