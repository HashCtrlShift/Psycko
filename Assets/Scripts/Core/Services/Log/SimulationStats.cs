using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Psycko.Core.Domain.Log;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Statistiques dérivées d'un lot de GameResult (jamais de GameLog : fonctionne en LogMode.Off).
    /// Écart-type : formule de POPULATION. Médiane : moyenne des deux centraux si effectif pair.
    /// Rang : 0 = vainqueur ... dernier = Psycko. Siège = index 0..n-1 (Bot1 = siège 0).
    /// </summary>
    public sealed class SimulationStats
    {
        public int TotalGames { get; }
        public int ErrorGames { get; }
        public int SuccessfulGames => TotalGames - ErrorGames;
        public double ErrorRatePercent => TotalGames == 0 ? 0 : 100.0 * ErrorGames / TotalGames;

        // --- Durées (en coups globaux) ---
        public double AverageMoves { get; }
        public double MedianMoves { get; }
        public int MinMoves { get; }
        public int MaxMoves { get; }
        public int SeedOfMinGame { get; }
        public int SeedOfMaxGame { get; }
        public double StdDevMoves { get; }

        // --- Par siège (index = siège) ---
        public IReadOnlyList<int> WinsBySeat { get; }
        public IReadOnlyList<int> PsyckosBySeat { get; }
        public IReadOnlyList<double> WinRateBySeat { get; }

        /// <summary>Moyenne des Plays du siège à sa sortie (parties où il sort).</summary>
        public IReadOnlyList<double> AveragePlaysAtExitBySeat { get; }

        /// <summary>Moyenne du coup de sortie du siège (parties où il sort).</summary>
        public IReadOnlyList<double> AverageExitMoveBySeat { get; }

        /// <summary>Moyenne des ramassages du siège par partie.</summary>
        public IReadOnlyList<double> AveragePickupsBySeat { get; }

        /// <summary>
        /// Moyenne, sur les parties, de (Plays du vainqueur à sa sortie / coups globaux à sa sortie).
        /// Mesure la fluidité d'une partie : proche de 1/4 = tout le monde joue, bas = beaucoup de ramassages.
        /// </summary>
        public double AverageWinnerPlayRatio { get; }

        public SimulationStats(IReadOnlyList<GameResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            TotalGames = results.Count;
            var ok = results.Where(r => !r.IsError).ToList();
            ErrorGames = TotalGames - ok.Count;

            int seats = ok.Count > 0 ? ok[0].PlaysBySeat.Count : 0;

            var wins = new int[seats];
            var psyckos = new int[seats];
            var exitPlaysSum = new double[seats];
            var exitMoveSum = new double[seats];
            var exitCount = new int[seats];
            var pickupSum = new double[seats];
            double ratioSum = 0;
            int ratioCount = 0;

            foreach (var r in ok)
            {
                if (r.WinnerSeat.HasValue) wins[r.WinnerSeat.Value]++;
                if (r.PsyckoSeat.HasValue) psyckos[r.PsyckoSeat.Value]++;

                for (int s = 0; s < seats; s++)
                {
                    pickupSum[s] += r.PickupsBySeat[s];
                    if (r.ExitMoveBySeat[s] >= 0)
                    {
                        exitPlaysSum[s] += r.PlaysAtExitBySeat[s];
                        exitMoveSum[s] += r.ExitMoveBySeat[s];
                        exitCount[s]++;
                    }
                }

                if (r.WinnerSeat.HasValue && r.ExitMoveBySeat[r.WinnerSeat.Value] > 0)
                {
                    int w = r.WinnerSeat.Value;
                    ratioSum += (double)r.PlaysAtExitBySeat[w] / r.ExitMoveBySeat[w];
                    ratioCount++;
                }
            }

            WinsBySeat = wins;
            PsyckosBySeat = psyckos;
            WinRateBySeat = wins.Select(w => ok.Count == 0 ? 0 : 100.0 * w / ok.Count).ToArray();
            AveragePlaysAtExitBySeat = Enumerable.Range(0, seats)
                .Select(s => exitCount[s] == 0 ? 0 : exitPlaysSum[s] / exitCount[s]).ToArray();
            AverageExitMoveBySeat = Enumerable.Range(0, seats)
                .Select(s => exitCount[s] == 0 ? 0 : exitMoveSum[s] / exitCount[s]).ToArray();
            AveragePickupsBySeat = pickupSum.Select(p => ok.Count == 0 ? 0 : p / ok.Count).ToArray();
            AverageWinnerPlayRatio = ratioCount == 0 ? 0 : ratioSum / ratioCount;

            if (ok.Count > 0)
            {
                var moves = ok.Select(r => r.TotalMoves).OrderBy(m => m).ToList();
                AverageMoves = moves.Average();
                MinMoves = moves[0];
                MaxMoves = moves[moves.Count - 1];
                SeedOfMinGame = ok.First(r => r.TotalMoves == MinMoves).Seed;
                SeedOfMaxGame = ok.First(r => r.TotalMoves == MaxMoves).Seed;
                int mid = moves.Count / 2;
                MedianMoves = moves.Count % 2 == 1 ? moves[mid] : (moves[mid - 1] + moves[mid]) / 2.0;
                StdDevMoves = Math.Sqrt(moves.Sum(m => (m - AverageMoves) * (m - AverageMoves)) / moves.Count);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Statistiques de simulation ===");
            sb.AppendLine($"Parties : {TotalGames} (terminées : {SuccessfulGames}, erreurs : {ErrorGames} = {ErrorRatePercent:F4} %)");
            sb.AppendLine($"Coups   : moy {AverageMoves:F1} | méd {MedianMoves:F1} | écart-type {StdDevMoves:F1}");
            sb.AppendLine($"          min {MinMoves} (seed {SeedOfMinGame}) | max {MaxMoves} (seed {SeedOfMaxGame})");
            sb.AppendLine($"Ratio Plays/coups du vainqueur à sa sortie (moy) : {AverageWinnerPlayRatio:P1}");
            sb.AppendLine("Siège | Victoires (%) | Psycko | Plays à la sortie | Coup de sortie | Ramassages/partie");
            for (int s = 0; s < WinsBySeat.Count; s++)
                sb.AppendLine($"Bot{s + 1,-2} | {WinsBySeat[s],9} ({WinRateBySeat[s]:F2}) | {PsyckosBySeat[s],6} | {AveragePlaysAtExitBySeat[s],17:F1} | {AverageExitMoveBySeat[s],14:F1} | {AveragePickupsBySeat[s],8:F1}");
            return sb.ToString();
        }
    }
}