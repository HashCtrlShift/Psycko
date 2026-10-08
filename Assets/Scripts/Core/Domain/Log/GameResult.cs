using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Faits bruts d'une partie terminée. Immuable, neutre : aucune notion
    /// d'ELO ni de niveau (réutilisable par le futur système de classement).
    /// Les index des tableaux sont les index de SIÈGE (0..n-1).
    /// </summary>
    public sealed class GameResult
    {
        public int Seed { get; }

        /// <summary>Nombre de coups globaux (Plays + ramassages) de la partie.</summary>
        public int TotalMoves { get; }

        /// <summary>Nombre total de Plays (Play + BlindPlay) de la partie.</summary>
        public int TotalPlays { get; }

        /// <summary>Sièges classés : [0] = vainqueur, dernier = Psycko. Vide si partie en erreur.</summary>
        public IReadOnlyList<int> RankingBySeat { get; }

        /// <summary>Siège du Psycko, null si partie en erreur.</summary>
        public int? PsyckoSeat => RankingBySeat.Count > 0 ? RankingBySeat[RankingBySeat.Count - 1] : (int?)null;

        /// <summary>Siège du vainqueur, null si partie en erreur.</summary>
        public int? WinnerSeat => RankingBySeat.Count > 0 ? RankingBySeat[0] : (int?)null;

        /// <summary>Coup global au moment où chaque siège est passé Finished (-1 : jamais, ex. Psycko).</summary>
        public IReadOnlyList<int> ExitMoveBySeat { get; }

        /// <summary>Plays du siège au moment de sa sortie (-1 : jamais).</summary>
        public IReadOnlyList<int> PlaysAtExitBySeat { get; }

        /// <summary>Plays totaux par siège sur toute la partie.</summary>
        public IReadOnlyList<int> PlaysBySeat { get; }

        /// <summary>Ramassages par siège sur toute la partie.</summary>
        public IReadOnlyList<int> PickupsBySeat { get; }

        /// <summary>True si la partie a été interrompue par une exception.</summary>
        public bool IsError { get; }

        public GameResult(
            int seed,
            int totalMoves,
            int totalPlays,
            IEnumerable<int> rankingBySeat,
            IEnumerable<int> exitMoveBySeat,
            IEnumerable<int> playsAtExitBySeat,
            IEnumerable<int> playsBySeat,
            IEnumerable<int> pickupsBySeat,
            bool isError = false)
        {
            Seed = seed;
            TotalMoves = totalMoves;
            TotalPlays = totalPlays;
            RankingBySeat = (rankingBySeat ?? throw new ArgumentNullException(nameof(rankingBySeat))).ToList().AsReadOnly();
            ExitMoveBySeat = (exitMoveBySeat ?? throw new ArgumentNullException(nameof(exitMoveBySeat))).ToList().AsReadOnly();
            PlaysAtExitBySeat = (playsAtExitBySeat ?? throw new ArgumentNullException(nameof(playsAtExitBySeat))).ToList().AsReadOnly();
            PlaysBySeat = (playsBySeat ?? throw new ArgumentNullException(nameof(playsBySeat))).ToList().AsReadOnly();
            PickupsBySeat = (pickupsBySeat ?? throw new ArgumentNullException(nameof(pickupsBySeat))).ToList().AsReadOnly();
            IsError = isError;
        }

        /// <summary>Résultat d'une partie interrompue : seul le seed est exploitable.</summary>
        public static GameResult Error(int seed, int playerCount)
        {
            var empty = Enumerable.Repeat(-1, playerCount);
            var zeros = Enumerable.Repeat(0, playerCount);
            return new GameResult(seed, 0, 0, Enumerable.Empty<int>(),
                empty, empty, zeros, zeros, isError: true);
        }
    }
}