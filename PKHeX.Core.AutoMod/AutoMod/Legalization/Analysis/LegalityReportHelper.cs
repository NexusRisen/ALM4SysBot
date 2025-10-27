using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis;

public static class LegalityReportHelper
{
    public static string GetDetailedMoveAnalysis(LegalityAnalysis la, PKM pk)
    {
        var sb = new StringBuilder();
        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        sb.AppendLine("MOVE ANALYSIS:");

        // Current moves
        var moves = la.Info.Moves;
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (!move.IsParsed)
                continue;

            var moveInfo = context.FormatMove(move, i + 1, pk.Format);
            sb.AppendLine($"- {moveInfo}");

            if (!move.Valid)
            {
                sb.AppendLine($"  → Issue: {move.Summary(context)}");
            }
        }

        // Relearn moves for formats that support them
        if (pk.Format >= 6)
        {
            sb.AppendLine("\nRELEARN MOVES:");
            var relearn = la.Info.Relearn;
            for (int i = 0; i < relearn.Length; i++)
            {
                var move = relearn[i];
                if (!move.IsParsed)
                    continue;

                var moveInfo = context.FormatRelearn(move, i + 1);
                sb.AppendLine($"- {moveInfo}");

                if (!move.Valid)
                {
                    sb.AppendLine($"  → Issue: {move.Summary(context)}");
                }
            }
        }

        return sb.ToString();
    }

    public static string GetDetailedRibbonAnalysis(LegalityAnalysis la, PKM pk)
    {
        var sb = new StringBuilder();

        if (pk is not IRibbonIndex ribbons)
            return string.Empty;

        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        // Get ribbon verification messages from the analysis results
        var ribbonResults = la.Results.Where(r => r.Identifier == CheckIdentifier.Ribbon);
        foreach (var result in ribbonResults)
        {
            if (!result.Valid)
            {
                sb.AppendLine("RIBBON ANALYSIS:");
                sb.AppendLine(context.Humanize(result));
            }
        }

        // List all ribbons the Pokémon has
        var hasRibbons = new List<string>();
        var allRibbons = RibbonInfo.GetRibbonInfo(pk);

        foreach (var ribbon in allRibbons.Where(r => r.HasRibbon))
        {
            // Format the ribbon name from the property name
            var ribbonName = ribbon.Name;
            if (ribbonName.StartsWith("Ribbon"))
                ribbonName = ribbonName[6..]; // Remove "Ribbon" prefix

            // Add spaces between words (e.g., "ChampionKalos" -> "Champion Kalos")
            ribbonName = System.Text.RegularExpressions.Regex.Replace(ribbonName, "([a-z])([A-Z])", "$1 $2");

            if (ribbon.Type == RibbonValueType.Byte && ribbon.RibbonCount > 0)
                ribbonName += $" (Count: {ribbon.RibbonCount})";

            hasRibbons.Add(ribbonName);
        }

        if (hasRibbons.Count > 0)
        {
            sb.AppendLine("\nCURRENT RIBBONS:");
            foreach (var ribbon in hasRibbons)
                sb.AppendLine($"- {ribbon}");
        }

        return sb.ToString();
    }

    public static string GetInvalidChecksSummary(LegalityAnalysis la)
    {
        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        var invalidChecks = la.Results.Where(c => !c.Valid).ToList();
        if (invalidChecks.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("LEGALITY ISSUES SUMMARY:");

        // Group by identifier for better organization
        var grouped = invalidChecks.GroupBy(c => c.Identifier);

        foreach (var group in grouped)
        {
            sb.AppendLine($"\n{group.Key}:");
            foreach (var check in group)
            {
                var message = context.Humanize(check, verbose: false);
                sb.AppendLine($"- {message}");
            }
        }

        return sb.ToString();
    }
}