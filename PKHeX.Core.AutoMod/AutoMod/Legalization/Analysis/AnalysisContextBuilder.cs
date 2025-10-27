using System;
using System.Linq;
using System.Text;
using static PKHeX.Core.AutoMod.APILegality;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

public sealed class AnalysisContextBuilder(SaveFile sav)
{
    private readonly SaveFile _sav = sav;

    public string BuildContext(RegenTemplate template, AsyncLegalizationResult pk, LegalityAnalysis la, string timeInfo, GameVersion targetVersion)
    {
        var context = $"Game Version: {targetVersion} (Generation {_sav.Generation})\n";
        context += $"Species: {SpeciesName.GetSpeciesName(template.Species, (int)LanguageID.English)}\n";
        context += $"Form: {template.Form}\n";
        context += $"Level: {template.Level}\n";
        context += $"Legalization Status: {pk.Status}\n";
        context += $"Is Legal: {la.Valid}\n";
        context += $"{timeInfo}\n\n";

        // Make the status absolutely clear
        if (pk.Status == LegalizationResult.Failed)
        {
            context += "** LEGALIZATION FAILED - This Pokémon could NOT be made legal! **\n\n";
        }
        else if (pk.Status == LegalizationResult.Timeout)
        {
            context += "** LEGALIZATION TIMED OUT - Generation took too long! **\n\n";
        }
        else if (la.Valid && pk.Status == LegalizationResult.Regenerated)
        {
            context += "** THIS POKÉMON IS LEGAL - No changes needed! **\n\n";
        }
        else
        {
            context += "** ISSUES DETECTED - This Pokémon has problems! **\n\n";
        }

        if (!la.Valid || pk.Status != LegalizationResult.Regenerated)
        {
            context += "== DETAILED ANALYSIS ==\n";
            var validData = GetValidDataForSpecies(template, targetVersion);
            context += validData + "\n";
        }
        else
        {
            context += "== REFERENCE DATA ==\n";
            var validData = GetValidDataForSpecies(template, targetVersion);
            context += validData + "\n";
        }

        // Use PKHeX's new localized formatting system for better legality reports
        if (!la.Valid || pk.Status != LegalizationResult.Regenerated)
        {
            var formattedReport = GetFormattedLegalityReport(la, verbose: true);
            if (!string.IsNullOrWhiteSpace(formattedReport))
            {
                context += "== LEGALITY REPORT ==\n";
                context += formattedReport + "\n\n";
            }

            // Add detailed move analysis
            var moveAnalysis = LegalityReportHelper.GetDetailedMoveAnalysis(la, pk.Created);
            if (!string.IsNullOrWhiteSpace(moveAnalysis))
            {
                context += "== MOVE DETAILS ==\n";
                context += moveAnalysis + "\n";
            }

            // Add detailed ribbon analysis
            var ribbonAnalysis = LegalityReportHelper.GetDetailedRibbonAnalysis(la, pk.Created);
            if (!string.IsNullOrWhiteSpace(ribbonAnalysis))
            {
                context += "== RIBBON DETAILS ==\n";
                context += ribbonAnalysis + "\n";
            }

            // Add invalid checks summary
            var invalidSummary = LegalityReportHelper.GetInvalidChecksSummary(la);
            if (!string.IsNullOrWhiteSpace(invalidSummary))
            {
                context += "== ISSUES BREAKDOWN ==\n";
                context += invalidSummary + "\n";
            }
        }

        // Add encounter details using PKHeX's encounter formatting
        if (la.EncounterMatch is not EncounterInvalid)
        {
            context += "== ENCOUNTER DETAILS ==\n";
            context += GetFormattedEncounterInfo(la) + "\n";
        }

        return context;
    }

    private static string GetFormattedLegalityReport(LegalityAnalysis la, bool verbose = false)
    {
        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        var formatter = new BaseLegalityFormatter();
        return verbose ? formatter.GetReportVerbose(context) : formatter.GetReport(context);
    }

    private static string GetFormattedEncounterInfo(LegalityAnalysis la)
    {
        var sb = new StringBuilder();
        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        var lines = new System.Collections.Generic.List<string>();
        LegalityFormatting.AddEncounterInfo(context, lines);

        foreach (var line in lines)
            sb.AppendLine(line);

        return sb.ToString();
    }

