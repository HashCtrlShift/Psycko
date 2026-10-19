#nullable enable

using Psycko.Bots;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using Psycko.Core.Services.Log;

namespace Psycko.Console
{
    /// <summary>
    /// Usage :
    ///   sim [parties=1000] [seedDépart=1] [off|all|exceptional]
    ///   play [seed]        (toi au siège 0 contre 3 RandomBot, log complet)
    /// </summary>
    public static class Program
    {
        private const string LogDirectory = "Logs"; // relatif à Tools/PsyckoConsole/
        private const int ProgressStep = 10000;

        // Mode All : un fichier CSV tous les LogBatchSize parties (mémoire bornée).
        private const int LogBatchSize = 50;

        // Mode Exceptional : écriture par lots, plafonnée en nombre total de logs détaillés.
        private const int ExceptionalBatchSize = 50;
        private const int ExceptionalLogCap = 200;

        private static readonly string[] SimNames = { "Bot1", "Bot2", "Bot3", "Bot4" };
        private static readonly string[] PlayNames = { "Humain", "Bot1", "Bot2", "Bot3" };

        public static int Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length == 0) return ConsoleGameLocal.Run();
            string mode = args[0].ToLowerInvariant();
            return mode == "play" ? Play(args) : Simulate(args);
        }

        // ───────────── Simulation ─────────────
        private static int Simulate(string[] args)
        {
            int games = args.Length > 1 ? int.Parse(args[1]) : 1000;
            int startSeed = args.Length > 2 ? int.Parse(args[2]) : 1;
            var logMode = ParseLogMode(args.Length > 3 ? args[3] : "exceptional");

            // Valeurs par défaut : N=10, Y=250, X=6000, plafond=250000.
            var criteria = new ExceptionalGameCriteria();

            var botRandom = new Random(); // décisions NON reproductibles (choix assumé)
            var checker = new CardPlayabilityChecker();
            var agents = SimNames.Select(_ => (IPlayerAgent)new RandomBot(botRandom, checker)).ToList();

            // Tampons des extrêmes : bornés à TopN / BottomN.
            var shortest = new List<(GameLog Log, GameResult Result)>();
            var longest = new List<(GameLog Log, GameResult Result)>();
            var allResults = new List<GameResult>(games);

            // Tampon d'écriture (modes All et Exceptional) : borné, vidé par lots.
            var batchLogs = new List<GameLog>(LogBatchSize);
            var batchResults = new List<GameResult>(LogBatchSize);

            int exceptionalDetected = 0;
            int exceptionalLogged = 0;

            if (games < 1 || (long)startSeed + games > int.MaxValue)
            {
                System.Console.WriteLine(
                    $"Plage de seeds invalide : seedDépart {startSeed} + {games} parties dépasse {int.MaxValue}.");
                return 2;
            }

            for (int i = 0; i < games; i++)
            {
                int seed = startSeed + i;
                var outcome = GameRunner.Run(
                    seed, agents, SimNames,
                    recordLog: logMode != LogMode.Off,
                    moveCap: criteria.MoveCap);
                allResults.Add(outcome.Result);
                if ((i + 1) % ProgressStep == 0)
                    System.Console.WriteLine($"[{i + 1}/{games}]");

                if (outcome.Error != null)
                {
                    System.Console.WriteLine($"[ERREUR] seed {seed} : {outcome.Error.Message}");
                }

                if (outcome.Log == null) continue; // LogMode.Off

                if (logMode == LogMode.All)
                {
                    batchLogs.Add(outcome.Log);
                    batchResults.Add(outcome.Result);
                    if (batchLogs.Count >= LogBatchSize)
                        FlushBatch(batchLogs, batchResults);
                    continue;
                }

                // ExceptionalOnly : critères individuels au fil de l'eau.
                if (ExceptionalGameDetector.IsExceptional(outcome.Result, criteria))
                {
                    exceptionalDetected++;
                    string reason = Reason(outcome, criteria);
                    System.Console.WriteLine(
                        $"Exceptionnelle : seed {seed} ({reason}, {outcome.Result.TotalMoves} coups)");

                    if (exceptionalLogged < ExceptionalLogCap)
                    {
                        exceptionalLogged++;
                        batchLogs.Add(outcome.Log);
                        batchResults.Add(outcome.Result);
                        if (batchLogs.Count >= ExceptionalBatchSize)
                            FlushBatch(batchLogs, batchResults);
                    }
                    // Au-delà du plafond : détectée et comptée, log libéré.
                    continue;
                }

                // Tampons des N plus courtes / N plus longues (les autres logs sont libérés).
                // Ici la partie n'est pas en erreur (une erreur est déjà exceptionnelle si IncludeErrors).
                if (!outcome.Result.IsError)
                {
                    KeepExtreme(shortest, (outcome.Log, outcome.Result), criteria.BottomN, longest: false);
                    KeepExtreme(longest, (outcome.Log, outcome.Result), criteria.TopN, longest: true);
                }
            }

            System.Console.WriteLine(new SimulationStats(allResults));

            // Reliquat du tampon (All ou Exceptional).
            FlushBatch(batchLogs, batchResults);

            if (logMode == LogMode.ExceptionalOnly)
            {
                var extremes = new List<(GameLog Log, GameResult Result)>();
                extremes.AddRange(shortest);
                extremes.AddRange(longest);

                foreach (var x in shortest)
                    System.Console.WriteLine(
                        $"Extrême Courte : seed {x.Log.Seed} (plus courte, {x.Result.TotalMoves} coups)");
                foreach (var x in longest)
                    System.Console.WriteLine(
                        $"Extrême Longue : seed {x.Log.Seed} (plus longue, {x.Result.TotalMoves} coups)");

                if (extremes.Count > 0)
                {
                    var ordered = extremes.OrderBy(x => x.Log.Seed).ToList();
                    Write(ordered.Select(x => x.Log).ToList(), ordered.Select(x => x.Result).ToList());
                }

                int notLogged = exceptionalDetected - exceptionalLogged;
                System.Console.WriteLine(
                    $"Exceptionnelles : {exceptionalDetected} détectées, {exceptionalLogged} loguées, " +
                    $"{notLogged} non loguées (plafond {ExceptionalLogCap}). " +
                    $"Extrêmes écrites : {extremes.Count}.");
            }

            return allResults.Any(r => r.IsError) ? 1 : 0;
        }

        private static string Reason(GameRunOutcome outcome, ExceptionalGameCriteria criteria)
        {
            if (outcome.Error != null)
            {
                return outcome.Error.Message.Contains("Plafond anti-boucle")
                    ? $"plafond {criteria.MoveCap} coups"
                    : "erreur";
            }

            return outcome.Result.TotalMoves < criteria.MinMovesThreshold
                ? $"< {criteria.MinMovesThreshold} coups"
                : $"> {criteria.MaxMovesThreshold} coups";
        }

        private static void FlushBatch(List<GameLog> logs, List<GameResult> results)
        {
            if (logs.Count == 0) return;
            Write(logs, results);
            logs.Clear();
            results.Clear();
        }

        private static void KeepExtreme(List<(GameLog Log, GameResult Result)> buffer,
            (GameLog Log, GameResult Result) candidate, int n, bool longest)
        {
            if (n <= 0) return;
            buffer.Add(candidate);
            if (buffer.Count <= n) return;

            var drop = longest
                ? buffer.OrderBy(x => x.Result.TotalMoves).First()
                : buffer.OrderByDescending(x => x.Result.TotalMoves).First();
            buffer.Remove(drop);
        }

                // ───────────── Partie humaine ─────────────
        private static int Play(string[] args)
        {
            bool reveal = args.Any(a => a == "--reveal");
            var positional = args.Skip(1).Where(a => a != "--reveal").ToArray();

            int seed = positional.Length > 0 ? int.Parse(positional[0]) : Environment.TickCount;
            string modeText = positional.Length > 1 ? positional[1].ToLowerInvariant() : "all";

            LogMode logMode;
            switch (modeText)
            {
                case "off": logMode = LogMode.Off; break;
                case "all": logMode = LogMode.All; break;
                case "exceptional": logMode = LogMode.ExceptionalOnly; break;
                default:
                    System.Console.WriteLine($"Mode de logs inconnu : '{modeText}'.");
                    System.Console.WriteLine("Usage : play [seed] [off|all|exceptional] [--reveal]");
                    return 2;
            }

            var botRandom = new Random();
            var checker = new CardPlayabilityChecker();
            var agents = new List<IPlayerAgent>
            {
                new ConsoleHumanAgent(checker),
                new RandomBot(botRandom, checker),
                new RandomBot(botRandom, checker),
                new RandomBot(botRandom, checker),
            };

            var criteria = new ExceptionalGameCriteria();
            var printer = new ConsoleLogPrinter(PlayNames, reveal);
            var outcome = GameRunner.Run(
                seed, agents, PlayNames,
                recordLog: logMode != LogMode.Off,
                moveCap: criteria.MoveCap,
                observer: printer);

            System.Console.WriteLine(outcome.Error != null
                ? $"Partie interrompue : {outcome.Error.Message}"
                : $"Fin de partie. Psycko : {PlayNames[outcome.Result.PsyckoSeat ?? 0]}");

            if (outcome.Log != null &&
                (logMode == LogMode.All ||
                 (logMode == LogMode.ExceptionalOnly &&
                  ExceptionalGameDetector.IsExceptional(outcome.Result, criteria))))
            {
                Write(new List<GameLog> { outcome.Log }, new List<GameResult> { outcome.Result });
            }

            return 0;
        }

        // ───────────── Utilitaires ─────────────
        private static void Write(IReadOnlyList<GameLog> logs, IReadOnlyList<GameResult> results)
        {
            foreach (var path in CsvLogWriter.WriteToFiles(logs, results, LogDirectory))
                System.Console.WriteLine($"Log écrit : {path}");
        }

        private static LogMode ParseLogMode(string value) => value.ToLowerInvariant() switch
        {
            "off" => LogMode.Off,
            "all" => LogMode.All,
            _ => LogMode.ExceptionalOnly,
        };
    }
}