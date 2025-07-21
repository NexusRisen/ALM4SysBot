using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

internal static class PokemonDataHelper
{
    public static List<string> GetAvailableGames(ushort species, byte form)
    {
        var games = new List<string>();

        foreach (var version in GameUtil.GameVersions)
        {
            if (version.ExistsInGame(species, form))
            {
                games.Add(version.ToString());
            }
        }

        return games;
    }

    public static string GetGenderInfo(PersonalInfo pi, ushort species, byte form)
    {
        var ratio = pi.Gender;
        return ratio switch
        {
            PersonalInfo.RatioMagicGenderless => "- Genderless only",
            PersonalInfo.RatioMagicFemale => "- Female only (100% female)",
            PersonalInfo.RatioMagicMale => "- Male only (100% male)",
            _ => $"- Gender ratio: {GetGenderRatioString(ratio)}"
        };
    }

    public static string GetGenderRatioString(int ratio)
    {
        var femaleRatio = ratio / 255.0 * 100;
        var maleRatio = 100 - femaleRatio;
        return $"{maleRatio:F1}% male, {femaleRatio:F1}% female";
    }

    public static bool IsGenderValid(PersonalInfo pi, byte gender) =>
        pi.Gender switch
        {
            PersonalInfo.RatioMagicGenderless => gender == 2,
            PersonalInfo.RatioMagicFemale => gender == 1,
            PersonalInfo.RatioMagicMale => gender == 0,
            _ => gender < 2
        };

    public static string GetEggGroups(PersonalInfo pi)
    {
        var groups = new List<string>();

        if (pi.EggGroup1 != 0)
            groups.Add(((EggGroup)pi.EggGroup1).ToString());
        if (pi.EggGroup2 != 0 && pi.EggGroup2 != pi.EggGroup1)
            groups.Add(((EggGroup)pi.EggGroup2).ToString());

        return groups.Any() ? string.Join(", ", groups) : "Cannot breed";
    }

    public static List<string> GetValidAbilities(PersonalInfo pi, int generation)
    {
        var abilities = new List<string>();
        var strings = GameInfo.Strings;

        if (pi is IPersonalAbility12 pa12)
        {
            if (pa12.Ability1 != 0)
                abilities.Add($"{strings.abilitylist[pa12.Ability1]} (1)");
            if (pa12.Ability2 != 0)
                abilities.Add($"{strings.abilitylist[pa12.Ability2]} (2)");

            if (pa12 is IPersonalAbility12H pah && pah.AbilityH != 0)
                abilities.Add($"{strings.abilitylist[pah.AbilityH]} (Hidden)");
        }
        else
        {
            for (int i = 0; i < pi.AbilityCount; i++)
            {
                var abilityId = pi.GetAbilityAtIndex(i);
                if (abilityId != 0)
                    abilities.Add($"{strings.abilitylist[abilityId]} ({i + 1})");
            }
        }

        var uniqueAbilities = new List<string>();
        var seenNames = new HashSet<string>();

        foreach (var ability in abilities)
        {
            var abilityName = ability.Split(' ')[0];
            if (!seenNames.Contains(abilityName))
            {
                uniqueAbilities.Add(ability);
                seenNames.Add(abilityName);
            }
        }

        return uniqueAbilities;
    }

    public static List<(string Move, int Level)> GetValidMovesWithLevels(ushort species, byte form, int generation, GameVersion version)
    {
        var movesWithLevels = new Dictionary<ushort, int>();
        var strings = GameInfo.Strings;

        try
        {
            var learnSource = GameData.GetLearnSource(version);
            var learnset = learnSource.GetLearnset(species, form);
            var allMoves = learnset.GetAllMoves();

            foreach (var moveId in allMoves)
            {
                if (moveId == 0) continue;

                if (learnset.TryGetLevelLearnMove(moveId, out byte level))
                {
                    if (!movesWithLevels.TryGetValue(moveId, out int value) || level < value)
                    {
                        value = level;
                        movesWithLevels[moveId] = value;
                    }
                }
            }

            var allAvailableMoves = learnset.GetMoveRange(100);
            foreach (var moveId in allAvailableMoves)
            {
                if (moveId != 0 && !movesWithLevels.ContainsKey(moveId))
                {
                    movesWithLevels[moveId] = 0;
                }
            }

            var result = new List<(string Move, int Level)>();
            foreach (var kvp in movesWithLevels)
            {
                var moveName = strings.movelist[kvp.Key];
                result.Add((moveName, kvp.Value));
            }

            return [.. result.OrderBy(m => m.Move)];
        }
        catch (Exception)
        {
            var result = new List<(string Move, int Level)>();

            try
            {
                var learnSource = GameData.GetLearnSource(version);
                var learnset = learnSource.GetLearnset(species, form);
                var allMoves = learnset.GetMoveRange(100);

                foreach (var moveId in allMoves)
                {
                    if (moveId == 0) continue;

                    var moveName = strings.movelist[moveId];

                    if (learnset.TryGetLevelLearnMove(moveId, out byte level))
                    {
                        result.Add((moveName, level));
                    }
                    else
                    {
                        result.Add((moveName, 0));
                    }
                }
            }
            catch
            {
                var moves = GetValidMoves(species, form, generation, version);
                foreach (var move in moves)
                {
                    result.Add((move, 0));
                }
            }

            return [.. result.OrderBy(m => m.Move)];
        }
    }

    public static List<string> GetValidMoves(ushort species, byte form, int generation, GameVersion version, byte level = 100)
    {
        var moves = new HashSet<string>();
        var strings = GameInfo.Strings;

        var pk = EntityBlank.GetBlank((byte)generation);
        pk.Species = species;
        pk.Form = form;
        pk.CurrentLevel = level;

        var learnSource = GameData.GetLearnSource(version);
        var learnset = learnSource.GetLearnset(species, form);
        foreach (var move in learnset.GetMoveRange(level))
        {
            if (move != 0)
                moves.Add(strings.movelist[move]);
        }

        foreach (var move in learnset.GetMoveRange(100))
        {
            if (move != 0)
                moves.Add(strings.movelist[move]);
        }

        return [.. moves.OrderBy(m => m)];
    }

    public static List<string> GetValidBalls(ushort species, byte form, int generation, GameVersion version)
    {
        var balls = new List<string>();
        var wildBalls = BallUseLegality.GetWildBalls((byte)generation, version);

        for (byte ballId = 1; ballId < 64; ballId++)
        {
            if (BallUseLegality.IsBallPermitted(wildBalls, ballId))
            {
                var ball = (Ball)ballId;
                balls.Add(ball.ToString());
            }
        }

        if (generation >= 6)
        {
            for (byte ballId = 1; ballId < 64; ballId++)
            {
                var ball = (Ball)ballId;
                if (ball != Ball.Master && ball != Ball.Cherish &&
                    BallContextHOME.Instance.CanBreedWithBall(species, form, ball))
                {
                    if (!balls.Contains(ball.ToString()))
                        balls.Add($"{ball} (Breeding)");
                }
            }
        }

        return balls;
    }

    public static bool CanHaveHiddenAbility(ushort species, byte form, int generation)
    {
        if (generation < 5)
            return false;

        if (generation <= 6)
            return true;

        return true;
    }
}