    private string GetValidDataForSpecies(RegenTemplate template, GameVersion targetVersion)
    {
        var sb = new StringBuilder();
        var species = template.Species;
        var form = template.Form;
        var gen = _sav.Generation;
        var version = targetVersion;

        try
        {
            var pi = _sav.Personal.GetFormEntry(species, form);
            if (pi == null)
                return "Unable to find species data.";

            AppendGameAvailability(sb, species, form, version);
            AppendEvolutionRequirements(sb, species, form, version, template.Level);
            AppendMinimumEncounterLevel(sb, species, form, version, template.Level);
            AppendGenderInformation(sb, pi, species, form, template.Gender);
            AppendShinyAvailability(sb, species, form, version, gen, template.Shiny);
            AppendValidAbilities(sb, pi, gen);
            AppendValidMovesWithLevels(sb, species, form, gen, version, template);
            AppendMoveValidation(sb, template);
            AppendValidBalls(sb, species, form, gen, version);
            AppendEncounterTypes(sb, species, form, version);
            AppendEggGroups(sb, pi);
            AppendFormItemRestrictions(sb, species, form);
            AppendNatureRestrictions(sb, species, form);
            AppendSpecialFeatures(sb, species, form, gen, version, pi);
            AppendOverallMinimumLevelRequirement(sb, template, species, form, version);

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting valid data: {ex.Message}";
        }
    }

    private static void AppendGameAvailability(StringBuilder sb, ushort species, byte form, GameVersion version)
    {
        sb.AppendLine("GAME AVAILABILITY:");
        var availableGames = PokemonDataHelper.GetAvailableGames(species, form);
        if (availableGames.Count != 0)
        {
            sb.AppendLine($"- Available in: {string.Join(", ", availableGames)}");
            if (!availableGames.Contains(version.ToString()))
                sb.AppendLine($"  → WARNING: Not available in {version}!");
        }
    }

    private static void AppendEvolutionRequirements(StringBuilder sb, ushort species, byte form, GameVersion version, byte currentLevel)
    {
        sb.AppendLine("\nEVOLUTION REQUIREMENTS:");
        var (evolutionLevel, evolutionInfo) = EvolutionHelper.GetEvolutionLevelRequirement(species, form, version);
        if (evolutionLevel > 0)
        {
            sb.AppendLine($"- Minimum level for this evolution: {evolutionLevel}");
            sb.AppendLine($"  Evolution chain: {evolutionInfo}");
            if (currentLevel < evolutionLevel)
            {
                sb.AppendLine($"  → INVALID: Current level {currentLevel} is too low!");
                sb.AppendLine($"  → Suggestion: Set level to at least {evolutionLevel}");
            }
        }
        else
        {
            sb.AppendLine("- This is a base form or special evolution (no level requirement)");
        }
    }

    private void AppendMinimumEncounterLevel(StringBuilder sb, ushort species, byte form, GameVersion version, byte currentLevel)
    {
        sb.AppendLine("\nMINIMUM ENCOUNTER LEVEL:");
        var minEncounterLevel = EvolutionHelper.GetMinimumEncounterLevel(species, form, version, _sav);
        sb.AppendLine($"- Minimum wild/gift encounter level: {minEncounterLevel}");
        if (currentLevel < minEncounterLevel)
        {
            sb.AppendLine($"  → INVALID: Current level {currentLevel} is below minimum encounter level!");
            sb.AppendLine($"  → Suggestion: Set level to at least {minEncounterLevel}");
        }
    }

    private static void AppendGenderInformation(StringBuilder sb, PersonalInfo pi, ushort species, byte form, byte? templateGender)
    {
        sb.AppendLine("\nGENDER INFORMATION:");
        var genderInfo = PokemonDataHelper.GetGenderInfo(pi, species, form);
        sb.AppendLine(genderInfo);
        if (templateGender.HasValue && !PokemonDataHelper.IsGenderValid(pi, templateGender.Value))
        {
            sb.AppendLine($"  → INVALID: Gender {(templateGender.Value == 0 ? "Male" : templateGender.Value == 1 ? "Female" : "Genderless")} not valid for this species!");
        }
    }

    private static void AppendShinyAvailability(StringBuilder sb, ushort species, byte form, GameVersion version, int gen, bool isShiny)
    {
        sb.AppendLine("\nSHINY AVAILABILITY:");
        var shinyInfo = ValidationHelper.GetShinyAvailability(species, form, version, gen);
        sb.AppendLine(shinyInfo);
        if (isShiny && SimpleEdits.IsShinyLockedSpeciesForm(species, form))
        {
            sb.AppendLine("  → INVALID: This Pokemon is shiny-locked!");
            sb.AppendLine("  → Suggestion: Set shiny to false");
        }
    }

    private static void AppendValidAbilities(StringBuilder sb, PersonalInfo pi, int gen)
    {
        sb.AppendLine("\nVALID ABILITIES:");
        var abilities = PokemonDataHelper.GetValidAbilities(pi, gen);
        foreach (var ability in abilities)
            sb.AppendLine($"- {ability}");
    }

    private static void AppendValidMovesWithLevels(StringBuilder sb, ushort species, byte form, int gen, GameVersion version, RegenTemplate template)
    {
        sb.AppendLine("\nVALID MOVES WITH LEVELS:");
        var movesWithLevels = PokemonDataHelper.GetValidMovesWithLevels(species, form, gen, version);
        if (movesWithLevels.Count > 0)
        {
            sb.AppendLine($"Total valid moves: {movesWithLevels.Count}");
            var moveSample = movesWithLevels.Take(20).ToList();
            foreach (var (move, level) in moveSample)
            {
                if (level > 0)
                    sb.AppendLine($"- {move} (Level {level})");
                else
                    sb.AppendLine($"- {move} (TM/TR/Tutor/Egg)");
            }
            if (movesWithLevels.Count > 20)
                sb.AppendLine($"... and {movesWithLevels.Count - 20} more moves");
        }
    }

