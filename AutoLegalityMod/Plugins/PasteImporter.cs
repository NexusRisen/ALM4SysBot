using System;
using System.IO;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Core.Enhancements;

namespace AutoModPlugins;

/// <summary>
/// Main Plugin with clipboard import calls
/// </summary>
public class PasteImporter : AutoModPlugin
{
    // TODO: Check for Auto-Legality Mod Updates
    public override string Name => "Import with Auto-Legality Mod";
    public override int Priority => 0;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var ctrl = new ToolStripMenuItem(Name)
        {
            Image = Resources.autolegalitymod,
            ShortcutKeys = Keys.Control | Keys.I,
        };
        ctrl.Click += ImportPaste;
        ctrl.Name = "Menu_PasteImporter";
        modmenu.DropDownItems.Add(ctrl);
        ToolStripItem parent = modmenu.OwnerItem?? ctrl;
        var currparent = parent.GetCurrentParent()??throw new Exception("Parent not found");
        var form = (currparent.Parent ?? throw new Exception("Parent not found")).FindForm() ?? throw new Exception("Form not found");
        form.Icon = Resources.icon;
        form.KeyDown += Downkey;
        ShowdownSetLoader.PKMEditor = PKMEditor;
        ShowdownSetLoader.SaveFileEditor = SaveFileEditor;
    }

    private void Downkey(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.NumPad6 or Keys.D6 or Keys.A) || !e.Control)
            return;
        PKM[] result = [];
        var sav = SaveFileEditor.SAV;
        if (e.KeyCode is not Keys.A)
        {
            if (WinFormsUtil.Prompt(MessageBoxButtons.OKCancel, "Generate 6 Random Pokemon?") != DialogResult.OK)
                return;
            APILegality.RandTypes = _settings.RandomTypes;
            result = sav.GetSixRandomMons();
        }
        else
        {
            var text = GetTextShowdownData();
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Make an Egg from this set?", text) != DialogResult.Yes)
                return;
            ShowdownSet showdownSet = new(text);
            var made = sav.GenerateEgg(showdownSet, out _);
            PKMEditor.PopulateFields(made);
        }

        const int slotIndexStart = 0;
        var slot = slotIndexStart - 1;
        foreach (var pk in result)
        {
            slot = sav.NextOpenBoxSlot(slot);
            if (slot == -1)
                break;
            sav.SetBoxSlotAtIndex(pk, slot);
        }
        SaveFileEditor.ReloadSlots();
    }

    private static void ImportPaste(object? sender, EventArgs e)
    {
        // Check for showdown data in clipboard
        var text = GetTextShowdownData();
        if (string.IsNullOrWhiteSpace(text))
            return;

        ShowdownSetLoader.Import(text);
    }

    /// <summary>
    /// Check whether the showdown text is supposed to be loaded via a text file. If so, set the clipboard to its contents.
    /// </summary>
    /// <returns>output boolean that tells if the data provided is valid or not</returns>
    private static string? GetTextShowdownData()
    {
        bool skipClipboardCheck = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        if (!skipClipboardCheck && Clipboard.ContainsText())
        {
            var txt = Clipboard.GetText();
            if (ShowdownUtil.IsTextShowdownData(txt))
                return txt;
            if (ShowdownTeam.IsURL(txt, out var url) && ShowdownTeam.TryGetSets(url, out var content))
                return content;
            else if (PokepasteTeam.IsURL(txt, out url) && PokepasteTeam.TryGetSets(url, out content))
                return content;
        }
        if (!WinFormsUtil.OpenSAVPKMDialog(["txt"], out var path))
        {
            WinFormsUtil.Alert("No data provided.");
            return null;
        }

        if (path == null)
        {
            WinFormsUtil.Alert("Path invalid.");
            return null;
        }

        var text = File.ReadAllText(path).TrimEnd();
        if (ShowdownUtil.IsTextShowdownData(text))
        {
            return text;
        }

        WinFormsUtil.Alert("Text file with invalid data provided. Please provide a text file with proper Showdown data");
        return null;
    }
}
