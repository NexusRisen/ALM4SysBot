using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

internal static class EncounterHelper
{
    public static List<string> GetValidEncounterTypes(ushort species, byte form, GameVersion version, SaveFile sav)
    {
        var types = new HashSet<string>();

        var blank = EntityBlank.GetBlank((byte)sav.Generation);
        blank.Species = species;
        blank.Form = form;

        var la = new LegalityAnalysis(blank);
        var encounters = EncounterGenerator.GetEncounters(blank, la.Info);

        foreach (var enc in encounters)
        {
            var type = enc switch
            {
                IEncounterEgg => "Egg",
                _ when enc.Name.Contains("Egg") => "Egg",
                _ when enc.Name.Contains("Static") => "Static/Gift",
                _ when enc.Name.Contains("Trade") => "In-Game Trade",
                _ when enc.Name.Contains("Wild") || enc.Name.Contains("Slot") => "Wild",
                _ => enc.Name
            };
            types.Add(type);
        }

        return [.. types.OrderBy(t => t)];
    }
}