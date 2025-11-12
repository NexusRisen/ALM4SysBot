using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using static PKHeX.Core.Species;

namespace PKHeX.Core.AutoMod;

/// <summary>
/// Miscellaneous enhancement methods
/// </summary>
public static class ModLogic
{
    /// <summary>
    /// Exports the <see cref="SaveFile.CurrentBox"/> to <see cref="ShowdownSet"/> as a single string.
    /// </summary>
    /// <param name="provider">Save File to export from</param>
    /// <returns>Concatenated string of all sets in the current box.</returns>
    public static string GetRegenSetsFromBoxCurrent(this ISaveFileProvider provider) => GetRegenSetsFromBox(provider.SAV, provider.CurrentBox);

    /// <summary>
    /// Exports the <see cref="box"/> to <see cref="ShowdownSet"/> as a single string.
    /// </summary>
    /// <param name="sav">Save File to export from</param>
    /// <param name="box">Box to export from</param>
    /// <returns>Concatenated string of all sets in the specified box.</returns>
    public static string GetRegenSetsFromBox(this SaveFile sav, int box)
    {
        Span<PKM> data = sav.GetBoxData(box);
        var sep = Environment.NewLine + Environment.NewLine;
        return data.GetRegenSets(sep);
    }

    /// <summary>
    /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
    /// </summary>
    /// <param name="tr">Trainer Data to use in generating the encounter</param>
    /// <param name="species">Species ID to generate</param>
    /// <param name="form">Form to generate; if left null, picks first encounter</param>
    /// <param name="shiny"></param>
    /// <param name="alpha"></param>
    /// <param name="nativeOnly"></param>
    /// <param name="pk">Result legal pkm</param>
    /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
    public static bool GetRandomEncounter(this ITrainerInfo tr, ushort species, byte form, bool shiny, bool alpha, bool nativeOnly, out PKM? pk)
    {
        var blank = EntityBlank.GetBlank(tr);
        pk = GetRandomEncounter(blank, tr, species, form, shiny, alpha, nativeOnly);
        if (pk is null)
            return false;

        pk = EntityConverter.ConvertToType(pk, blank.GetType(), out _);
        return pk is not null;
    }

    /// <summary>
    /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
    /// </summary>
    /// <param name="blank">Template data that will have its properties modified</param>
    /// <param name="tr">Trainer Data to use in generating the encounter</param>
    /// <param name="species">Species ID to generate</param>
    /// <param name="form">Form to generate; if left null, picks first encounter</param>
    /// <param name="shiny"></param>
    /// <param name="alpha"></param>
    /// <param name="nativeOnly"></param>
    /// <returns>Result legal pkm, null if data should be ignored.</returns>
    private static PKM? GetRandomEncounter(PKM blank, ITrainerInfo tr, ushort species, byte form, bool shiny, bool alpha, bool nativeOnly)
    {
        blank.Species = species;
        blank.Gender = blank.GetSaneGender();
        if (species is ((ushort)Meowstic) or ((ushort)Indeedee))
        {
            blank.Gender = form;
            blank.Form = blank.Gender;
        }
        else
        {
            blank.Form = form;
        }

        var template = EntityBlank.GetBlank(tr.Generation, tr.Version);
        var item = GetFormSpecificItem(tr.Version, tr.Generation, blank.Species, blank.Form);
        if (item is not null)
            blank.HeldItem = (int)item;

        if (blank is { Species: (ushort)Keldeo, Form: 1 })
            blank.Move1 = (ushort)Move.SecretSword;

        if (blank.GetIsFormInvalid(tr.Generation, tr.Context, blank.Form))
            return null;

        var setText = new ShowdownSet(blank).Text.Split('\r')[0];
        if (species == (ushort)Zygarde && form == 2)
            setText += "-C";
        if (species == (ushort)Zygarde && form == 3)
            setText += "-50%-C";
        if ((shiny && !SimpleEdits.IsShinyLockedSpeciesForm(species, blank.Form)) || (shiny && tr.Generation != 6 && blank.Species != (ushort)Vivillon && blank.Form != 18))
            setText += Environment.NewLine + "Shiny: Yes";

        if (template is IAlphaReadOnly && alpha && tr.Version == GameVersion.PLA)
            setText += Environment.NewLine + "Alpha: Yes";

        var sset = new ShowdownSet(setText);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };
        template.ApplySetDetails(set);

        var t = template.Clone();
        var almres = tr.TryAPIConvert(set, t, nativeOnly);
        var pk = almres.Created;
        var success = almres.Status;

