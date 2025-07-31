using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace PKHeX.Core.AutoMod;

public static class BattleTemplateLegality
{
    public static string ANALYSIS_INVALID { get; set; } = "El análisis específico para este conjunto no está disponible.";
    public static string EXHAUSTED_ENCOUNTERS { get; set; } = "### __Error__\n- No hay un encuentro válido disponible: (Agotados **{0}/{1}** posibles encuentros).\n\n```No hay un encuentro en la base de datos que pueda corresponder al conjunto solicitado.\n\n📝Soluciones:\n• Por favor, verifica bien la informacion del conjunto e intentalo de nuevo.```\n```🔊Advertencia:\n• Debido a que Home ya no permite que los Pokémon generados en juegos que no son de origen (como generar un Enamorus en SV) sean depositados en Home ahora van a ser considerados ilegales y eso significa que ya no podrás generar ningún Pokémon que no esté disponible de forma nativa en cada juego.```\n### __Consejo__\n Puedes verificar la lista completa de pokemons abajo:\n- Lista de pokemons que no se pueden pedir al bot sin un archivo **pkm** con un __**Home Tracker**__ valido: [(Click Aqui)](https://i.imgur.com/8wMvQXa.png).\n- Lista de pokemons con __**Shiny Lock**__ en la ultima generación: [(Click Aqui)](https://i.imgur.com/vCFWPei.png).";
    public static string SPECIES_UNAVAILABLE_FORM { get; set; } = "### __Error__\n- **{0}** con la forma **{1}** no esta disponible en este juego.\n\n```La forma solicitada para este pokemon no esta disponible en el juego.. Por favor, intentelo con la forma regular del pokemon.```";
    public static string SPECIES_UNAVAILABLE { get; set; } = "### Error\n- **{0}** no esta disponible en el juego.\n\n```📝Soluciones:\n• Comprueba que el nombre del pokemon esta escrito correctamente y en ingles.\n\n• Puede que el pokemon solicitado no se encuentre en el juego.. Por favor, verifica la lista de pokemons obtenibles en el juego e intentalo de nuevo.```";
    public static string INVALID_MOVES { get; set; } = "### __Error__\n- **{0}** no puede aprender los siguientes movimientos en este juego: **{1}**.";
    public static string ALL_MOVES_INVALID { get; set; } = "### __Error__\n- Todos los movimientos solicitados para este Pokémon no son válidos.\n\n```📝Soluciones:\n• Cambia los movimientos o verifica que no estes solicitando una forma del pokemon que no puede ser obtenida por medio de intercambio.```\n```🔊Advertencia:\n• Debido a que Home ya no permite que los Pokémon generados en juegos que no son de origen (como generar un Enamorus en SV) sean depositados en Home ahora van a ser considerados ilegales y eso significa que ya no podrás generar ningún Pokémon que no esté disponible de forma nativa en cada juego.```\n### __Consejo__\n Puedes verificar la lista completa de pokemons abajo:\n- Lista de pokemons que no se pueden pedir al bot sin un archivo **pkm** con un __**Home Tracker**__ valido: [(Click Aqui)](https://i.imgur.com/8wMvQXa.png).\n- Lista de pokemons con __**Shiny Lock**__ en la ultima generación: [(Click Aqui)](https://i.imgur.com/vCFWPei.png).";
    public static string LEVEL_INVALID { get; set; } = "### __Error__\n- El nivel solicitado es inferior al nivel mínimo posible para **{0}**. El nivel mínimo requerido es **{1}**.\n\n```📝Soluciones:\n• Cambia el nivel del pokemon solicitado al nivel {1}```";
    public static string SHINY_INVALID { get; set; } = "### __Error__\n- Valor shiny establecido **(ShinyType.{0})** no es posible para el conjunto solicitado.\n\n```📝Soluciones:\n• Verificar que no se esta solicitando un pokemon con Shiny Lock, de ser el caso puedes eliminar (Shiny: Yes) del conjunto!```\n### __Consejo__\n- Puedes verificar la lista de pokemons con Shiny Lock aqui [(Click Aqui)](https://i.imgur.com/vCFWPei.png)";
    public static string ALPHA_INVALID { get; set; } = "### __Error__\n- El Pokémon solicitado no pueden ser alfa.";
    public static string BALL_INVALID { get; set; } = "### __Error__\n- **{0} Ball** no es posible para el conjunto solicitado.";
    public static string ONLY_HIDDEN_ABILITY_AVAILABLE { get; set; } = "### __Error__\n- Sólo se puede obtener **{0}** con habilidad oculta en este juego.";
    public static string HIDDEN_ABILITY_UNAVAILABLE { get; set; } = "### __Error__\n- No puedes obtener **{0}** con habilidad oculta en este juego.";
    public static string HOME_TRANSFER_ONLY { get; set; } = "### __Error__\n- **{0}** sólo está disponible en este juego a través de __**Home Transfer**__.";
    public static string BAD_WORDS { get; set; } = "### __Error__\n- El apodo, OT o HT de **{0}** contiene una palabra filtrada.\n\n```📝Soluciones:\n• Cambia el apodo, OT o HT del Pokémon para eliminar palabras prohibidas o inapropiadas.\n\n• Asegúrate de usar nombres que cumplan con las reglas del bot y del juego.```";

