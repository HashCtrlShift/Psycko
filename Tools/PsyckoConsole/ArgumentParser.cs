#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Psycko.Core.Domain.Log;

namespace Psycko.Console
{
    /// <summary>Parsing unique de la ligne de commande et des saisies du menu.</summary>
    public static class ArgumentParser
    {
        public const string Usage =
            "Usage :\n"
            + "  sim <n> <seed> <off|all|exceptional> [options]\n"
            + "  play [seed] [off|all|exceptional] [options]\n"
            + "Options : --bots a,b,c,d  --names a,b,c,d  --short N  --long X  --batch Y  --cap Z  --move-cap M  --reveal  --dry-run";

        /// <summary>Noms par défaut dérivés des sièges : Humain, puis Bot1..BotN dans l'ordre.</summary>
        public static string[] DefaultNames(SeatKind[] seats)
        {
            var names = new string[seats.Length];
            int botNumber = 1;
            for (int i = 0; i < seats.Length; i++)
                names[i] = seats[i] == SeatKind.Human ? "Humain" : $"Bot{botNumber++}";
            return names;
        }

        public static bool TryParseLogMode(string? text, out LogMode mode, out string? error)
        {
            mode = LogMode.Off;
            error = null;

            switch (text?.ToLowerInvariant())
            {
                case "off":
                    return true;
                case "all":
                    mode = LogMode.All;
                    return true;
                case "exceptional":
                    mode = LogMode.ExceptionalOnly;
                    return true;
                default:
                    error = $"Mode de log inconnu : '{text}'. Valeurs possibles : off, all, exceptional.";
                    return false;
            }
        }

        public static bool TryParseInt(string? text, string label, int min, int max, out int value, out string? error)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                error = $"{label} doit être numérique : '{text}'.";
                return false;
            }

