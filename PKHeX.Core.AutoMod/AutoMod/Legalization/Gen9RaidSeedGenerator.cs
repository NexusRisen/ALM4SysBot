using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PKHeX.Core.ShinyUtil;

namespace PKHeX.Core.AutoMod;

/// <summary>
/// Generates legal Gen 9 Tera Raid Pokémon from Showdown sets by finding valid seeds
/// </summary>
public static class Gen9RaidSeedGenerator
{
    /// <summary>
    /// Attempts to generate a Tera Raid Pokémon from a RegenTemplate
    /// </summary>
    /// <param name="regen">The RegenTemplate to generate from</param>
    /// <param name="tr">The trainer info (must be Gen 9)</param>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 15 seconds, 0 = no timeout)</param>
    /// <param name="maxAttempts">Maximum number of seeds to check (default: 150 million)</param>
    /// <returns>A legal PK9 if found, otherwise null</returns>
    public static PK9? TryGenerateFromShowdownSet(RegenTemplate regen, ITrainerInfo tr, int timeoutSeconds = 15, int maxAttempts = 150_000_000)
    {
        // Validate this is Gen 9
        if (tr.Generation != 9)
            return null;

        // Check if this is a Tera Raid request (MetLocation = 30024)
        if (!regen.Regen.TryGetBatchValue("MetLocation", out var metLocationStr) ||
            !ushort.TryParse(metLocationStr, out var metLocation) ||
            metLocation != Locations.TeraCavern9)
            return null;

        Debug.WriteLine($"[Gen9RaidSeedGenerator] Attempting to generate Tera Raid {regen.Species} from Showdown set");
        Console.WriteLine($"[Gen9RaidSeedGenerator] Attempting to generate Tera Raid {(Species)regen.Species} from Showdown set");

        // Step 1: Determine star rating from MetLevel (not current level!)
        byte metLevel = regen.Level; // Default to current level
        if (regen.Regen.TryGetBatchValue("MetLevel", out var metLevelStr) && byte.TryParse(metLevelStr, out var parsedMetLevel))
            metLevel = parsedMetLevel;

        // Step 2: Get matching encounters for this species
        // For level 75, we need to check both 5★ and 6★ raids
        var encounters = GetRaidEncountersForLevel(regen.Species, regen.Form, metLevel, tr.Version);
        if (encounters.Count == 0)
        {
            Debug.WriteLine($"[Gen9RaidSeedGenerator] No encounters found for {regen.Species} at level {metLevel}");
            Console.WriteLine($"[Gen9RaidSeedGenerator] ❌ No encounters found for {(Species)regen.Species} at level {metLevel}");
            return null;
        }

        Debug.WriteLine($"[Gen9RaidSeedGenerator] Found {encounters.Count} possible encounter(s) for {regen.Species} at level {metLevel}");
        Console.WriteLine($"[Gen9RaidSeedGenerator] Found {encounters.Count} possible encounter(s) for {(Species)regen.Species} at level {metLevel}");

        // Step 3: Convert RegenTemplate to search criteria
        var criteria = ConvertToCriteria(regen);
        var ivRanges = ConvertIVsToRanges(regen.IVs);

        // Step 4: Search for a valid seed with timeout
        var timer = Stopwatch.StartNew();
        using var cts = timeoutSeconds > 0 ? new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)) : new CancellationTokenSource();

        var result = SearchForMatchingSeed(encounters, criteria, ivRanges, regen, tr, maxAttempts, cts.Token);
        timer.Stop();

        if (result != null)
        {
            Debug.WriteLine($"[Gen9RaidSeedGenerator] ✅ Found valid seed {result.Seed:X8} in {timer.ElapsedMilliseconds}ms after checking {result.AttemptsChecked:N0} seeds");
            Console.WriteLine($"[Gen9RaidSeedGenerator] ✅ Found valid seed {result.Seed:X8} in {timer.ElapsedMilliseconds}ms after checking {result.AttemptsChecked:N0} seeds");
            return result.Pokemon;
        }
        else if (cts.IsCancellationRequested && timeoutSeconds > 0)
        {
            Debug.WriteLine($"[Gen9RaidSeedGenerator] ⏱️ Timed out after {timer.ElapsedMilliseconds}ms ({timeoutSeconds}s limit)");
            Console.WriteLine($"[Gen9RaidSeedGenerator] ⏱️ Timed out after {timer.ElapsedMilliseconds}ms ({timeoutSeconds}s limit)");
            Console.WriteLine($"[Gen9RaidSeedGenerator] 💡 TIP: Increase the timeout setting in ALM settings (currently {timeoutSeconds}s) or adjust your criteria (shiny, IVs, nature, etc.)");
            return null;
        }
        else
        {
            Debug.WriteLine($"[Gen9RaidSeedGenerator] ❌ Failed to find valid seed after {maxAttempts:N0} attempts in {timer.ElapsedMilliseconds}ms");
            Console.WriteLine($"[Gen9RaidSeedGenerator] ❌ Failed to find valid seed after {maxAttempts:N0} attempts in {timer.ElapsedMilliseconds}ms");
            Console.WriteLine($"[Gen9RaidSeedGenerator] 💡 TIP: This seed combination may not exist. Try relaxing your criteria (shiny, IVs, nature, etc.)");
            return null;
        }
    }

    /// <summary>
    /// Determines the possible star ratings based on the level
    /// Based on PKHeX encounter data: 1★=12, 2★=20, 3★=35, 4★=45, 5★=75, 6★=75, 7★=90
    /// </summary>
    private static byte[] GetPossibleStarsForLevel(int level)
    {
        return level switch
        {
            12 => [1],
            20 => [2],
            35 => [3],
            45 => [4],
            75 => [5, 6], // Level 75 can be either 5★ or 6★!
            90 => [7], // Mighty raids
            _ => []
        };
    }

    /// <summary>
    /// Gets all possible raid encounters for a species/form/level combination
    /// </summary>
    private static List<ITeraRaid9> GetRaidEncountersForLevel(ushort species, byte form, byte level, GameVersion version)
    {
        var possibleStars = GetPossibleStarsForLevel(level);
        if (possibleStars.Length == 0)
            return [];

        var encounters = new List<ITeraRaid9>();

        // Check all possible star ratings for this level
        foreach (var stars in possibleStars)
        {
            encounters.AddRange(GetRaidEncountersForSpecies(species, form, stars, version));
        }

        return encounters;
    }

    /// <summary>
    /// Gets all possible raid encounters for a species/form/star combination
    /// </summary>
    private static List<ITeraRaid9> GetRaidEncountersForSpecies(ushort species, byte form, byte stars, GameVersion version)
    {
        var encounters = new List<ITeraRaid9>();

        // Check base game raids
        encounters.AddRange(Encounters9.TeraBase.Where(e =>
            e.Species == species && e.Stars == stars && IsFormCompatible(e, form)));

        // Check DLC1 raids (Kitakami)
        encounters.AddRange(Encounters9.TeraDLC1.Where(e =>
            e.Species == species && e.Stars == stars && IsFormCompatible(e, form)));

        // Check DLC2 raids (Blueberry)
        encounters.AddRange(Encounters9.TeraDLC2.Where(e =>
            e.Species == species && e.Stars == stars && IsFormCompatible(e, form)));

        // Check event distribution raids
        encounters.AddRange(Encounters9.Dist.Where(e =>
            e.Species == species && e.Stars == stars && IsFormCompatible(e, form)));

        // Check 7-star mighty raids
        encounters.AddRange(Encounters9.Might.Where(e =>
            e.Species == species && e.Stars == stars && IsFormCompatible(e, form)));

        // Filter by version availability
        encounters = encounters.Where(e => IsAvailableInVersion(e, version)).ToList();

        // Prioritize: Mighty > Dist > Base
        return encounters
            .OrderByDescending(e => e is EncounterMight9)
            .ThenByDescending(e => e is EncounterDist9)
            .ToList();
    }

    /// <summary>
    /// Checks if an encounter is available in the specified game version
    /// </summary>
    private static bool IsAvailableInVersion(ITeraRaid9 encounter, GameVersion version)
    {
        if (encounter is not EncounterTera9 tera)
            return true; // Dist and Might are available in both

        return version switch
        {
            GameVersion.SL => tera.IsAvailableHostScarlet,
            GameVersion.VL => tera.IsAvailableHostViolet,
            _ => true
        };
    }

    /// <summary>
    /// Checks if the encounter form is compatible with the desired form
    /// </summary>
    private static bool IsFormCompatible(ITeraRaid9 encounter, byte desiredForm)
    {
        if (encounter.Form == desiredForm)
            return true;

        // Check for random form encounters
        if (encounter is IEncounterFormRandom { IsRandomUnspecificForm: true })
            return true;

        // Check if form can change
        return FormInfo.IsFormChangeable(encounter.Species, encounter.Form, desiredForm, EntityContext.Gen9, EntityContext.Gen9);
    }

    /// <summary>
    /// Converts a RegenTemplate to EncounterCriteria
    /// </summary>
    private static EncounterCriteria ConvertToCriteria(RegenTemplate regen)
    {
        var shiny = regen.Shiny ? Shiny.Always : Shiny.Never;

        // Check if RegenSet has shiny type override
        if (regen.Regen.Extra.ShinyType != Shiny.Random)
            shiny = regen.Regen.Extra.ShinyType;

        return new EncounterCriteria
        {
            Shiny = shiny,
            Nature = regen.Nature,
            Gender = regen.Gender.HasValue ? (Gender)regen.Gender.Value : Gender.Random,
            Ability = GetAbilityPermissionFromSet(regen)
        };
    }

    /// <summary>
    /// Determines ability permission from RegenTemplate
    /// </summary>
    private static AbilityPermission GetAbilityPermissionFromSet(RegenTemplate regen)
    {
        if (regen.Ability == -1)
            return AbilityPermission.Any12H;

        // Check if it's a hidden ability by looking at PersonalInfo
        var pi = PersonalTable.SV.GetFormEntry(regen.Species, regen.Form);
        if (regen.Ability == pi.AbilityH)
            return AbilityPermission.OnlyHidden;
        if (regen.Ability == pi.Ability1)
            return AbilityPermission.OnlyFirst;
        if (regen.Ability == pi.Ability2)
            return AbilityPermission.OnlySecond;

        return AbilityPermission.Any12H;
    }

    /// <summary>
    /// Converts IVs to search ranges
    /// </summary>
    private static IVRange[] ConvertIVsToRanges(int[] ivs)
    {
        var ranges = new IVRange[6];
        for (int i = 0; i < 6; i++)
        {
            // IVs are always specified in RegenTemplate (default is 31)
            ranges[i] = new IVRange(ivs[i], ivs[i]); // Exact value
        }
        return ranges;
    }

    /// <summary>
    /// Searches for a seed that produces a Pokémon matching all criteria
    /// </summary>
    private static SeedSearchResult? SearchForMatchingSeed(
        List<ITeraRaid9> encounters,
        EncounterCriteria criteria,
        IVRange[] ivRanges,
        RegenTemplate regen,
        ITrainerInfo tr,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var result = new ConcurrentBag<SeedSearchResult>();
        long attemptsChecked = 0;
        long shinyPassedCount = 0;
        long ivPatternPassedCount = 0;
        long generatedCount = 0;
        long lastReportedAt = 0;
        var progressReportInterval = 10_000_000; // Report every 10M seeds

        // Use custom trainer info from showdown set if provided, otherwise use save file trainer info
        var trainerInfo = regen.Regen.HasTrainerSettings && regen.Regen.Trainer != null ? regen.Regen.Trainer : tr;

        // Use parallel processing for speed
        var coreCount = Environment.ProcessorCount;
        var chunkSize = Math.Max(10000, maxAttempts / (coreCount * 100));

        Debug.WriteLine($"[Gen9RaidSeedGenerator] Starting parallel search with {coreCount} cores, chunk size {chunkSize}");
        Console.WriteLine($"[Gen9RaidSeedGenerator] Search criteria: Shiny={criteria.Shiny}, IVs={string.Join("/", ivRanges.Select(r => $"{r.Min}-{r.Max}"))}");

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = coreCount,
            CancellationToken = cancellationToken
        };

        try
        {
            Parallel.For(0, (maxAttempts + chunkSize - 1) / chunkSize, parallelOptions, (chunkIndex, loopState) =>
            {
                uint chunkStart = (uint)(chunkIndex * chunkSize);
                uint chunkEnd = Math.Min((uint)maxAttempts - 1, chunkStart + (uint)chunkSize - 1);

                for (uint seed = chunkStart; seed <= chunkEnd && result.IsEmpty && !cancellationToken.IsCancellationRequested; seed++)
                {
                    var currentCount = Interlocked.Increment(ref attemptsChecked);

                    // Progress reporting
                    if (currentCount - lastReportedAt >= progressReportInterval)
                    {
                        Interlocked.Exchange(ref lastReportedAt, currentCount);
                        var shinyPassed = Interlocked.Read(ref shinyPassedCount);
                        var ivPassed = Interlocked.Read(ref ivPatternPassedCount);
                        var generated = Interlocked.Read(ref generatedCount);
                        Console.WriteLine($"[Gen9RaidSeedGenerator] Progress: {currentCount:N0} seeds checked | Shiny passed: {shinyPassed:N0} | IV pattern passed: {ivPassed:N0} | Generated: {generated:N0}");
                    }

                    // Try each encounter
                    foreach (var encounter in encounters)
                    {
                        // Check if this seed can produce this encounter
                        if (!encounter.CanBeEncountered(seed))
                            continue;

                        // OPTIMIZATION 1: Early shiny check (saves ~94% for shiny-only searches)
                        if (criteria.Shiny == Shiny.Always || criteria.Shiny == Shiny.AlwaysSquare || criteria.Shiny == Shiny.AlwaysStar)
                        {
                            if (!WillBeShiny(seed, encounter, trainerInfo.ID32))
                                continue;
                            Interlocked.Increment(ref shinyPassedCount);
                        }
                        else if (criteria.Shiny == Shiny.Never)
                        {
                            if (WillBeShiny(seed, encounter, trainerInfo.ID32))
                                continue;
                        }

                        // OPTIMIZATION 2: IV pattern pre-check (saves ~30-50% for specific IV patterns)
                        if (!CanMatchIVPattern(seed, encounter, ivRanges))
                            continue;
                        Interlocked.Increment(ref ivPatternPassedCount);

                        // Generate the full Pokémon
                        var pk = GenerateRaidPokemon(encounter, seed, criteria, regen, tr);
                        if (pk == null)
                            continue;
                        Interlocked.Increment(ref generatedCount);

                        // Validate all criteria
                        if (!ValidateAllCriteria(pk, criteria, ivRanges))
                            continue;

                        // Success! Store result and stop searching
                        result.Add(new SeedSearchResult
                        {
                            Seed = seed,
                            Pokemon = pk,
                            Encounter = encounter,
                            AttemptsChecked = Interlocked.Read(ref attemptsChecked)
                        });

                        loopState.Stop(); // Stop all parallel loops
                        return;
                    }

                    // Break if we reached the end
                    if (seed == chunkEnd)
                        break;
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled
        }

        // Final statistics
        var finalShinyPassed = Interlocked.Read(ref shinyPassedCount);
        var finalIvPassed = Interlocked.Read(ref ivPatternPassedCount);
        var finalGenerated = Interlocked.Read(ref generatedCount);
        var finalChecked = Interlocked.Read(ref attemptsChecked);

        Console.WriteLine($"[Gen9RaidSeedGenerator] Final stats: {finalChecked:N0} seeds checked | Shiny passed: {finalShinyPassed:N0} | IV pattern passed: {finalIvPassed:N0} | Generated: {finalGenerated:N0}");

        return result.IsEmpty ? null : result.First();
    }

    /// <summary>
    /// Pre-checks if a seed will produce a shiny Pokémon (without full generation)
    /// </summary>
    private static bool WillBeShiny(uint seed, ITeraRaid9 encounter, uint trainerID32)
    {
        // If encounter forces shiny/non-shiny, return that
        if (encounter.Shiny == Shiny.Always)
            return true;
        if (encounter.Shiny == Shiny.Never)
            return false;

        // For random shiny encounters, simulate the RNG
        var rand = new Xoroshiro128Plus(seed);
        _ = rand.NextInt(); // EC
        var fakeTID = (uint)rand.NextInt();
        uint pid = (uint)rand.NextInt();

        // Check for shiny (raids use 1 roll by default)
        var xor = GetShinyXor(pid, fakeTID);
        return xor < 16; // Is shiny
    }

    /// <summary>
    /// OPTIMIZATION: Pre-checks if a seed can produce the desired IV pattern without full generation
    /// </summary>
    private static bool CanMatchIVPattern(uint seed, ITeraRaid9 encounter, IVRange[] desiredRanges)
    {
        // Extract parameters
        byte flawlessCount = encounter.FlawlessIVCount;
        IndividualValueSet fixedIVs = default;

        if (encounter is EncounterMight9 might)
            fixedIVs = might.IVs;
        else if (encounter is EncounterDist9 dist)
            fixedIVs = dist.IVs;

        // If IVs are fully fixed, check compatibility immediately
        if (fixedIVs.IsSpecified)
        {
            // Reorder to match internal IV order (HP/Atk/Def/Spe/SpA/SpD)
            var ivArray = new[] { fixedIVs.HP, fixedIVs.ATK, fixedIVs.DEF, fixedIVs.SPE, fixedIVs.SPA, fixedIVs.SPD };
            for (int i = 0; i < 6; i++)
            {
                if (ivArray[i] != -1)
                {
                    if (ivArray[i] < desiredRanges[i].Min || ivArray[i] > desiredRanges[i].Max)
                        return false;
                }
            }
            return true;
        }

        // For random flawless IVs, simulate the slot selection
        if (flawlessCount == 0)
            return true; // No constraints to check

        var rand = new Xoroshiro128Plus(seed);
        _ = rand.NextInt(); // EC
        _ = rand.NextInt(); // FakeTID
        _ = rand.NextInt(); // PID

        // Simulate flawless IV slot selection
        bool[] flawlessSlots = new bool[6];
        for (int i = 0; i < flawlessCount; i++)
        {
            int index;
            do { index = (int)rand.NextInt(6); }
            while (flawlessSlots[index]);
            flawlessSlots[index] = true;
        }

        // Check if flawless slots are compatible with desired ranges
        // We can only reject if a flawless slot (guaranteed 31) conflicts with desired range
        for (int i = 0; i < 6; i++)
        {
            if (flawlessSlots[i])
            {
                // This IV will be 31 - check if that's acceptable
                if (desiredRanges[i].Max < 31)
                    return false; // Wants non-31, but will be 31 (impossible)
            }
            // Don't reject non-flawless slots even if they need 31
            // They can still randomly roll 31 (1/32 chance)
        }

        return true;
    }

    /// <summary>
    /// Generates a Pokémon from an encounter and seed using PKHeX's raid generation
    /// </summary>
    private static PK9? GenerateRaidPokemon(ITeraRaid9 encounter, uint seed, EncounterCriteria criteria, RegenTemplate regen, ITrainerInfo tr)
    {
        var pi = PersonalTable.SV[encounter.Species, encounter.Form];

        // Get proper parameters based on encounter type
        byte genderRatio = pi.Gender;
        byte height = 0;
        byte weight = 0;
        SizeType9 scaleType = SizeType9.RANDOM;
        byte scale = 0;
        IndividualValueSet ivs = default;

        // Extract encounter-specific properties
        if (encounter is EncounterMight9 might)
        {
            genderRatio = might.Gender switch
            {
                0 => PersonalInfo.RatioMagicMale,
                1 => PersonalInfo.RatioMagicFemale,
                2 => PersonalInfo.RatioMagicGenderless,
                _ => pi.Gender
            };
            scaleType = might.ScaleType;
            scale = might.Scale;
            ivs = might.IVs;
        }
        else if (encounter is EncounterDist9 dist)
        {
            scaleType = dist.ScaleType;
            scale = dist.Scale;
            ivs = dist.IVs;
        }

        var param = new GenerateParam9(
            encounter.Species,
            genderRatio,
            encounter.FlawlessIVCount,
            1, // roll count
            height,
            weight,
            scaleType,
            scale,
            encounter.Ability,
            encounter.Shiny,
            encounter is IFixedNature fn ? fn.Nature : Nature.Random,
            ivs
        );

        // Use custom trainer info from showdown set if provided, otherwise use save file trainer info
        var trainerInfo = regen.Regen.HasTrainerSettings && regen.Regen.Trainer != null ? regen.Regen.Trainer : tr;

        int language = (int)Language.GetSafeLanguage(9, (LanguageID)trainerInfo.Language);

        var pk = new PK9
        {
            Species = encounter.Species,
            Form = encounter is IEncounterFormRandom { IsRandomUnspecificForm: true } ? (byte)0 : encounter.Form,
            CurrentLevel = encounter.LevelMin,
            MetLocation = Locations.TeraCavern9,
            MetLevel = encounter.LevelMin,
            MetDate = EncounterDate.GetDateSwitch(),
            Version = trainerInfo.Version,
            Ball = (byte)Ball.Poke,
            ID32 = trainerInfo.ID32,
            OriginalTrainerName = trainerInfo.OT,
            OriginalTrainerGender = trainerInfo.Gender,
            Language = language,
            ObedienceLevel = encounter.LevelMin,
            OriginalTrainerFriendship = pi.BaseFriendship,
            Nickname = SpeciesName.GetSpeciesNameGeneration(encounter.Species, language, 9),
        };

        try
        {
            // Use PKHeX's exact raid generation method
            if (!Encounter9RNG.GenerateData(pk, param, criteria, seed))
                return null;

            var teraType = Tera9RNG.GetTeraType(seed, encounter.TeraType, encounter.Species, encounter.Form);
            pk.TeraTypeOriginal = (MoveType)teraType;

            if (encounter is IMoveset ms && ms.Moves.HasMoves)
                pk.SetMoves(ms.Moves);

            // For 7-star raids, set the Mightiest Mark
            if (encounter is EncounterMight9)
            {
                pk.RibbonMarkMightiest = true;
                pk.AffixedRibbon = (sbyte)RibbonIndex.MarkMightiest;
            }

            pk.ResetPartyStats();
            return pk;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Gen9RaidSeedGenerator] Error generating Pokémon: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates that a generated Pokémon matches all search criteria
    /// </summary>
    private static bool ValidateAllCriteria(PK9 pk, EncounterCriteria criteria, IVRange[] ivRanges)
    {
        // Validate shiny
        if (criteria.Shiny != Shiny.Random)
        {
            bool matches = criteria.Shiny switch
            {
                Shiny.Never => !pk.IsShiny,
                Shiny.Always => pk.IsShiny,
                Shiny.AlwaysSquare => pk.IsShiny && pk.ShinyXor == 0,
                Shiny.AlwaysStar => pk.IsShiny && pk.ShinyXor != 0,
                _ => true
            };
            if (!matches)
                return false;
        }

        // Validate nature
        if (criteria.Nature != Nature.Random && pk.Nature != criteria.Nature)
            return false;

        // Validate gender
        if (criteria.Gender != Gender.Random && pk.Gender != (int)criteria.Gender)
            return false;

        // Validate IVs
        if (!CheckIVRanges(pk, ivRanges))
            return false;

        // Validate ability (if specified)
        if (criteria.Ability != AbilityPermission.Any12H && !CheckAbilityCriteria(pk, criteria.Ability))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the Pokémon's IVs are within the specified ranges.
    /// Ranges array is in internal order: HP/Atk/Def/Spe/SpA/SpD
    /// </summary>
    private static bool CheckIVRanges(PK9 pk, IVRange[] ranges)
    {
        return pk.IV_HP >= ranges[0].Min && pk.IV_HP <= ranges[0].Max &&
               pk.IV_ATK >= ranges[1].Min && pk.IV_ATK <= ranges[1].Max &&
               pk.IV_DEF >= ranges[2].Min && pk.IV_DEF <= ranges[2].Max &&
               pk.IV_SPE >= ranges[3].Min && pk.IV_SPE <= ranges[3].Max &&
               pk.IV_SPA >= ranges[4].Min && pk.IV_SPA <= ranges[4].Max &&
               pk.IV_SPD >= ranges[5].Min && pk.IV_SPD <= ranges[5].Max;
    }

    /// <summary>
    /// Checks if the Pokémon matches the ability criteria
    /// </summary>
    private static bool CheckAbilityCriteria(PK9 pk, AbilityPermission criteria)
    {
        var pi = PersonalTable.SV[pk.Species, pk.Form];

        return (criteria, pk.AbilityNumber) switch
        {
            (AbilityPermission.OnlyFirst, 1) => pk.Ability == pi.Ability1,
            (AbilityPermission.OnlySecond, 2) => pk.Ability == pi.Ability2,
            (AbilityPermission.OnlyHidden, 4) => pk.Ability == pi.AbilityH,
            (AbilityPermission.Any12, <= 2) => true,
            (_, _) when criteria == AbilityPermission.Any12H => true,
            _ => false
        };
    }

    /// <summary>
    /// Represents an IV range for searching
    /// </summary>
    private readonly record struct IVRange(int Min, int Max);

    /// <summary>
    /// Result of a seed search operation
    /// </summary>
    private sealed class SeedSearchResult
    {
        public required uint Seed { get; init; }
        public required PK9 Pokemon { get; init; }
        public required ITeraRaid9 Encounter { get; init; }
        public required long AttemptsChecked { get; init; }
    }
}