    public static string VerifyPokemonName(string inputName, int language)
    {
        if (!SpeciesName.TryGetSpecies(inputName, language, out ushort species))
        {
            return $"### Error\n- El nombre introducido no fue reconocido como un Pokémon válido.\n\n```📝Soluciones:\n• Comprueba que el nombre del Pokémon esté escrito correctamente y en inglés.\n\n• Utiliza el nombre oficial del Pokémon en el juego.```";
        }
        return null; // No hay error, el nombre es válido
    }

    public static string SetAnalysis(this IBattleTemplate set, ITrainerInfo sav, PKM failed)
    {
        if (failed.Version == 0)
            failed.Version = sav.Version;

        var species_name = SpeciesName.GetSpeciesNameGeneration(set.Species, (int)LanguageID.English, sav.Generation);
        var nameCheckError = VerifyPokemonName(species_name, (int)LanguageID.English);
        if (nameCheckError != null)
            return nameCheckError; // Si el nombre no es válido, retorna el mensaje de error.
        var analysis = set.Form == 0 ? string.Format(SPECIES_UNAVAILABLE, species_name) : string.Format(SPECIES_UNAVAILABLE_FORM, species_name, set.FormName);

        // Species checks
        var gv = sav.Version;
        if (!gv.ExistsInGame(set.Species, set.Form))
            return analysis; // Species does not exist in the game

        // Species exists -- check if it has at least one move.
        // If it has no moves, and it didn't generate, that makes the mon still illegal in game (moves are set to legal ones)
        Memory<ushort> moves = set.Moves;
        var empty = moves.Span.IndexOf<ushort>(0);
        if (empty != -1)
            moves = moves[..empty];

        // Reusable data
        var destVer = sav.Version;
        if (destVer <= 0 && sav is SaveFile s)
            destVer = s.Version;

        var gamelist = APILegality.FilteredGameList(failed, destVer, APILegality.AllowBatchCommands, set);

        // Move checks
        var bestCombination = GetValidMovesetWithMostPresent(set, sav, moves, failed, gamelist);
        if (bestCombination.Length != moves.Length)
        {
            if (bestCombination.Length == 0)
                return ALL_MOVES_INVALID;
            var sb = new StringBuilder();
            AddMovesNotPresentIn(moves.Span, bestCombination, sb);
            return string.Format(INVALID_MOVES, species_name, sb);
        }

        // All moves possible, get encounters
        var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: failed, moves, gamelist).ToList();
        var initialcount = encounters.Count;
        if (set is RegenTemplate { Regen.EncounterFilters: { } x })
            encounters.RemoveAll(enc => !BatchEditing.IsFilterMatch(x, enc));

        // No available encounters
        if (encounters.Count == 0)
            return string.Format(EXHAUSTED_ENCOUNTERS, initialcount, initialcount);

        // Level checks, check if level is impossible to achieve
        if (encounters.All(z => !APILegality.IsRequestedLevelValid(set, z)))
            return string.Format(LEVEL_INVALID, species_name, encounters.Min(z => z.LevelMin));

        encounters.RemoveAll(enc => !APILegality.IsRequestedLevelValid(set, enc));

        // Shiny checks, check if shiny is impossible to achieve
        Shiny shinytype = set.Shiny ? Shiny.Always : Shiny.Never;
        if (set is RegenTemplate { Regen.HasExtraSettings: true } ret)
            shinytype = ret.Regen.Extra.ShinyType;

        if (encounters.All(z => !APILegality.IsRequestedShinyValid(set, z)))
            return string.Format(SHINY_INVALID, shinytype);

        encounters.RemoveAll(enc => !APILegality.IsRequestedShinyValid(set, enc));

        // Alpha checks
        if (encounters.All(z => !APILegality.IsRequestedAlphaValid(set, z)))
            return ALPHA_INVALID;

