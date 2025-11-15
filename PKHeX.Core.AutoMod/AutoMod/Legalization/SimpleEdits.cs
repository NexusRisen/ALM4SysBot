using System;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.Species;

namespace PKHeX.Core.AutoMod;

public static class SimpleEdits
{
    // Make PKHeX use our own marking method
    static SimpleEdits() => MarkingApplicator.MarkingMethod = FlagIVsAutoMod;

    internal static ReadOnlySpan<ushort> AlolanOriginForms =>
    [
        019, // Rattata
        020, // Raticate
        027, // Sandshrew
        028, // Sandslash
        037, // Vulpix
        038, // Ninetales
        050, // Diglett
        051, // Dugtrio
        052, // Meowth
        053, // Persian
        074, // Geodude
        075, // Graveler
        076, // Golem
        088, // Grimer
        089, // Muk
    ];

    /// <summary>
    /// Determines if a species/form combination is shiny-locked.
    /// </summary>
    /// <param name="species">The species ID.</param>
    /// <param name="form">The form ID.</param>
    /// <returns>True if shiny-locked, otherwise false.</returns>
    public static bool IsShinyLockedSpeciesForm(ushort species, byte form) => (Species)species switch
    {
        Pikachu => form is not (0 or 8), // Cap Pikachus, Cosplay
        Pichu => form is 1, // Spiky-eared
        Victini or Keldeo => true,
        Scatterbug or Spewpa or Vivillon => form is 19, // Poké Ball
        Hoopa or Volcanion or Cosmog or Cosmoem => true,

        Magearna => true, // Even though a shiny is available via HOME, can't generate as legal.

        Kubfu or Urshifu or Zarude => true,
        Glastrier or Spectrier or Calyrex => true,
        Enamorus => true,
        Gimmighoul => form is 1,

        WoChien or ChienPao or TingLu or ChiYu => true,
        Koraidon or Miraidon => true,

        WalkingWake or IronLeaves => true,
        Okidogi or Munkidori or Fezandipiti => true,
        Ogerpon => true,
        GougingFire or RagingBolt or IronBoulder or IronCrown => true,
        Terapagos => true,
        Pecharunt => true,

        _ => false,
    };

    private static Func<int, int, int> FlagIVsAutoMod(PKM pk)
    {
        return pk.Format < 7 ? GetSimpleMarking : GetComplexMarking;

        // value, index
        static int GetSimpleMarking(int val, int _) => val == 31 ? 1 : 0;
        static int GetComplexMarking(int val, int _) => val switch
        {
            31 => 1,
            1 => 2,
            0 => 2,
            _ => 0,
        };
    }

    /// <summary>
    /// Set Encryption Constant based on PKM Generation
    /// </summary>
    /// <param name="pk">PKM to modify</param>
    /// <param name="enc">Encounter details</param>
    public static void SetEncryptionConstant(this PKM pk, IEncounterTemplate enc)
    {
        if (pk.Format < 6)
            return;

        if (enc is { Species: 658, Form: 1 } || APILegality.IsPIDIVSet(pk, enc)) // Ash-Greninja or raids
            return;

        if (enc.Generation is 3 or 4 or 5)
        {
            var ec = pk.PID;
            pk.EncryptionConstant = ec;
            var pidxor = ((pk.TID16 ^ pk.SID16 ^ (int)(ec & 0xFFFF) ^ (int)(ec >> 16)) & ~0x7) == 8;
            pk.PID = pidxor ? ec ^ 0x80000000 : ec;
            return;
        }
        var wIndex = WurmpleUtil.GetWurmpleEvoGroup(pk.Species);
        if (wIndex != WurmpleEvolution.None)
        {
            pk.EncryptionConstant = WurmpleUtil.GetWurmpleEncryptionConstant(wIndex);
            return;
        }

        if (enc is not ITeraRaid9 && pk is { Species: (ushort)Maushold, Form: 0 } or { Species: (ushort)Dudunsparce, Form: 1 })
        {
            pk.EncryptionConstant = pk.EncryptionConstant / 100 * 100;
            return;
        }

        if (pk.EncryptionConstant != 0)
            return;

        pk.EncryptionConstant = enc is WC8 { PIDType: ShinyType8.FixedValue, EncryptionConstant: 0 } ? 0 : Util.Rand32();
    }

    /// <summary>
    /// Sets shiny value to whatever boolean is specified. Takes in specific shiny as a boolean.
    /// </summary>
    /// <param name="pk">PKM to modify</param>
    /// <param name="isShiny">Shiny value that needs to be set</param>
    /// <param name="enc">Encounter details</param>
    /// <param name="shiny">Set is shiny</param>
    /// <param name="method">PID generation method</param>
    /// <param name="criteria">Encounter criteria</param>
    public static void SetShinyBoolean(this PKM pk, bool isShiny, IEncounterTemplate enc, Shiny shiny, PIDType method, EncounterCriteria criteria)
    {
        // Early exit conditions
        if (IsShinyLockedSpeciesForm(pk.Species, pk.Form))
            return;

        if (pk.IsShiny == isShiny)
            return; // Already has the desired shiny state

        // Handle non-shiny request
        if (!isShiny)
        {
            pk.SetUnshiny();
            return;
        }

        // Route to appropriate shiny handler based on encounter type and generation
        var handled = enc switch
        {
            // Gen 8 Raids
            EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U =>
                HandleRaidShiny(pk, shiny, enc),

            // HOME Gifts
            WC8 { IsHOMEGift: true } =>
                HandleHOMEGiftShiny(pk),

            // Mystery Gifts
            MysteryGift mg =>
                HandleMysteryGiftShiny(pk, mg, enc.Generation),

            // Handle other cases by generation
            _ => false
        };

        if (handled)
            return;

        // Handle generation-specific shiny logic
        if (enc.Generation > 5 || pk.VC)
        {
            HandleModernShiny(pk, shiny, enc);
        }
        else if (enc.Generation == 5)
        {
            HandleGen5Shiny(pk, shiny);
        }
        else if (enc.Generation is 3 or 4)
        {
            HandleClassicShiny(pk, enc, method, criteria);
        }
        else if (enc.Generation is 1 or 2)
        {
            HandleGen12Shiny(pk);
        }
    }

