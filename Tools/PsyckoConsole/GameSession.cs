using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain.Log;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using Psycko.Core.Services.Log;

namespace Psycko.Console
{
    /// <summary>Moteur d'exécution unique : simulation et partie jouée.</summary>
    public static class GameSession
    {
        private const string LogDirectory = "Logs"; // relatif à Tools/PsyckoConsole/
        private const int ProgressStep = 10000;
        private const int MaxErrorLines = 20;

        public static int Run(RunConfiguration configuration)
        {
            return configuration.Mode == RunMode.Play ? Play(configuration) : Simulate(configuration);
        }

        // ───────────── Simulation ─────────────
        private static int Simulate(RunConfiguration configuration)
        {
            int games = configuration.GameCount;
            int startSeed = configuration.StartSeed;
            var logMode = configuration.LogMode;
            var criteria = configuration.Criteria;
            string[] names = configuration.Names.ToArray();
            int progressStep = Math.Max(1, Math.Min(ProgressStep, games / 10));

            var checker = new CardPlayabilityChecker();
            if (!BotFactory.TryCreateAgents(configuration.Seats, checker, out var agents, out var factoryError))
            {
                System.Console.WriteLine(factoryError);
                return 2;
            }

            var shortest = new List<(GameLog Log, GameResult Result)>();
            var longest = new List<(GameLog Log, GameResult Result)>();
            var allResults = new List<GameResult>(games);
            var batchLogs = new List<GameLog>(configuration.BatchSize);
            var batchResults = new List<GameResult>(configuration.BatchSize);

            int exceptionalDetected = 0;
            int exceptionalLogged = 0;
            int errorsSeen = 0;

            for (int i = 0; i < games; i++)
            {
                int seed = startSeed + i;
                var outcome = GameRunner.Run(
                    seed, agents, names,
                    recordLog: logMode != LogMode.Off,
                    moveCap: criteria.MoveCap);
                allResults.Add(outcome.Result);

                if ((i + 1) % progressStep == 0 && i + 1 < games)
                    System.Console.WriteLine($"[{i + 1}/{games}]");

                if (outcome.Error != null)
                {
                    errorsSeen++;
                    if (errorsSeen <= MaxErrorLines)
                        System.Console.WriteLine(
                            $"[ERREUR] seed {seed} : {outcome.Error.ToString()} (TotalMoves={outcome.Result.TotalMoves})");
                }

                if (outcome.Log == null) continue; // LogMode.Off

                if (logMode == LogMode.All)
                {
                    batchLogs.Add(outcome.Log);
                    batchResults.Add(outcome.Result);
                    if (batchLogs.Count >= configuration.BatchSize)
                        FlushBatch(batchLogs, batchResults);
                    continue;
                }

                // ExceptionalOnly : critères individuels au fil de l'eau.
                if (ExceptionalGameDetector.IsExceptional(outcome.Result, criteria))
                {
                    exceptionalDetected++;
                    System.Console.WriteLine(
                        $"Exceptionnelle : seed {seed} ({Reason(outcome, criteria)}, {outcome.Result.TotalMoves} coups)");

                    if (exceptionalLogged < configuration.LogCap)
                    {
                        exceptionalLogged++;
                        batchLogs.Add(outcome.Log);
                        batchResults.Add(outcome.Result);
                        if (batchLogs.Count >= configuration.BatchSize)
                            FlushBatch(batchLogs, batchResults);
                    }

                    continue;
                }

                if (!outcome.Result.IsError)
                {
                    KeepExtreme(shortest, (outcome.Log, outcome.Result), criteria.BottomN, longest: false);
                    KeepExtreme(longest, (outcome.Log, outcome.Result), criteria.TopN, longest: true);
                }
            }

            if (errorsSeen > MaxErrorLines)
                System.Console.WriteLine($"... {errorsSeen - MaxErrorLines} autres erreurs non affichées.");

            System.Console.WriteLine(new SimulationStats(allResults));
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

                var ordered = extremes
                    .GroupBy(x => x.Log.Seed)
                    .Select(g => g.First())
                    .OrderBy(x => x.Log.Seed)
                    .ToList();

                if (ordered.Count > 0)
                    Write(ordered.Select(x => x.Log).ToList(), ordered.Select(x => x.Result).ToList());

                System.Console.WriteLine(
                    $"Exceptionnelles : {exceptionalDetected} détectées, {exceptionalLogged} loguées, "
                    + $"{exceptionalDetected - exceptionalLogged} non loguées (plafond {configuration.LogCap}). "
                    + $"Extrêmes écrites : {ordered.Count}.");
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

        private static void KeepExtreme(
            List<(GameLog Log, GameResult Result)> buffer,
            (GameLog Log, GameResult Result) candidate,
            int n,
            bool longest)
        {
            if (n <= 0) return;
            buffer.Add(candidate);
            if (buffer.Count <= n) return;

            var drop = longest
                ? buffer.OrderBy(x => x.Result.TotalMoves).First()
                : buffer.OrderByDescending(x => x.Result.TotalMoves).First();
            buffer.Remove(drop);
        }

        // ───────────── Partie jouée ─────────────
        private static int Play(RunConfiguration configuration)
        {
            var checker = new CardPlayabilityChecker();
            if (!BotFactory.TryCreateAgents(configuration.Seats, checker, out var agents, out var factoryError))
            {
                System.Console.WriteLine(factoryError);
                return 2;
            }

            var names = configuration.Names.ToArray();
            int humanSeat = configuration.Seats.ToList().IndexOf(SeatKind.Human);
            var printer = new ConsoleLogPrinter(names, configuration.Reveal, humanSeat);

            var outcome = GameRunner.Run(
                configuration.StartSeed, agents, names,
                recordLog: configuration.LogMode != LogMode.Off,
                moveCap: configuration.Criteria.MoveCap,
                observer: printer);

            System.Console.WriteLine(outcome.Error != null
                ? $"Partie interrompue : {outcome.Error.Message}"
                : $"Fin de partie. Psycko : {names[outcome.Result.PsyckoSeat ?? 0]}");

            bool write = outcome.Log != null
                && (configuration.LogMode == LogMode.All
                    || (configuration.LogMode == LogMode.ExceptionalOnly
                        && ExceptionalGameDetector.IsExceptional(outcome.Result, configuration.Criteria)));

            if (write)
                Write(new List<GameLog> { outcome.Log }, new List<GameResult> { outcome.Result });

            return outcome.Error != null ? 1 : 0;
        }

        private static void Write(IReadOnlyList<GameLog> logs, IReadOnlyList<GameResult> results)
        {
            foreach (var path in CsvLogWriter.WriteToFiles(logs, results, LogDirectory))
                System.Console.WriteLine($"Log écrit : {path}");
        }
    }
}