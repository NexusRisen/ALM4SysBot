using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

internal static class PokemonEvolutionHelper
{
    internal static string GetEvolutionString(int species, byte form, GameVersion game)
    {
        var context = GetEntityContext(game);
        var evoTree = EvolutionTree.GetEvolutionTree(context);

        var evolutions = new List<(int Species, byte Form, int Level)>();
        GetDirectEvolutions((ushort)species, form, evoTree, evolutions);

        var preEvolutions = new List<(int Species, byte Form, int Level)>();
        if (evolutions.Count == 0)
        {
            var node = evoTree.Reverse.GetReverse((ushort)species, form);
            if (node.First.Species != 0)
            {
                int level = GetEvolutionLevelFromMethod(node.First.Method);
                preEvolutions.Add((node.First.Species, node.First.Form, level));
            }
        }

        if (evolutions.Count > 0)
        {
            return FormatEvolutions(evolutions);
        }
        else if (preEvolutions.Count > 0)
        {
            return FormatPreEvolution(preEvolutions[0]);
        }

        return string.Empty;
    }

    private static EntityContext GetEntityContext(GameVersion game)
    {
        return game switch
        {
            GameVersion.SV => EntityContext.Gen9,
            GameVersion.PLA => EntityContext.Gen8a,
            GameVersion.BDSP => EntityContext.Gen8b,
            GameVersion.SWSH => EntityContext.Gen8,
            GameVersion.GG => EntityContext.Gen7b,
            _ => EntityContext.Gen9
        };
    }

    private static void GetDirectEvolutions(ushort species, byte form, EvolutionTree evoTree, List<(int Species, byte Form, int Level)> evolutions)
    {
        var evos = evoTree.Forward.GetForward(species, form);
        foreach (var evo in evos.Span)
        {
            int level = GetEvolutionLevel(evo);
            byte evoForm = (byte)evo.Form;
            evolutions.Add((evo.Species, evoForm, level));

            GetDirectEvolutions((ushort)evo.Species, evoForm, evoTree, evolutions);
        }
    }

    private static void GetPreEvolutions(ushort species, byte form, EvolutionTree evoTree, List<(int Species, byte Form, int Level)> preEvolutions)
    {
        var node = evoTree.Reverse.GetReverse(species, form);
        var first = node.First;
        if (first.Species == 0)
            return;

        int level = GetEvolutionLevelFromMethod(first.Method);
        preEvolutions.Add((first.Species, first.Form, level));

        GetPreEvolutions(first.Species, first.Form, evoTree, preEvolutions);
    }

    private static int GetEvolutionLevelFromMethod(EvolutionMethod method)
    {
        if (method.Level > 0)
            return method.Level;
        if (method.Method == EvolutionType.LevelUp && method.Argument > 0)
            return method.Argument;
        return 0;
    }

    private static int GetEvolutionLevel(EvolutionMethod evo)
    {
        if (evo.Level > 0)
            return evo.Level;
        if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
            return evo.Argument;
        return 0;
    }

    private static string FormatPreEvolution((int Species, byte Form, int Level) preEvolution)
    {
        var strings = GameInfo.GetStrings("en");
        if (strings == null)
            return string.Empty;

        var (species, form, level) = preEvolution;
        string speciesName = strings.Species[species];

        if (form > 0)
        {
            var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
            if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
            {
                speciesName = $"{speciesName}-{formNames[form]}";
            }
        }

        if (level > 0)
        {
            return $"{speciesName} ({level})";
        }

        return speciesName;
    }

    private static string FormatEvolutions(List<(int Species, byte Form, int Level)> evolutions)
    {
        var sb = new StringBuilder();
        var strings = GameInfo.GetStrings("en");
        if (strings == null)
            return string.Empty;

        for (int i = 0; i < evolutions.Count; i++)
        {
            var (species, form, level) = evolutions[i];

            if (i > 0)
                sb.Append(", ");

            string speciesName = strings.Species[species];
            if (form > 0)
            {
                var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
                if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
                {
                    speciesName = $"{speciesName}-{formNames[form]}";
                }
            }

            sb.Append(speciesName);

            if (level > 0)
            {
                sb.Append($" ({level})");
            }
        }

        return sb.ToString();
    }
}