    /// <summary>
    /// Handles shiny logic for raid encounters
    /// </summary>
    private static bool HandleRaidShiny(PKM pk, Shiny shiny, IEncounterTemplate enc)
    {
        pk.SetRaidShiny(shiny, enc);
        return true;
    }

    /// <summary>
    /// Handles shiny logic for HOME gifts
    /// </summary>
    private static bool HandleHOMEGiftShiny(PKM pk)
    {
        // Set XOR as 0 so SID comes out as 8 or less, Set TID based on that
        pk.TID16 = (ushort)(0 ^ (pk.PID & 0xFFFF) ^ (pk.PID >> 16));
        pk.SID16 = (ushort)Util.Rand.Next(8);
        return true;
    }

    /// <summary>
    /// Handles shiny logic for Mystery Gifts
    /// </summary>
    private static bool HandleMysteryGiftShiny(PKM pk, MysteryGift mg, byte generation)
    {
        if (mg.IsEgg || mg is PGT { IsManaphyEgg: true })
        {
            pk.SetShinySID(); // not SID locked
            return true;
        }

        pk.SetShiny();

        if (generation < 6)
            return true;

        // Ensure bit 3 is not set for Gen 6+ Mystery Gifts
        while (IsBit3Set(pk))
        {
            pk.SetShiny();
        }

        return true;
    }

    /// <summary>
    /// Handles shiny logic for modern games (Gen 6+)
    /// </summary>
    private static void HandleModernShiny(PKM pk, Shiny shiny, IEncounterTemplate enc)
    {
        if (enc.Shiny is Shiny.FixedValue or Shiny.Never)
            return;

        while (true)
        {
            pk.SetShiny();

            if (IsShinyTypeMatch(pk, shiny))
                return;
        }
    }

