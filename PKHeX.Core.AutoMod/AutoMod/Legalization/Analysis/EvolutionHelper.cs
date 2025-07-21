using System;
using System.Linq;
using System.Text.RegularExpressions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

internal static class EvolutionHelper
{
    private static readonly Regex LevelPattern = new(@"\((\d+)\)", RegexOptions.Compiled);

    public static (int Level, string Info) GetEvolutionLevelRequirement(ushort species, byte form, GameVersion version)
    {
        try
        {
            var evoString = PokemonEvolutionHelper.GetEvolutionString(species, form, version);

            if (string.IsNullOrEmpty(evoString))
                return (0, "Base form with no evolutions");

            var match = LevelPattern.Match(evoString);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int level))
            {
                return (level, evoString);
            }

            return (0, $"Special evolution: {evoString}");
        }
        catch
        {
            return (0, "Unable to determine evolution requirements");
        }
    }

    public static int GetMinimumEncounterLevel(ushort species, byte form, GameVersion version, SaveFile sav)
    {
        try
        {
            var blank = EntityBlank.GetBlank((byte)sav.Generation);
            blank.Species = species;
            blank.Form = form;

            var la = new LegalityAnalysis(blank);
            var encounters = EncounterGenerator.GetEncounters(blank, la.Info);

            int minLevel = 100;
            bool foundEncounter = false;

            foreach (var enc in encounters)
            {
                if (enc.IsEgg || enc is IEncounterEgg)
                    continue;

                int encMinLevel = enc.LevelMin;

                if (enc.LevelMin == enc.LevelMax && enc.LevelMin > 0)
                {
                    encMinLevel = enc.LevelMin;
                }

                if (encMinLevel < minLevel)
                {
                    minLevel = encMinLevel;
                    foundEncounter = true;
                }
            }

            if (!foundEncounter)
                return 1;

            return minLevel;
        }
        catch
        {
            return 1;
        }
    }
}