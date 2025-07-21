using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

internal static class ValidationHelper
{
    public static string GetShinyAvailability(ushort species, byte form, GameVersion version, int generation)
    {
        if (SimpleEdits.IsShinyLockedSpeciesForm(species, form))
        {
            return "- Shiny-locked (cannot be shiny)";
        }

        if (generation < 2)
        {
            return "- Shinies not available in Generation 1";
        }

        var restrictions = new List<string>();

        switch ((Species)species)
        {
            case Species.Manaphy:
                restrictions.Add("Special shiny mechanics (Ranger egg)");
                break;
            case Species.Toxtricity:
                restrictions.Add("Shiny form depends on nature");
                break;
        }

        var mightyGender = SimpleEdits.GetMightyRaidGender(species, form);
        if (mightyGender.HasValue)
        {
            var genderStr = mightyGender.Value switch
            {
                0 => "Male",
                1 => "Female",
                2 => "Genderless",
                _ => "Unknown"
            };
            restrictions.Add($"Mighty Raid: Gender locked to {genderStr}");
        }

        if (restrictions.Any())
        {
            return $"- Can be shiny with restrictions:\n  {string.Join("\n  ", restrictions)}";
        }

        return "- Can be shiny";
    }

    public static bool HasFormItemRestrictions(ushort species) =>
        species switch
        {
            (ushort)Species.Arceus => true,
            (ushort)Species.Silvally => true,
            (ushort)Species.Genesect => true,
            (ushort)Species.Giratina => true,
            (ushort)Species.Dialga => true,
            (ushort)Species.Palkia => true,
            (ushort)Species.Ogerpon => true,
            _ => false
        };

    public static string GetFormItemRestrictions(ushort species, byte form) =>
        species switch
        {
            (ushort)Species.Arceus => form == 0 ? "- No item required for Normal form" : $"- Form {form} requires specific plate item",
            (ushort)Species.Silvally => form == 0 ? "- No item required for Normal form" : $"- Form {form} requires Memory item",
            (ushort)Species.Genesect => form == 0 ? "- No item required for base form" : $"- Form {form} requires Drive item",
            (ushort)Species.Giratina => form == 1 ? "- Origin form requires Griseous Orb (Gen 4-7) or Griseous Core (Gen 8+)" : "- Altered form (no item required)",
            (ushort)Species.Dialga => form == 1 ? "- Origin form requires Adamant Crystal" : "- Base form (no item required)",
            (ushort)Species.Palkia => form == 1 ? "- Origin form requires Lustrous Globe" : "- Base form (no item required)",
            (ushort)Species.Ogerpon => form == 0 ? "- Teal Mask form (no item required)" : "- Mask forms require specific mask items",
            _ => "- No specific item restrictions"
        };

    public static bool HasNatureRestrictions(ushort species, byte form) =>
        species == (ushort)Species.Toxtricity;

    public static string GetNatureRestrictions(ushort species, byte form)
    {
        if (species == (ushort)Species.Toxtricity)
        {
            if (form == 0)
            {
                return "- Amped form requires: Adamant, Brave, Docile, Hardy, Hasty, Impish, Jolly, Lax, Naive, Naughty, Quirky, Rash, or Sassy nature";
            }
            else
            {
                return "- Low Key form requires: Bashful, Bold, Calm, Careful, Gentle, Lonely, Mild, Modest, Quiet, Relaxed, Serious, or Timid nature";
            }
        }
        return "- No nature restrictions";
    }
}