#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Bots;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using Psycko.Core.Services.Log;

namespace Psycko.Console
{
    public enum LogMode { Off, All, ExceptionalOnly }

    /// <summary>
    /// Usage :
    ///   sim [parties=1000] [seedDépart=1] [off|all|exceptional]
    ///   play [seed]        (toi au siège 0 contre 3 RandomBot, log complet)
    /// </summary>
    public static class Program
    {
        private const string LogDirectory = "Logs"; // relatif à Tools/PsyckoConsole/
        private static readonly string[] SeatNames = { "J1", "J2", "J3", "J4" };

        public static int Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "sim";
            return mode == "play" ? Play(args) : Simulate(args);
        }

        // ───────────── Simulation ─────────────
        private static int Simulate(string[] args)
        {
            int games = args.Length > 1 ? int.Parse(args[1]) : 1000;
            int startSeed = args.Length > 2 ? int.Parse(args[2]) : 1;
            var logMode = ParseLogMode(args.Length > 3 ? args[3] : "exceptional");
            var criteria = new ExceptionalGameCriteria(); // X, N à caler après petites simulations

            var botRandom = new Random(); // décisions NON reproductibles (choix assumé)
            var checker = new CardPlayabilityChecker();
            var agents = SeatNames.Select(_ => (IPlayerAgent)new RandomBot(botRandom, checker)).ToList();

            var kept = new List<(GameLog Log, GameResult Result, string Reason)>();
            var shortest = new List<(GameLog Log, GameResult Result)>();
            var longest = new List<(GameLog Log, GameResult Result)>();
            var allResults = new List<GameResult>(games);
            var allLogs = new List<GameLog>();

            for (int i = 0; i < games; i++)
            {
                int seed = startSeed + i;
                var outcome = GameRunner.Run(seed, agents, SeatNames, recordLog: logMode != LogMode.Off);
                allResults.Add(outcome.Result);

                if (outcome.Error != null)
                    System.Console.WriteLine($"[ERREUR] seed {seed} : {outcome.Error.Message}");

                if (outcome.Log == null) continue;

                if (logMode == LogMode.All) { allLogs.Add(outcome.Log); continue; }

                // ExceptionalOnly : critères individuels au fil de l'eau…
                if (ExceptionalGameDetector.IsExceptional(outcome.Log, criteria, outcome.Error != null))
                {
                    kept.Add((outcome.Log, outcome.Result, outcome.Error != null ? "erreur" : "> seuil"));
                    continue;
                }

                // …et tampons des N plus courtes / N plus longues (les autres logs sont libérés).
                if (!outcome.Result.IsError)
                {
                    KeepExtreme(shortest, (outcome.Log, outcome.Result), criteria.BottomN, longest: false);
                    KeepExtreme(longest, (outcome.Log, outcome.Result), criteria.TopN, longest: true);
                }
            }

            PrintStats(allResults);

            if (logMode == LogMode.All)
            {
                Write(allLogs, allResults);
            }
            else if (logMode == LogMode.ExceptionalOnly)
            {
                kept.AddRange(shortest.Select(x => (x.Log, x.Result, "plus courte")));
                kept.AddRange(longest.Select(x => (x.Log, x.Result, "plus longue")));
                var unique = kept.GroupBy(k => k.Log.Seed).Select(g => g.First()).OrderBy(k => k.Log.Seed).ToList();

                foreach (var k in unique)
                    System.Console.WriteLine($"Exceptionnelle : seed {k.Log.Seed} ({k.Reason}, {k.Result.TotalMoves} coups)");

                if (unique.Count > 0)
                    Write(unique.Select(k => k.Log).ToList(), unique.Select(k => k.Result).ToList());
            }

            return allResults.Any(r => r.IsError) ? 1 : 0;
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
            int seed = args.Length > 1 ? int.Parse(args[1]) : Environment.TickCount;
            var botRandom = new Random();
            var checker = new CardPlayabilityChecker();

            var agents = new List<IPlayerAgent>
            {
                new ConsoleHumanAgent(checker),
                new RandomBot(botRandom, checker),
                new RandomBot(botRandom, checker),
                new RandomBot(botRandom, checker),
            };

            var outcome = GameRunner.Run(seed, agents, SeatNames, recordLog: true);

            System.Console.WriteLine(outcome.Error != null
                ? $"Partie interrompue : {outcome.Error.Message}"
                : $"Fin de partie. Psycko : siège {outcome.Result.PsyckoSeat}");

            // Toujours écrit, même partie abandonnée ('q') ou en erreur.
            Write(new List<GameLog> { outcome.Log! }, new List<GameResult> { outcome.Result });
            return 0;
        }

        // ───────────── Utilitaires ─────────────
        private static void Write(IReadOnlyList<GameLog> logs, IReadOnlyList<GameResult> results)
        {
            foreach (var path in CsvLogWriter.WriteToFiles(logs, results, LogDirectory))
                System.Console.WriteLine($"Log écrit : {path}");
        }

        private static void PrintStats(IReadOnlyList<GameResult> results)
        {
            var ok = results.Where(r => !r.IsError).Select(r => r.TotalMoves).OrderBy(x => x).ToList();
            System.Console.WriteLine($"Parties : {results.Count} | erreurs : {results.Count(r => r.IsError)}");
            if (ok.Count == 0) return;
            System.Console.WriteLine(
                $"Coups min {ok.First()} | moy {ok.Average():0.#} | méd {ok[ok.Count / 2]} | max {ok.Last()}");
        }

        private static LogMode ParseLogMode(string value) => value.ToLowerInvariant() switch
        {
            "off" => LogMode.Off,
            "all" => LogMode.All,
            _ => LogMode.ExceptionalOnly,
        };
    }
}