    /// <summary>
    /// Handles shiny logic for Gen 5
    /// </summary>
    private static void HandleGen5Shiny(PKM pk, Shiny shiny)
    {
        while (true)
        {
            pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Form, pk.PID);

            if (!IsShinyTypeMatch(pk, shiny))
                continue;

            var isValidGen5SID = pk.SID16 & 1;
            pk.SetShinySID();
            pk.EncryptionConstant = pk.PID;

            var result = (pk.PID & 1) ^ (pk.PID >> 31) ^ (pk.TID16 & 1) ^ (pk.SID16 & 1);
            if ((isValidGen5SID == (pk.SID16 & 1)) && result == 0)
                break;
        }
    }

    /// <summary>
    /// Handles shiny logic for classic games (Gen 3-4)
    /// </summary>
    private static void HandleClassicShiny(PKM pk, IEncounterTemplate enc, PIDType method, EncounterCriteria criteria)
    {
        // Type-safe handling for GameCube games
        if (pk is XK3 xk3)
        {
            HandleCXDShiny(xk3, enc, method, criteria);
            return;
        }

        // For non-CXD Gen 3-4, use standard shiny SID method
        if (TrainerIDVerifier.TryGetShinySID(pk.PID, pk.TID16, pk.Version, out var sid))
            pk.SID16 = sid;
    }

    /// <summary>
    /// Handles shiny logic specifically for Colosseum/XD
    /// </summary>
    private static void HandleCXDShiny(XK3 xk3, IEncounterTemplate enc, PIDType method, EncounterCriteria criteria)
    {
        if (xk3.Version == GameVersion.CXD && method == PIDType.CXD && criteria.Shiny.IsShiny())
        {
            MethodCXD.SetStarterFromIVs(xk3, criteria);
        }

        var la = new LegalityAnalysis(xk3);
        if (la.Info.PIDIV.Type is not PIDType.CXD and not PIDType.CXD_ColoStarter ||
            !la.Info.PIDIVMatches ||
            !xk3.IsValidGenderPID(enc))
        {
            MethodCXD.SetFromIVs(xk3, criteria, (PersonalInfo3)xk3.PersonalInfo, false);
        }

        if (TrainerIDVerifier.TryGetShinySID(xk3.PID, xk3.TID16, xk3.Version, out var sid))
            xk3.SID16 = sid;
    }

    /// <summary>
    /// Handles shiny logic for Gen 1-2
    /// </summary>
    private static void HandleGen12Shiny(PKM pk)
    {
        pk.SetShiny();
    }

    /// <summary>
    /// Checks if the shiny type matches the requested type
    /// </summary>
    private static bool IsShinyTypeMatch(PKM pk, Shiny requestedType)
    {
        return requestedType switch
        {
            Shiny.AlwaysSquare => pk.ShinyXor == 0,
            Shiny.AlwaysStar => pk.ShinyXor != 0,
            _ => true
        };
    }

    /// <summary>
    /// Checks if bit 3 is set (used for Mystery Gift validation)
    /// </summary>
    private static bool IsBit3Set(PKM pk)
    {
        return ((pk.TID16 ^ pk.SID16 ^ (int)(pk.PID & 0xFFFF) ^ (int)(pk.PID >> 16)) & ~0x7) == 8;
    }

    /// <summary>
    /// Sets the shiny status for a raid encounter.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="shiny">The shiny type to set.</param>
    /// <param name="enc">The encounter template.</param>
    public static void SetRaidShiny(this PKM pk, Shiny shiny, IEncounterTemplate enc)
    {
        if (pk.IsShiny)
            return;

        while (true)
        {
            pk.SetShiny();
            if (pk.Format <= 7)
                return;

            var xor = pk.ShinyXor;

            // Special handling for Max Lair encounters - they must have XOR=1 when shiny
            if (enc is EncounterStatic8U)
            {
                // If already shiny with XOR=1, we're done
                if (xor == 1)
                    return;

                // If not shiny and we don't want shiny, we're done  
                if (!pk.IsShiny && shiny == Shiny.Never)
                    return;

                // Force XOR=1 for shiny Max Lair encounters
                if (pk.IsShiny && xor != 1)
                {
                    pk.PID = GetShinyPID(pk.TID16, pk.SID16, pk.PID, 1);
                    return;
                }

                continue; // Try again if conditions not met
            }

            // Regular raid shiny handling for other encounter types
            if ((shiny == Shiny.AlwaysStar && xor == 1) ||
                (shiny == Shiny.AlwaysSquare && xor == 0) ||
                ((shiny is Shiny.Always or Shiny.Random) && xor < 2))
                return;
        }
    }

    /// <summary>
    /// Clears all relearn moves for the PKM.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    public static void ClearRelearnMoves(this PKM pk)
    {
        pk.RelearnMove1 = 0;
        pk.RelearnMove2 = 0;
        pk.RelearnMove3 = 0;
        pk.RelearnMove4 = 0;
    }

    /// <summary>
    /// Calculates a shiny PID based on TID, SID, PID, and type.
    /// </summary>
    /// <param name="tid">Trainer ID.</param>
    /// <param name="sid">Secret ID.</param>
    /// <param name="pid">PID value.</param>
    /// <param name="type">Type value.</param>
    /// <returns>The calculated shiny PID.</returns>
    public static uint GetShinyPID(int tid, int sid, uint pid, int type)
    {
        return (uint)(((tid ^ sid ^ (pid & 0xFFFF) ^ type) << 16) | (pid & 0xFFFF));
    }

    /// <summary>
    /// Applies height and weight values to the PKM based on the encounter template.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="enc">The encounter template.</param>
    public static void ApplyHeightWeight(this PKM pk, IEncounterTemplate enc, bool signed = true)
    {
        if (enc is { Generation: < 8, Context: not EntityContext.Gen7b } && pk.Format >= 8) // height and weight don't apply prior to GG
            return;
        if (enc is { Context: EntityContext.Gen9a }) // do not break xoroshiro correlation.
            return;

        if (pk is IScaledSizeValue obj) // Deal with this later -- restrictions on starters/statics/alphas, for now roll with whatever encounter DB provides
        {
            obj.HeightAbsolute = obj.CalcHeightAbsolute;
            obj.WeightAbsolute = obj.CalcWeightAbsolute;
            if (pk is PB7 pb1)
                pb1.ResetCP();

            return;
        }
        if (pk is not IScaledSize size)
            return;

        // fixed height and weight
        bool isFixedScale = enc switch
        {
            EncounterStatic9 { Size: not 0 } => true,
            EncounterTrade8b => true,
            EncounterTrade9 => true,
            EncounterStatic8a { HasFixedHeight: true } => true,
            EncounterStatic8a { HasFixedWeight: true } => true,
            _ => false,
        };
        if (isFixedScale)
            return;

        if (enc is WC8 { IsHOMEGift: true })
            return; // HOME gift. No need to set height and weight

        if (enc is WC9 wc9)
        {
            size.WeightScalar = (byte)wc9.WeightValue;
            size.HeightScalar = (byte)wc9.HeightValue;
            return;
        }

        if (enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND)
            return;
        if (APILegality.IsPIDIVSet(pk, enc) && enc is not EncounterEgg8b)
            return;

        var height = 0x12;
        var weight = 0x97;
        var scale = 0xFB;
        if (signed)
        {
            if (GameVersion.SWSH.Contains(pk.Version) || GameVersion.BDSP.Contains(pk.Version) || GameVersion.SV.Contains(pk.Version))
            {
                var top = (int)(pk.PID >> 16);
                var bottom = (int)(pk.PID & 0xFFFF);
                height = (top % 0x80) + (bottom % 0x81);
                weight = ((int)(pk.EncryptionConstant >> 16) % 0x80) + ((int)(pk.EncryptionConstant & 0xFFFF) % 0x81);
                scale = ((int)(pk.PID >> 16)*height % 0x80) + ((int)(pk.PID &0xFFFF)*height % 0x81);
            }
            else if (pk.GG)
            {
                height = (int)(pk.PID >> 16) % 0xFF;
                weight = (int)(pk.PID & 0xFFFF) % 0xFF;
                scale = (int)(pk.PID >> 8) % 0xFF;
            }
        }
        else
        {
            height = Util.Rand.Next(255);
            weight = Util.Rand.Next(255);
            scale = Util.Rand.Next(255);
        }
        size.HeightScalar = (byte)height;
        size.WeightScalar = (byte)weight;
        if (pk is IScaledSize3 sz3 && enc is not EncounterFixed9 && sz3.Scale != 128)
            sz3.Scale = (byte)scale;
    }

    /// <summary>
    /// Sets the friendship values for the PKM based on the encounter template.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="enc">The encounter template.</param>
    public static void SetFriendship(this PKM pk, IEncounterTemplate enc)
    {
        if (enc.Generation <= 2)
        {
            pk.OriginalTrainerFriendship = (byte)GetBaseFriendship(EntityContext.Gen7, pk.Species, pk.Form); // VC transfers use SM personal info
            return;
        }

        bool wasNeverOriginalTrainer = !HistoryVerifier.GetCanOTHandle(enc, pk, enc.Generation);
        if (wasNeverOriginalTrainer)
        {
            pk.OriginalTrainerFriendship = (byte)GetBaseFriendship(enc);
            pk.HandlingTrainerFriendship = pk.HasMove(218) ? (byte)0 : (byte)255;
        }
        else
        {
            pk.CurrentFriendship = pk.HasMove(218) ? (byte)0 : (byte)255;
        }
    }

    /// <summary>
    /// Sets calculated values for Beluga-based PKM.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    public static void SetBelugaValues(this PKM pk)
    {
        if (pk is PB7 pb7)
            pb7.ResetCalculatedValues();
    }

    /// <summary>
    /// Sets Awakened Values (AVs) for a PKM based on a battle template.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="set">The battle template.</param>
    public static void SetAwakenedValues(this PKM pk, IBattleTemplate set)
    {
        if (pk is not PB7 pb7)
            return;

        Span<byte> result = stackalloc byte[6];
        AwakeningUtil.SetExpectedMinimumAVs(result, pb7);

        const byte max = AwakeningUtil.AwakeningMax;
        ReadOnlySpan<int> evs = set.EVs;
        pb7.AV_HP  = (byte)Math.Min(max, Math.Max(result[0], evs[0]));
        pb7.AV_ATK = (byte)Math.Min(max, Math.Max(result[1], evs[1]));
        pb7.AV_DEF = (byte)Math.Min(max, Math.Max(result[2], evs[2]));
        pb7.AV_SPA = (byte)Math.Min(max, Math.Max(result[3], evs[4]));
        pb7.AV_SPD = (byte)Math.Min(max, Math.Max(result[4], evs[5]));
        pb7.AV_SPE = (byte)Math.Min(max, Math.Max(result[5], evs[3]));
    }

    /// <summary>
    /// Sets the handling trainer language for the PKM.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="prefer">Preferred language ID.</param>
    public static void SetHTLanguage(this PKM pk, byte prefer)
    {
        var preferID = (LanguageID)prefer;
        if (preferID is LanguageID.None or LanguageID.UNUSED_6)
            prefer = 2; // prefer english

        if (pk is IHandlerLanguage h)
            h.HandlingTrainerLanguage = prefer;
    }

    /// <summary>
    /// Sets the Gigantamax factor for the PKM based on the battle template and encounter.Add commentMore actions
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="set">The battle template.</param>
    /// <param name="enc">The encounter template.</param>
    public static void SetGigantamaxFactor(this PKM pk, IBattleTemplate set, IEncounterTemplate enc)
    {
        if (pk is not IGigantamax gmax || gmax.CanGigantamax == set.CanGigantamax)
            return;

        if (Gigantamax.CanToggle(pk.Species, pk.Form, enc.Species, enc.Form))
            gmax.CanGigantamax = set.CanGigantamax; // soup hax
    }

    /// <summary>
    /// Sets Dyna/Gimmick values for the PKM based on the battle template.
    /// </summary>
    /// <param name="pk">The PKM to modify.</param>
    /// <param name="set">The battle template.</param>
    public static void SetGimmicks(this PKM pk, IBattleTemplate set)
    {
        if (pk is IDynamaxLevel d)
            d.DynamaxLevel = d.GetSuggestedDynamaxLevel(pk, requested: set.DynamaxLevel);

        if (pk is ITeraType t && set.TeraType != MoveType.Any && t.GetTeraType() != set.TeraType)
            t.SetTeraType(set.TeraType);
    }

    internal static void HyperTrain(this IHyperTrain t, PKM pk, ReadOnlySpan<int> ivs, EncounterCriteria criteria)
    {
        t.HT_HP  = pk.IV_HP  != 31 && criteria.IV_HP is -1;
        t.HT_ATK = pk.IV_ATK != 31 && ivs[1] > 2 && criteria.IV_ATK is -1;
        t.HT_DEF = pk.IV_DEF != 31 && criteria.IV_DEF is -1;
        t.HT_SPA = pk.IV_SPA != 31 && ivs[4] > 2 && criteria.IV_SPA is -1;
        t.HT_SPD = pk.IV_SPD != 31 && criteria.IV_SPD is -1;
        t.HT_SPE = pk.IV_SPE != 31 && ivs[3] > 2 && criteria.IV_SPE is -1;

        if (pk is PB7 pb)
            pb.ResetCP();
    }

    public static void SetSuggestedMemories(this PKM pk)
    {
        switch (pk)
        {
            case PK9 pk9 when !pk.IsUntraded:
                pk9.ClearMemoriesHT();
                break;
            case PA8 pa8 when !pk.IsUntraded:
                pa8.ClearMemoriesHT();
                break;
            case PB8 pb8 when !pk.IsUntraded:
                pb8.ClearMemoriesHT();
                break;
            case PK8 pk8 when !pk.IsUntraded:
                pk8.SetTradeMemoryHT8();
                break;
            case PK7 pk7 when !pk.IsUntraded:
                pk7.SetTradeMemoryHT6(true);
                break;
            case PK6 pk6 when !pk.IsUntraded:
                pk6.SetTradeMemoryHT6(true);
                break;
        }
    }

    private static int GetBaseFriendship(IEncounterTemplate enc) => enc switch
    {
        IFixedOTFriendship f => f.OriginalTrainerFriendship,
        { Version: GameVersion.BD or GameVersion.SP } => PersonalTable.SWSH.GetFormEntry(enc.Species, enc.Form).BaseFriendship,
        _ => GetBaseFriendship(enc.Context, enc.Species, enc.Form),
    };

    private static int GetBaseFriendship(EntityContext context, ushort species, byte form) => context switch
    {
        EntityContext.Gen1  => PersonalTable.USUM[species].BaseFriendship,
        EntityContext.Gen2  => PersonalTable.USUM[species].BaseFriendship,
        EntityContext.Gen6  => PersonalTable.AO  [species].BaseFriendship,
        EntityContext.Gen7  => PersonalTable.USUM[species].BaseFriendship,
        EntityContext.Gen7b => PersonalTable.GG  [species].BaseFriendship,
        EntityContext.Gen8  => PersonalTable.SWSH[species, form].BaseFriendship,
        EntityContext.Gen8a => PersonalTable.LA  [species, form].BaseFriendship,
        EntityContext.Gen8b => PersonalTable.BDSP[species, form].BaseFriendship,
        EntityContext.Gen9  => PersonalTable.SV  [species, form].BaseFriendship,
        EntityContext.Gen9a => PersonalTable.ZA  [species, form].BaseFriendship,
        _ => throw new IndexOutOfRangeException(),
    };

    /// <summary>
    /// Set TID, SID and OT
    /// </summary>
    /// <param name="pk">PKM to set trainer data to</param>
    /// <param name="trainer">Trainer data</param>
    public static void SetTrainerData(this PKM pk, ITrainerInfo trainer)
    {
        pk.TID16 = trainer.TID16;
        pk.SID16 = pk.Generation >= 3 ? trainer.SID16 : (ushort)0;
        pk.OriginalTrainerName = trainer.OT;
    }

    /// <summary>
    /// Set Handling Trainer data for a given PKM
    /// </summary>
    /// <param name="pk">PKM to modify</param>
    /// <param name="trainer">Trainer to handle the <see cref="pk"/></param>
    /// <param name="enc">Encounter template originated from</param>
    public static void SetHandlerAndMemory(this PKM pk, ITrainerInfo trainer, IEncounterTemplate enc)
    {
        if (IsUntradeableEncounter(enc))
            return;

        var expect = trainer.IsFromTrainer(pk) ? 0 : 1;
        if (pk.CurrentHandler == expect && expect == 0)
            return;

        pk.CurrentHandler = 1;
        pk.HandlingTrainerName = trainer.OT;
        pk.HandlingTrainerGender = trainer.Gender;
        pk.SetHTLanguage((byte)trainer.Language);
        pk.SetSuggestedMemories();
    }

    /// <summary>
    /// Set trainer data for a legal PKM
    /// </summary>
    /// <param name="pk">Legal PKM for setting the data</param>
    /// <param name="trainer"></param>
    /// <returns>PKM with the necessary values modified to reflect trainer data changes</returns>
    public static void SetAllTrainerData(this PKM pk, ITrainerInfo trainer)
    {
        pk.SetBelugaValues(); // trainer details changed?

        if (pk is not IGeoTrack gt)
            return;

        if (trainer is not IRegionOrigin o)
        {
            gt.ConsoleRegion = 1; // North America
            gt.Country = 49; // USA
            gt.Region = 7; // California
            return;
        }

        gt.ConsoleRegion = o.ConsoleRegion;
        gt.Country = o.Country;
        gt.Region = o.Region;
        if (pk is PK7 pk7 && pk.Generation <= 2)
            pk7.FixVCRegion();
        else if (pk.Species is (int)Vivillon or (int)Spewpa or (int)Scatterbug)
            pk.FixVivillonRegion();
    }

    /// <summary>
    /// Sets a moveset which is suggested based on calculated legality.
    /// </summary>
    /// <param name="pk">Legal PKM for setting the data</param>
    /// <param name="random">True for Random assortment of legal moves, false if current moves only.</param>
    public static void SetSuggestedMoves(this PKM pk, bool random = false)
    {
        Span<ushort> m = stackalloc ushort[4];
        pk.GetMoveSet(m, random);
        var moves = m.ToArray();
        if (moves.All(z => z == 0))
            return;

        if (pk.Moves.SequenceEqual(moves))
            return;

        pk.SetMoves(moves);
    }

    /// <summary>
    /// Set Dates for date-locked Pokémon
    /// </summary>
    /// <param name="pk">Pokémon file to modify</param>
    /// <param name="enc">encounter used to generate Pokémon file</param>
    public static void SetDateLocks(this PKM pk, IEncounterTemplate enc)
    {
        if (enc is WC8 { IsHOMEGift: true } wc8)
            SetDateLocksWC8(pk, wc8);
    }

    private static void SetDateLocksWC8(PKM pk, WC8 w)
    {
        var locked = w.GetDistributionWindow(out var time);
        if (locked)
            pk.MetDate = time.Start;
    }

    public static bool TryApplyHardcodedSeedWild8(PK8 pk, IEncounterTemplate enc, ReadOnlySpan<int> ivs, Shiny requestedShiny)
    {
        // Don't bother if there is no overworld correlation
        if (enc is not IOverworldCorrelation8 eo)
            return false;

        // Check if a seed exists
        var flawless = Overworld8Search.GetFlawlessIVCount(enc, ivs, out var seed);

        // Ensure requested criteria matches
        if (flawless == -1)
            return false;

        APILegality.FindWildPIDIV8(pk, requestedShiny, flawless, seed);
        return eo.IsOverworldCorrelationCorrect(pk) && requestedShiny switch
        {
            Shiny.AlwaysStar when pk.ShinyXor is 0 or > 15 => false,
            Shiny.Never when pk.ShinyXor < 16 => false,
            _ => true,
        };
    }

    public static bool ExistsInGame(this GameVersion destVer, ushort species, byte form)
    {
        // Don't process if Game is LGPE and requested PKM is not Kanto / Meltan / Melmetal
        // Don't process if Game is SWSH and requested PKM is not from the Galar Dex (Zukan8.DexLookup)
        if (destVer is GameVersion.GP or GameVersion.GE)
            return species is <= 151 or 808 or 809;

        if (GameVersion.SWSH.Contains(destVer))
            return PersonalTable.SWSH.IsPresentInGame(species, form);

        if (GameVersion.PLA.Contains(destVer))
            return PersonalTable.LA.IsPresentInGame(species, form);

        if (destVer is GameVersion.ZA)
            return PersonalTable.ZA.IsPresentInGame(species, form);

        return GameVersion.SV.Contains(destVer) ? PersonalTable.SV.IsPresentInGame(species, form) : (uint)species <= destVer.GetMaxSpeciesID();
    }

    public static GameVersion GetIsland(this GameVersion ver) => ver switch
    {
        GameVersion.BD or GameVersion.SP => GameVersion.BDSP,
        GameVersion.SW or GameVersion.SH => GameVersion.SWSH,
        GameVersion.GP or GameVersion.GE => GameVersion.GG,
        GameVersion.SN or GameVersion.MN => GameVersion.SM,
        GameVersion.US or GameVersion.UM => GameVersion.USUM,
        GameVersion.X or GameVersion.Y => GameVersion.XY,
        GameVersion.OR or GameVersion.AS => GameVersion.ORAS,
        GameVersion.B or GameVersion.W => GameVersion.BW,
        GameVersion.B2 or GameVersion.W2 => GameVersion.B2W2,
        GameVersion.HG or GameVersion.SS => GameVersion.HGSS,
        GameVersion.FR or GameVersion.LG => GameVersion.FRLG,
        GameVersion.D or GameVersion.P or GameVersion.Pt => GameVersion.DPPt,
        GameVersion.R or GameVersion.S or GameVersion.E => GameVersion.RSE,
        GameVersion.GD or GameVersion.SI or GameVersion.C => GameVersion.GSC,
        GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GN => GameVersion.Gen1,
        _ => ver,
    };

    public static void ApplyPostBatchFixes(this PKM pk)
    {
        if (pk is IScaledSizeValue sv)
        {
            sv.ResetHeight();
            sv.ResetWeight();
        }
    }

    public static bool IsUntradeableEncounter(IEncounterTemplate enc) => enc switch
    {
        EncounterStatic7b { Location: 28 } => true, // LGP/E Starter
        _ => false,
    };

    public static void SetRecordFlags(this PKM pk, ReadOnlySpan<ushort> moves)
    {
        if (pk is ITechRecord tr and not PA8 and not PA9)
        {
            if (pk.Species == (ushort)Hydrapple)
            {
                ReadOnlySpan<ushort> dc = [(ushort)Move.DragonCheer];
                tr.SetRecordFlags(dc);
            }
            if (moves.Length != 0)
            {
                tr.SetRecordFlags(moves);
            }
            else
            {
                var permit = tr.Permit;
                for (int i = 0; i < permit.RecordCountUsed; i++)
                {
                    if (permit.IsRecordPermitted(i))
                        tr.SetMoveRecordFlag(i);
                }
            }
            return;
        }

        if (pk is IMoveShop8Mastery master)
            master.SetMoveShopFlags(pk);
        if (pk is PA9 pa9)
        {
            var permit = (IPermitPlus)pa9.PersonalInfo;
            var (_, plus) = LearnSource9ZA.GetLearnsetAndPlus(pa9.Species, pa9.Form);
            PlusRecordApplicator.SetPlusFlagsEncounter(pa9, permit, plus, pa9.CurrentLevel);
        }
        else if (pk is IPlusRecord pr)
        {
            pr.SetPlusFlags((IPermitPlus)pk.PersonalInfo, new LegalityAnalysis(pk), true, true);
        }
    }

    public static void SetSuggestedContestStats(this PKM pk, IEncounterTemplate enc)
    {
        var la = new LegalityAnalysis(pk);
        pk.SetSuggestedContestStats(enc, la.Info.EvoChainsAllGens);
    }

    /// <summary>
    /// Contains the valid date ranges for 7-star "Unrivaled" Tera Raid events where Pokémon could receive the Mightiest Mark.
    /// </summary>
    /// <remarks>
    /// Each entry maps a species ID to a list of date ranges representing the distribution windows for that species'
    /// 7-star raid events. These dates are based on official event schedules and are used to ensure legal Met Dates
    /// for Pokémon with the Mightiest Mark ribbon.
    /// </remarks>
    private static readonly Dictionary<int, List<(DateOnly Start, DateOnly End)>> UnrivaledDateRanges = new()
    {
        // Generation 1
        [(int)Species.Charizard] = [(new(2022, 12, 02), new(2022, 12, 04)), (new(2022, 12, 16), new(2022, 12, 18)), (new(2024, 03, 13), new(2024, 03, 17))], // Charizard
        [(int)Species.Venusaur] = [(new(2024, 02, 28), new(2024, 03, 05))], // Venusaur
        [(int)Species.Blastoise] = [(new(2024, 03, 06), new(2024, 03, 12))], // Blastoise
        [(int)Species.Pikachu] = [(new(2023, 02, 24), new(2023, 02, 27)), (new(2024, 07, 12), new(2024, 07, 25))], // Pikachu
        [(int)Species.Eevee] = [(new(2023, 11, 17), new(2023, 11, 20))], // Eevee
        [(int)Species.Dragonite] = [(new(2024, 08, 23), new(2024, 09, 01))], // Dragonite
        [(int)Species.Mewtwo] = [(new(2023, 09, 01), new(2023, 09, 17))], // Mewtwo

        // Generation 2
        [(int)Species.Meganium] = [(new(2024, 04, 05), new(2024, 04, 07)), (new(2024, 04, 12), new(2024, 04, 14))], // Meganium
        [(int)Species.Typhlosion] = [(new(2023, 04, 14), new(2023, 04, 16)), (new(2023, 04, 21), new(2023, 04, 23))], // Typhlosion
        [(int)Species.Feraligatr] = [(new(2024, 11, 01), new(2024, 11, 03)), (new(2024, 11, 08), new(2024, 11, 10))], // Feraligatr
        [(int)Species.Tyranitar] = [(new(2025, 03, 28), new(2025, 03, 30)), (new(2025, 04, 04), new(2025, 04, 06))], // Tyranitar
        [(int)Species.Porygon2] = [(new(2025, 06, 05), new(2025, 06, 15))], // Porygon2

        // Generation 3
        [(int)Species.Sceptile] = [(new(2024, 06, 28), new(2024, 06, 30)), (new(2024, 07, 05), new(2024, 07, 07))], // Sceptile
        [(int)Species.Blaziken] = [(new(2024, 01, 12), new(2024, 01, 14)), (new(2024, 01, 19), new(2024, 01, 21))], // Blaziken
        [(int)Species.Swampert] = [(new(2024, 05, 31), new(2024, 06, 02)), (new(2024, 06, 07), new(2024, 06, 09))], // Swampert
        [(int)Species.Salamence] = [(new(2025, 04, 18), new(2025, 04, 20)), (new(2025, 04, 25), new(2025, 04, 27))], // Salamence
        [(int)Species.Metagross] = [(new(2025, 05, 09), new(2025, 05, 11)), (new(2025, 05, 12), new(2025, 05, 19))], // Metagross

        // Generation 4
        [(int)Species.Empoleon] = [(new(2024, 02, 02), new(2024, 02, 04)), (new(2024, 02, 09), new(2024, 02, 11))], // Empoleon
        [(int)Species.Infernape] = [(new(2024, 10, 04), new(2024, 10, 06)), (new(2024, 10, 11), new(2024, 10, 13))],  // Infernape
        [(int)Species.Torterra] = [(new(2024, 11, 15), new(2024, 11, 17)), (new(2024, 11, 22), new(2024, 11, 24))],  // Torterra
        [(int)Species.Garchomp] = [(new(2025, 05, 22), new(2025, 05, 25)), (new(2025, 05, 30), new(2025, 06, 01))], // Garchomp

        // Generation 5
        [(int)Species.Emboar] = [(new(2024, 06, 14), new(2024, 06, 16)), (new(2024, 06, 21), new(2024, 06, 23))], // Emboar
        [(int)Species.Serperior] = [(new(2024, 09, 20), new(2024, 09, 22)), (new(2024, 09, 27), new(2024, 09, 29))], // Serperior
        [(int)Species.Samurott] = [(new(2023, 03, 31), new(2023, 04, 02)), (new(2023, 04, 07), new(2023, 04, 09))], // Samurott
        [(int)Species.Hydreigon] = [(new(2025, 11, 07), new(2025, 11, 09)), (new(2025, 11, 14), new(2025, 11, 16))], // Hydreigon

        // Generation 6
        [(int)Species.Chesnaught] = [(new(2023, 05, 12), new(2023, 05, 14)), (new(2023, 06, 16), new(2023, 06, 18))], // Chesnaught
        [(int)Species.Delphox] = [(new(2023, 07, 07), new(2023, 07, 09)), (new(2023, 07, 14), new(2023, 07, 16))], // Delphox
        [(int)Species.Greninja] = [(new(2023, 01, 27), new(2023, 01, 29)), (new(2023, 02, 10), new(2023, 02, 12))], // Greninja

        // Generation 7
        [(int)Species.Decidueye] = [(new(2023, 03, 17), new(2023, 03, 19)), (new(2023, 03, 24), new(2023, 03, 26))], // Decidueye
        [(int)Species.Primarina] = [(new(2024, 05, 10), new(2024, 05, 12)), (new(2024, 05, 17), new(2024, 05, 19))], // Primarina
        [(int)Species.Incineroar] = [(new(2024, 09, 06), new(2024, 09, 08)), (new(2024, 09, 13), new(2024, 09, 15))], // Incineroar
        [(int)Species.Kommoo] = [(new(2025, 07, 11), new(2025, 07, 13)), (new(2025, 07, 18), new(2025, 07, 20))], // Kommo-o

        // Generation 8
        [(int)Species.Rillaboom] = [(new(2023, 07, 28), new(2023, 07, 30)), (new(2023, 08, 04), new(2023, 08, 06))], // Rillaboom
        [(int)Species.Cinderace] = [(new(2022, 12, 30), new(2023, 01, 01)), (new(2023, 01, 13), new(2023, 01, 15))], // Cinderace
        [(int)Species.Inteleon] = [(new(2023, 04, 28), new(2023, 04, 30)), (new(2023, 05, 05), new(2023, 05, 07))], // Inteleon

        // Generation 9
        [(int)Species.Meowscarada] = [(new(2025, 02, 28), new(2025, 03, 06))], // Meowscarada
        [(int)Species.Skeledirge] = [(new(2025, 03, 06), new(2025, 03, 13))], // Skeledirge
        [(int)Species.Quaquaval] = [(new(2025, 03, 14), new(2025, 03, 20))], // Quaquaval
        [(int)Species.Baxcalibur] = [(new(2025, 06, 19), new(2025, 06, 22)), (new(2025, 06, 27), new(2025, 06, 29))], // Baxcalibur
        [(int)Species.Dondozo] = [(new(2024, 07, 26), new(2024, 08, 08))], // Dondozo

        // Paradox Pokémon
        [(int)Species.IronBundle] = [(new(2023, 12, 22), new(2023, 12, 24))], // Iron Bundle
        [(int)Species.IronValiant] = [(new(2025, 10, 03), new(2025, 10, 12))], // Iron Valiant
        [(int)Species.RoaringMoon] = [(new(2025, 10, 03), new(2025, 10, 12))], // Roaring Moon
    };

    /// <summary>
    /// Checks and sets a valid Met Date for Pokémon with the Mightiest Mark ribbon from 7-star Tera Raid events.
    /// </summary>
    /// <param name="pk">The PKM to check and potentially modify the Met Date for.</param>
    /// <remarks>
    /// This method ensures that Pokémon obtained from 7-star "Unrivaled" raids have Met Dates that match
    /// the actual event distribution windows. If the current Met Date is invalid or missing, a random valid
    /// date from the available event windows will be assigned.
    /// </remarks>
    public static void CheckAndSetUnrivaledDate(this PKM pk)
    {
        if (pk is not IRibbonSetMark9 ribbonSetMark || !ribbonSetMark.RibbonMarkMightiest)
            return;

        List<(DateOnly Start, DateOnly End)> dateRanges;

        if (UnrivaledDateRanges.TryGetValue(pk.Species, out var ranges))
        {
            dateRanges = ranges;
        }
        else if (pk.Species is (int)Species.Decidueye or (int)Species.Typhlosion or (int)Species.Samurott && pk.Form == 1)
        {
            // Special handling for Hisuian forms
            dateRanges = pk.Species switch
            {
                (int)Species.Decidueye => [(new(2023, 10, 06), new(2023, 10, 08)), (new(2023, 10, 13), new(2023, 10, 15))],
                (int)Species.Typhlosion => [(new(2023, 11, 03), new(2023, 11, 05)), (new(2023, 11, 10), new(2023, 11, 12))],
                (int)Species.Samurott => [(new(2023, 11, 24), new(2023, 11, 26)), (new(2023, 12, 01), new(2023, 12, 03))],
                _ => []
            };
        }
        else
        {
            return;
        }

        if (!pk.MetDate.HasValue || !IsDateInRanges(pk.MetDate.Value, dateRanges))
        {
            SetRandomDateFromRanges(pk, dateRanges);
        }
    }

    /// <summary>
    /// Determines if a given date falls within any of the provided date ranges.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="ranges">A list of date ranges to check against.</param>
    /// <returns><c>true</c> if the date falls within any of the ranges; otherwise, <c>false</c>.</returns>
    private static bool IsDateInRanges(DateOnly date, List<(DateOnly Start, DateOnly End)> ranges)
    {
        return ranges.Any(range => date >= range.Start && date <= range.End);
    }

    /// <summary>
    /// Sets a random Met Date for the PKM from one of the provided valid date ranges.
    /// </summary>
    /// <param name="pk">The PKM to set the Met Date for.</param>
    /// <param name="ranges">A list of valid date ranges to choose from.</param>
    /// <remarks>
    /// This method randomly selects one of the date ranges and then randomly selects a date within
    /// that range (inclusive of both start and end dates). If no ranges are provided, the method returns
    /// without making any changes.
    /// </remarks>
    private static void SetRandomDateFromRanges(PKM pk, List<(DateOnly Start, DateOnly End)> ranges)
    {
        if (ranges.Count == 0)
            return;

        var random = new Random();
        var (Start, End) = ranges[random.Next(ranges.Count)];
        var days = (End.DayNumber - Start.DayNumber) + 1;
        var randomDays = random.Next(days);
        pk.MetDate = Start.AddDays(randomDays);
    }

    /// <summary>
    /// Gets the required gender for species that appear in Mighty raids.
    /// </summary>
    /// <param name="species">The species to check</param>
    /// <param name="form">The form to check (for regional variants)</param>
    /// <returns>The required gender for Mighty raids, or null if not gender-locked</returns>
    public static byte? GetMightyRaidGender(ushort species, byte form = 0) => (Species)species switch
    {
        // Gen 1
        Species.Charizard => 0,      // Male only
        Species.Venusaur => 0,       // Male only
        Species.Blastoise => 0,      // Male only
        Species.Pikachu => 0,        // Male only
        Species.Mewtwo => 2,         // Genderless (always)
        Species.Eevee => 1,          // Female only
        Species.Dragonite => 0,      // Male only

        // Gen 2
        Species.Meganium => 1,       // Female only
        Species.Typhlosion when form == 0 => 0,  // Male only (Johtonian)
        Species.Typhlosion when form == 1 => 0,  // Male only (Hisuian)
        Species.Feraligatr => 0,     // Male only
        Species.Porygon2 => 2,       // Genderless (always)
        Species.Tyranitar => 0,      // Male only

        // Gen 3
        Species.Sceptile => 0,       // Male only
        Species.Blaziken => 0,       // Male only
        Species.Swampert => 0,       // Male only
        Species.Salamence => 0,      // Male only
        Species.Metagross => 2,      // Genderless (always)

        // Gen 4
        Species.Torterra => 0,       // Male only
        Species.Infernape => 0,      // Male only
        Species.Empoleon => 0,       // Male only
        Species.Garchomp => 0,       // Male only

        // Gen 5
        Species.Serperior => 0,      // Male only
        Species.Emboar => 0,         // Male only
        Species.Samurott when form == 0 => 0,  // Male only (Unovan)
        Species.Samurott when form == 1 => 0,  // Male only (Hisuian)
        Species.Hydreigon => 0,      // Male only

        // Gen 6
        Species.Chesnaught => 0,     // Male only
        Species.Delphox => 1,        // Female only
        Species.Greninja => 0,       // Male only

        // Gen 7
        Species.Decidueye when form == 0 => 0,  // Male only (Alolan)
        Species.Decidueye when form == 1 => 0,  // Male only (Hisuian)
        Species.Incineroar => 0,     // Male only
        Species.Primarina => 1,      // Female only

        // Gen 8
        Species.Rillaboom => 0,      // Male only
        Species.Cinderace => 0,      // Male only
        Species.Inteleon => 0,       // Male only
        Species.Dondozo => 0,        // Male only

        // Gen 9
        Species.Meowscarada => 1,    // Female only
        Species.Skeledirge => 0,     // Male only
        Species.Quaquaval => 0,      // Male only
        Species.Baxcalibur => 0,     // Male only
        Species.IronBundle => 2,     // Genderless (always)
        Species.Kommoo => 0,         // Male only
        Species.IronValiant => 2,    // Genderless
        Species.RoaringMoon => 2,    // Genderless

        _ => null
    };

    private static ReadOnlySpan<ushort> ArceusPlateIDs =>
    [
        303, 306, 304, 305, 309, 308, 310, 313, 298, 299, 301, 300, 307, 302, 311, 312, 644,
    ];

    public static ushort? GetArceusHeldItemFromForm(int form) => form is >= 1 and <= 17 ? ArceusPlateIDs[form - 1] : null;

    public static int? GetSilvallyHeldItemFromForm(int form) => form == 0 ? null : form + 903;

    public static int? GetGenesectHeldItemFromForm(int form) => form == 0 ? null : form + 115;
}
