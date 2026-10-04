using System.Collections.Generic;

namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Log complet et immuable d'une partie, de la distribution initiale
    /// jusqu'à la désignation du Psycko (entrée GameEnded = fin de partie).
    /// </summary>
    public sealed class GameLog
    {
        /// <summary>
        /// Seed ayant servi à générer la distribution initiale (cf. T37).
        /// </summary>
        public int Seed { get; }

        /// <summary>
        /// Liste ordonnée des entrées, dans l'ordre chronologique de la partie.
        /// La dernière entrée est toujours de type GameEnded.
        /// </summary>
        public IReadOnlyList<GameLogEntry> Entries { get; }

        public GameLog(int seed, IReadOnlyList<GameLogEntry> entries)
        {
            Seed = seed;
            Entries = entries ?? System.Array.Empty<GameLogEntry>();
        }
    }
}