            if (value < min || value > max)
            {
                error = $"{label} hors plage ({min} à {max}) : {value}.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryParseSeats(string? text, out SeatKind[]? seats, out string? error)
        {
            seats = null;
            error = null;

            if (text == null)
            {
                error = "--bots : valeur manquante.";
                return false;
            }

            var parts = text.Split(',');

            if (parts.Length != 4)
            {
                error = $"--bots attend exactement 4 valeurs (reçu : {parts.Length}).";
                return false;
            }

            var result = new SeatKind[4];
            for (int i = 0; i < 4; i++)
            {
                switch (parts[i].Trim().ToLowerInvariant())
                {
                    case "random":
                        result[i] = SeatKind.Random;
                        break;
                    case "human":
                        result[i] = SeatKind.Human;
                        break;
                    case "discard":
                        result[i] = SeatKind.Discard;
                        break;
                    default:
                        error = $"Type de bot inconnu : '{parts[i]}'. Valeurs possibles : random, human, discard.";
                        return false;
                }
            }

            if (result.Count(x => x == SeatKind.Human) > 1)
            {
                error = "Un seul siège humain est autorisé.";
                return false;
            }

            seats = result;
            return true;
        }

        public static bool TryParseNames(string? text, out string[]? names, out string? error)
        {
            names = null;
            error = null;

            if (text == null)
            {
                error = "--names : valeur manquante.";
                return false;
            }

            var parsed = text.Split(',').Select(x => x.Trim()).ToArray();

            if (parsed.Length != 4
                || parsed.Any(string.IsNullOrWhiteSpace)
                || parsed.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
            {
                error = "--names attend 4 noms non vides et distincts.";
                return false;
            }

            names = parsed;
            return true;
        }

        /// <summary>
        /// Parse la ligne de commande. Précondition : args.Length > 0
        /// (le cas sans argument est géré par Program.Main, qui lance le menu).
        /// </summary>
        public static bool TryParse(
            string[] args,
            [NotNullWhen(true)] out RunConfiguration? configuration,
            out string? error)
        {
            configuration = null;
            error = null;

            if (args.Length == 0)
            {
                error = "Aucune commande fournie.";
                return false;
            }

            string command = args[0].ToLowerInvariant();
            if (command != "sim" && command != "play")
            {
                error = $"Commande inconnue : '{args[0]}'.";
                return false;
            }

            // Valeurs par défaut : source unique = ExceptionalGameCriteria et RunConfiguration.
            var defaults = new ExceptionalGameCriteria();

            bool isSim = command == "sim";
            int index = 1;
            int games = 1;
            int seed = Environment.TickCount;
            LogMode logMode = isSim ? LogMode.ExceptionalOnly : LogMode.All;
            bool reveal = false;
            bool dryRun = false;
            SeatKind[] seats = isSim
                ? new[] { SeatKind.Random, SeatKind.Random, SeatKind.Random, SeatKind.Random }
                : new[] { SeatKind.Human, SeatKind.Random, SeatKind.Random, SeatKind.Random };
            string[]? names = null; // null = non fourni : noms dérivés des sièges après la boucle d'options
            int shortThreshold = defaults.MinMovesThreshold;
            int longThreshold = defaults.MaxMovesThreshold;
            int moveCap = defaults.MoveCap;
            int batch = RunConfiguration.DefaultBatchSize;
            int cap = RunConfiguration.DefaultLogCap;

            if (isSim)
            {
                if (args.Length < 4)
                {
                    error = "sim attend <n> <seed> <off|all|exceptional>.";
                    return false;
                }

                if (!TryParseInt(args[index++], "Nombre de parties", 1, int.MaxValue, out games, out error))
                    return false;
                if (!TryParseInt(args[index++], "Seed", int.MinValue, int.MaxValue, out seed, out error))
                    return false;
                if (!TryParseLogMode(args[index++], out logMode, out error))
                    return false;
            }
            else
            {
                if (index < args.Length && !args[index].StartsWith("--"))
                {
                    if (!TryParseInt(args[index++], "Seed", int.MinValue, int.MaxValue, out seed, out error))
                        return false;
                }

                if (index < args.Length && !args[index].StartsWith("--"))
                {
                    if (!TryParseLogMode(args[index++], out logMode, out error))
                        return false;
                }
            }

            while (index < args.Length)
            {
                string option = args[index++];

                if (option == "--reveal")
                {
                    if (isSim)
                    {
                        error = "--reveal est réservé à play.";
                        return false;
                    }

                    reveal = true;
                    continue;
                }

                if (option == "--dry-run")
                {
                    dryRun = true;
                    continue;
                }

                if (!option.StartsWith("--"))
                {
                    error = $"Option inconnue : '{option}'.";
                    return false;
                }

                if (index >= args.Length)
                {
                    error = $"Valeur manquante pour {option}.";
                    return false;
                }

                string value = args[index++];

                switch (option)
                {
                    case "--bots":
                        if (!TryParseSeats(value, out var parsedSeats, out error)) return false;
                        seats = parsedSeats!;
                        break;
                    case "--names":
                        if (!TryParseNames(value, out var parsedNames, out error)) return false;
                        names = parsedNames!;
                        break;
                    case "--short":
                        if (!TryParseInt(value, "--short", 0, int.MaxValue, out shortThreshold, out error)) return false;
                        break;
                    case "--long":
                        if (!TryParseInt(value, "--long", 1, int.MaxValue, out longThreshold, out error)) return false;
                        break;
                    case "--batch":
                        if (!TryParseInt(value, "--batch", 1, int.MaxValue, out batch, out error)) return false;
                        break;
                    case "--cap":
                        if (!TryParseInt(value, "--cap", 1, int.MaxValue, out cap, out error)) return false;
                        break;
                    case "--move-cap":
                        if (!TryParseInt(value, "--move-cap", 1, int.MaxValue, out moveCap, out error)) return false;
                        break;
                    default:
                        error = $"Option inconnue : '{option}'.";
                        return false;
                }
            }

            names ??= DefaultNames(seats);

            if ((long)seed + games - 1 > int.MaxValue)
            {
                error = $"Plage de seeds invalide : seed {seed} + {games} parties dépasse {int.MaxValue}.";
                return false;
            }

            if (shortThreshold >= longThreshold)
            {
                error = "--short doit être inférieur à --long.";
                return false;
            }

            try
            {
                configuration = new RunConfiguration(
                    isSim ? RunMode.Sim : RunMode.Play,
                    games, seed, logMode, reveal,
                    new ExceptionalGameCriteria(
                        defaults.TopN, defaults.BottomN, shortThreshold, longThreshold, moveCap, defaults.IncludeErrors),
                    seats, names, dryRun, batch, cap);
                error = null;
                return true;
            }
            catch (ArgumentException ex)
            {
                configuration = null;
                error = ex.Message;
                return false;
            }
        }
    }
}