    private static void AppendMoveValidation(StringBuilder sb, RegenTemplate template)
    {
        sb.AppendLine("\nMOVE VALIDATION:");
        var movelist = GameInfo.Strings.movelist;
        _ = template.Level;

        for (int i = 0; i < template.Moves.Length; i++)
        {
            var move = template.Moves[i];
            if (move == 0) continue;

            var moveName = movelist[move];
            sb.AppendLine($"- {moveName}: Move validation would occur here");
        }
    }

    private static void AppendValidBalls(StringBuilder sb, ushort species, byte form, int gen, GameVersion version)
    {
        sb.AppendLine("\nVALID BALLS:");
        var validBalls = PokemonDataHelper.GetValidBalls(species, form, gen, version);
        foreach (var ball in validBalls)
            sb.AppendLine($"- {ball}");
    }

    private void AppendEncounterTypes(StringBuilder sb, ushort species, byte form, GameVersion version)
    {
        sb.AppendLine("\nVALID ENCOUNTER TYPES:");
        var encounters = EncounterHelper.GetValidEncounterTypes(species, form, version, _sav);
        foreach (var enc in encounters)
            sb.AppendLine($"- {enc}");
    }

    private static void AppendEggGroups(StringBuilder sb, PersonalInfo pi)
    {
        sb.AppendLine("\nEGG GROUPS:");
        var eggGroups = PokemonDataHelper.GetEggGroups(pi);
        sb.AppendLine($"- {eggGroups}");
    }

    private static void AppendFormItemRestrictions(StringBuilder sb, ushort species, byte form)
    {
        if (ValidationHelper.HasFormItemRestrictions(species))
        {
            sb.AppendLine("\nFORM-ITEM RESTRICTIONS:");
            var itemRestrictions = ValidationHelper.GetFormItemRestrictions(species, form);
            sb.AppendLine(itemRestrictions);
        }
    }

    private static void AppendNatureRestrictions(StringBuilder sb, ushort species, byte form)
    {
        if (ValidationHelper.HasNatureRestrictions(species, form))
        {
            sb.AppendLine("\nNATURE RESTRICTIONS:");
            var natureRestrictions = ValidationHelper.GetNatureRestrictions(species, form);
            sb.AppendLine(natureRestrictions);
        }
    }

    private static void AppendSpecialFeatures(StringBuilder sb, ushort species, byte form, int gen, GameVersion version, PersonalInfo pi)
    {
        if (gen == 8 && Gigantamax.CanToggle(species, form, species, form))
        {
            sb.AppendLine("\nGIGANTAMAX:");
            sb.AppendLine("- Can have Gigantamax factor");
        }

        if (version == GameVersion.PLA)
        {
            sb.AppendLine("\nALPHA AVAILABILITY:");
            sb.AppendLine("- Can be Alpha in Legends: Arceus");
        }

        if (pi is IPersonalAbility12H pah && pah.AbilityH != 0)
        {
            sb.AppendLine($"\nHIDDEN ABILITY: {GameInfo.Strings.abilitylist[pah.AbilityH]} - ");
            sb.Append(PokemonDataHelper.CanHaveHiddenAbility(species, form, gen) ? "Available" : "Not available in this generation");
            sb.AppendLine();
        }
    }

    private void AppendOverallMinimumLevelRequirement(StringBuilder sb, RegenTemplate template, ushort species, byte form, GameVersion version)
    {
        var (evolutionLevel, _) = EvolutionHelper.GetEvolutionLevelRequirement(species, form, version);
        var minEncounterLevel = EvolutionHelper.GetMinimumEncounterLevel(species, form, version, _sav);
        var minRequiredLevel = template.Level;

        var overallMinLevel = Math.Max(Math.Max(evolutionLevel, minEncounterLevel), minRequiredLevel);
        if (overallMinLevel > template.Level)
        {
            sb.AppendLine($"\n** MINIMUM LEVEL REQUIREMENT: {overallMinLevel} **");
            sb.AppendLine($"Current level {template.Level} is too low due to:");
            if (evolutionLevel > template.Level)
                sb.AppendLine($"- Evolution requirement: Level {evolutionLevel}+");
            if (minEncounterLevel > template.Level)
                sb.AppendLine($"- Encounter requirement: Level {minEncounterLevel}+");
            if (minRequiredLevel > template.Level && minRequiredLevel > evolutionLevel && minRequiredLevel > minEncounterLevel)
                sb.AppendLine($"- Move requirement: Level {minRequiredLevel}+");
        }
    }
}