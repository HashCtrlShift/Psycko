using System;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Point d'entrée unique pour la génération aléatoire du Deck initial.
    /// La seed ne garantit PAS la reproductibilité d'une partie entière
    /// (les décisions des bots/joueurs ne sont pas rejouables).
    /// Elle sert uniquement à recréer une distribution de cartes identique
    /// (même ordre de Deck après mélange) à partir d'une même valeur.
    /// </summary>
    public static class GameSeed
    {
        /// <summary>
        /// Crée une instance de Random dédiée au mélange du Deck,
        /// à partir d'une seed donnée.
        /// </summary>
        public static Random CreateRandom(long seed)
        {
            // Random(Int32) attend un int ; on réduit le long de façon stable.
            int intSeed = unchecked((int)(seed ^ (seed >> 32)));
            return new Random(intSeed);
        }
    }
}