        encounters.RemoveAll(enc => !APILegality.IsRequestedAlphaValid(set, enc));
        if (WordFilter.IsFiltered(failed.Nickname, failed.Context, out _, out _) || WordFilter.IsFiltered(failed.OriginalTrainerName, failed.Context, out _, out _) || WordFilter.IsFiltered(failed.HandlingTrainerName, failed.Context, out _, out _))
            return string.Format(BAD_WORDS, species_name);

        // Ability checks
        var abilityreq = APILegality.GetRequestedAbility(failed, set);
        if (abilityreq == AbilityRequest.NotHidden && encounters.All(z => z is { Ability: AbilityPermission.OnlyHidden }))
            return string.Format(ONLY_HIDDEN_ABILITY_AVAILABLE, species_name);

        if (abilityreq == AbilityRequest.Hidden && encounters.All(z => z.Generation is 3 or 4) && destVer.GetGeneration() < 8)
            return string.Format(HIDDEN_ABILITY_UNAVAILABLE, species_name);

        // Home Checks
        if (!APILegality.AllowHOME)
        {
            if (encounters.All(z => HomeTrackerUtil.IsRequired(z, failed)))
                return string.Format(HOME_TRANSFER_ONLY, species_name);

            encounters.RemoveAll(enc => HomeTrackerUtil.IsRequired(enc, failed));
        }

        // Ball checks
        if (set is RegenTemplate { Regen.HasExtraSettings: true } regt)
        {
            var ball = regt.Regen.Extra.Ball;
            if (encounters.All(z => !APILegality.IsRequestedBallValid(set, z)))
                return string.Format(BALL_INVALID, ball);

            encounters.RemoveAll(enc => !APILegality.IsRequestedBallValid(set, enc));
        }

        return string.Format(EXHAUSTED_ENCOUNTERS, initialcount - encounters.Count, initialcount);
    }

    private static void AddMovesNotPresentIn(ReadOnlySpan<ushort> check, ReadOnlySpan<ushort> set, StringBuilder sb)
    {
        foreach (var move in check)
        {
            if (set.Contains(move))
                continue;
            if (move == 0)
                continue;
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append($"{(Move)move}");
        }
    }

    private static ReadOnlySpan<ushort> GetValidMovesetWithMostPresent(IBattleTemplate set, ITrainerInfo sav, Memory<ushort> moves, PKM blank, GameVersion[] gamelist)
    {
        if (sav.Generation <= 2)
            blank.EXP = 0; // no relearn moves in gen 1/2 so pass level 1 to generator

        // Eager check: current moveset is valid
        if (HasAnyEncounterForMoves(set, blank, moves, gamelist))
            return moves.Span;

        // Okay, at least one move is invalid. Recursively permute combinations to find the moveset with most moves valid.
        moves = moves.ToArray(); // copy to not disturb the original array.
        var count = Recurse(set, moves, blank, gamelist, [..moves.Span]);
        // The moves array is now the most-populated combination of moves that are valid.
        return moves.Span[..count];
    }

    private static int Recurse(IBattleTemplate set, Memory<ushort> request, PKM blank, GameVersion[] gamelist, List<ushort> moves)
    {
        if (moves.Count <= 1)
            return 0;

        // Breadth first search to find the most valid moveset -- remove one move and check, and restore if not.
        request = request[..(moves.Count - 1)];
        for (int i = 0; i < moves.Count; i++)
        {
            // Original order doesn't matter, skip an array copy shift when reinserting
            // This essentially cycles them like a queue
            var move = moves[0];
            moves.RemoveAt(0);
            moves.CopyTo(request.Span);
            if (HasAnyEncounterForMoves(set, blank, request, gamelist))
                return moves.Count;
            moves.Add(move);
        }

        // If above failed, recurse the same as above with more moves removed.
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[0];
            moves.RemoveAt(0);
            var count = Recurse(set, request, blank, gamelist, moves);
            if (count != 0) // ignore 0, the removed move might be valid in a different combination
                return count;
            moves.Add(move);
        }
        return 0;
    }

    private static bool HasAnyEncounterForMoves(IBattleTemplate set, PKM blank,
        ReadOnlyMemory<ushort> moves, GameVersion[] gamelist)
    {
        // Do we even need to set the moves to the template?
        Span<ushort> tmp = stackalloc ushort[4];
        moves.Span.CopyTo(tmp);
        blank.SetMoves(tmp);

        var encounters = EncounterMovesetGenerator.GenerateEncounters(blank, moves, gamelist);
        if (set is not RegenTemplate { Regen.EncounterFilters: { Count: not 0 } x })
            return encounters.Any();
        encounters = encounters.Where(enc => BatchEditing.IsFilterMatch(x, enc));
        return encounters.Any();
    }
}
