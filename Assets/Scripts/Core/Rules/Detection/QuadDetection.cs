using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Detection
{
    /// <summary>
    /// Détecte un Carré : ≥4 cartes de même hauteur consécutives dans la pile
    /// (vue Cards, aplatie), en traversant les Jokers de Verre (transparents,
    /// non comptés). S'arrête sur un Joker Noir/Couleur (rupture de la chaîne).
    /// Le seuil est ">=4" et non "==4" : anticipe une future PowerCard capable
    /// de pousser le cumul au-delà de 4 (voir CLAUDE.md, note QuadDetection).
    /// </summary>
    public static class QuadDetection
    {
        private const int QuadThreshold = 4;

        /// <summary>
        /// Retourne true si les dernières cartes de la pile forment un Carré (≥4).
        /// </summary>
        public static bool IsQuadDetected(IGameStateQuery state)
            => GetQuadCount(state) >= QuadThreshold;

        /// <summary>
        /// Retourne le nombre de cartes consécutives de même hauteur en fin de
        /// pile (Cards, Jokers de Verre traversés et non comptés). Retourne le
        /// cumul brut (peut dépasser 4 avec une future PowerCard) ; retourne un
        /// nombre < 4 si aucun Carré n'est formé.
        /// </summary>
        public static int GetQuadCount(IGameStateQuery state)
        {
            var cards = state.Pile.Cards;

            if (cards.Count == 0)
                return 0;

            DefRank? referenceRank = null;
            int count = 0;

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];

                if (card.IsJoker)
                {
                    if (card.JokerType == DefJokerType.Glass)
                        continue; // transparent, on continue de remonter

                    break; // Noir/Couleur : rupture de la chaîne
                }

                var rank = card.Rank!.Value;

                if (referenceRank is null)
                {
                    referenceRank = rank;
                    count = 1;
                }
                else if (rank == referenceRank.Value)
                {
                    count++;
                }
                else
                {
                    break; // hauteur différente : fin de la série
                }
            }

            return count;
        }
    }
}