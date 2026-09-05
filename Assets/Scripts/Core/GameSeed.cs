using System;

namespace Psycko.Core
{
    /// <summary>
    /// Utilitaire de dérivation de seeds pour garantir la reproductibilité totale d'une partie.
    /// Une seed racine dérive de façon déterministe : la seed du Deck et une seed par bot/joueur.
    /// Vit en Core car ce mécanisme doit être utilisable côté serveur (Photon Fusion) plus tard,
    /// pas seulement en outil de simulation console.
    /// </summary>
    public static class GameSeed
    {
        /// <summary>
        /// Dérive la seed du Deck depuis la seed racine de partie.
        /// </summary>
        public static int DeriveDeckSeed(int rootSeed)
        {
            return Derive(rootSeed, 0);
        }

        /// <summary>
        /// Dérive la seed d'un bot/joueur à un index de siège donné (0-3) depuis la seed racine.
        /// </summary>
        public static int DerivePlayerSeed(int rootSeed, int seatIndex)
        {
            if (seatIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(seatIndex));

            return Derive(rootSeed, seatIndex + 1);
        }

        /// <summary>
        /// Dérivation déterministe simple : combine la seed racine et un offset via un hash stable.
        /// Ne dépend d'aucune source d'aléatoire externe (pas de DateTime, pas de Guid).
        /// </summary>
        private static int Derive(int rootSeed, int offset)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + rootSeed;
                hash = hash * 31 + offset;
                // Évite les seeds négatives pour rester cohérent avec Deck(int seed >= 0)
                return hash & 0x7FFFFFFF;
            }
        }
    }
}