        if (success == LegalizationResult.Regenerated)
            return pk;

        sset = new ShowdownSet(setText.Split('\r')[0]);
        set = new RegenTemplate(sset) { Nickname = string.Empty };
        template.ApplySetDetails(set);

        t = template.Clone();
        almres = tr.TryAPIConvert(set, t, nativeOnly);
        pk = almres.Created;
        success = almres.Status;
        if (pk.Species is (ushort)Gholdengo)
        {
            pk.SetSuggestedFormArgument();
            pk.SetSuggestedMoves();
            success = LegalizationResult.Regenerated;
        }

        return success == LegalizationResult.Regenerated ? pk : null;
    }

    private static bool GetIsFormInvalid(this PKM pk, byte generation, EntityContext ctx, byte form)
    {
        var species = pk.Species;
        switch ((Species)species)
        {
            case Floette when form == 5 && ctx < EntityContext.Gen9a:
            case Shaymin or Furfrou or Hoopa when form != 0 && generation <= 6:
            case Arceus when generation == 4 && form == 9: // ??? form
            case Scatterbug or Spewpa when form == 19:
                return true;
        }
        if (FormInfo.IsBattleOnlyForm(species, form, generation))
            return true;

        if (form == 0)
            return false;

        if (species == 25 || SimpleEdits.AlolanOriginForms.Contains(species))
        {
            if (generation >= 7 && pk.Generation is < 7 and not 0)
                return true;
        }

        return false;
    }

    private static int? GetFormSpecificItem(GameVersion game, byte generation, ushort species, byte form)
    {
        if (game == GameVersion.PLA)
            return null;

        return species switch
        {
            (ushort)Arceus => generation != 4 || form < 9 ? SimpleEdits.GetArceusHeldItemFromForm(form) : SimpleEdits.GetArceusHeldItemFromForm(form - 1),
            (ushort)Silvally => SimpleEdits.GetSilvallyHeldItemFromForm(form),
            (ushort)Genesect => SimpleEdits.GetGenesectHeldItemFromForm(form),
            (ushort)Giratina => form == 1 && generation < 9 ? 112 : form == 1 ? 1779 : null, // Griseous Orb
            (ushort)Zacian => form == 1 ? 1103 : null, // Rusted Sword
            (ushort)Zamazenta => form == 1 ? 1104 : null, // Rusted Shield
            _ => null,
        };
    }

    /// <summary>
    /// Legalizes all <see cref="PKM"/> in the specified <see cref="box"/>.
    /// </summary>
    /// <param name="sav">Save File to legalize</param>
    /// <param name="box">Box to legalize</param>
    /// <returns>Count of Pokémon that are now legal.</returns>
    public static int LegalizeBox(this SaveFile sav, int box)
    {
        if ((uint)box >= sav.BoxCount)
            return -1;

        var data = sav.GetBoxData(box);
        var ctr = sav.LegalizeAll(data);
        if (ctr > 0)
            sav.SetBoxData(data, box);

        return ctr;
    }

    /// <summary>
    /// Legalizes all <see cref="PKM"/> in all boxes.
    /// </summary>
    /// <param name="sav">Save File to legalize</param>
    /// <returns>Count of Pokémon that are now legal.</returns>
    public static int LegalizeBoxes(this SaveFile sav)
    {
        if (!sav.HasBox)
            return -1;

        var ctr = 0;
        for (int i = 0; i < sav.BoxCount; i++)
        {
            var result = sav.LegalizeBox(i);
            if (result < 0)
                return result;

            ctr += result;
        }
        return ctr;
    }

    /// <summary>
    /// Legalizes all <see cref="PKM"/> in the provided <see cref="data"/>.
    /// </summary>
    /// <param name="sav">Save File context to legalize with</param>
    /// <param name="data">Data to legalize</param>
    /// <returns>Count of Pokémon that are now legal.</returns>
    public static int LegalizeAll(this SaveFile sav, IList<PKM> data)
    {
        var ctr = 0;
        for (int i = 0; i < data.Count; i++)
        {
            var pk = data[i];
            if (pk.Species == 0 || new LegalityAnalysis(pk).Valid)
                continue;

            var result = sav.Legalize(pk);
            result.Heal();
            if (!new LegalityAnalysis(result).Valid)
                continue; // failed to legalize

            data[i] = result;
            ctr++;
        }

        return ctr;
    }

    public static PKM[] GetSixRandomMons(this ITrainerInfo tr) =>
        tr.GetSixRandomMons(GameData.GetPersonal(tr.Version));

    /// <summary>
    /// Generates a team of six random legal Pokémon for the given trainer and personal table.
    /// </summary>
    /// <param name="tr">Trainer information to use for generating Pokémon.</param>
    /// <param name="personal">Personal table containing species and form data.</param>
    /// <returns>An array of six legal <see cref="PKM"/> objects.</returns>
    public static PKM[] GetSixRandomMons(this ITrainerInfo tr, IPersonalTable personal)
    {
        var result = new PKM[6];
        Span<int> ivs = stackalloc int[6];
        Span<ushort> selectedSpecies = stackalloc ushort[6];
        var rng = Util.Rand;

        int ctr = 0;
        MoveType[] types = APILegality.RandTypes;
        byte destGeneration = tr.Generation;
        var destVersion = tr.Version;

        while (ctr != 6)
        {
            var spec = (ushort)rng.Next(personal.MaxSpeciesID);

            if (selectedSpecies.Contains(spec))
                continue;

            byte form = 0;
            var rough = EntityBlank.GetBlank(tr);
            rough.Species = spec;
            rough.Gender = rough.GetSaneGender();

            if (!personal.IsSpeciesInGame(spec))
                continue;

            if (types.Length != 0)
            {
                var pi = rough.PersonalInfo;
                if (!types.Contains((MoveType)pi.Type1) || !types.Contains((MoveType)pi.Type2))
                    continue;
            }

            var formnumb = personal[spec].FormCount;
            if (formnumb == 1)
                formnumb = (byte)FormConverter.GetFormList(spec, GameInfo.Strings.types, GameInfo.Strings.forms, GameInfo.GenderSymbolUnicode, tr.Context).Length;

            do
            {
                if (formnumb == 0)
                    break;
                form = rough.Form = (byte)rng.Next(formnumb);
            }
            while (!personal.IsPresentInGame(spec, form) || FormInfo.IsLordForm(spec, form, tr.Context) || FormInfo.IsBattleOnlyForm(spec, form, destGeneration) || FormInfo.IsFusedForm(spec, form, destGeneration) || (FormInfo.IsTotemForm(spec, form) && tr.Context is not EntityContext.Gen7));

            if (spec is ((ushort)Meowstic) or ((ushort)Indeedee))
            {
                rough.Gender = rough.Form;
                form = rough.Form = rough.Gender;
            }

            var item = GetFormSpecificItem(destVersion, destGeneration, spec, form);
            if (item is not null)
                rough.HeldItem = (int)item;

            if (rough is { Species: (ushort)Keldeo, Form: 1 })
                rough.Move1 = (ushort)Move.SecretSword;

            if (GetIsFormInvalid(rough, destGeneration, tr.Context, form))
                continue;

            try
            {
                var goodset = new SmogonSetGenerator(rough);
                if (goodset is { Valid: true, Sets.Count: not 0 })
                {
                    var checknull = tr.GetLegalFromSet(goodset.Sets[rng.Next(goodset.Sets.Count)]);
                    if (checknull.Status != LegalizationResult.Regenerated)
                        continue;
                    checknull.Created.ResetPartyStats();
                    selectedSpecies[ctr] = spec;
                    result[ctr++] = checknull.Created;
                    continue;
                }
            }
            catch (Exception) { Debug.Write("Smogon Issues"); }

            var showstring = new ShowdownSet(rough).Text.Split('\r')[0];
            showstring += "\nLevel: 100\n";
            ivs.Clear();
            EffortValues.SetMax(ivs, rough);
            showstring += $"EVs: {ivs[0]} HP / {ivs[1]} Atk / {ivs[2]} Def / {ivs[3]} SpA / {ivs[4]} SpD / {ivs[5]} Spe\n";
            var m = new ushort[4];
            rough.GetMoveSet(m, true);

            var moves = GameInfo.GetStrings("en").Move;
            showstring += $"- {moves[m[0]]}\n- {moves[m[1]]}\n- {moves[m[2]]}\n- {moves[m[3]]}";
            showstring += "\n\n";
            var nullcheck = tr.GetLegalFromSet(new ShowdownSet(showstring));
            if (nullcheck.Status != LegalizationResult.Regenerated)
                continue;
            var pk = nullcheck.Created;
            pk.ResetPartyStats();
            selectedSpecies[ctr] = spec;
            result[ctr++] = pk;
        }

        return result;
    }
}
