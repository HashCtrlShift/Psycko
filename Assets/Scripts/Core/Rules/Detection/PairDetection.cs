using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Detection
{
    /// <summary>
    /// Détecte un Doublon : le coup courant (dernier Play de la pile) a la même
    /// hauteur effective que le Play précédent, en traversant les coups
    /// composés d'un Joker de Verre seul (transparents pour la hauteur).
    /// Un coup multi-cartes (ex. 2x3) n'est PAS un Doublon à lui seul : seule
    /// compte la comparaison entre le dernier coup et le coup précédent.
    /// Ne détermine PAS la priorité Carré/Doublon : c'est à l'appelant
    /// (TurnManager) d'interroger QuadDetection en premier.
    /// </summary>
    public static class PairDetection
    {
        /// <summary>
        /// Retourne true si le dernier coup posé forme un Doublon avec le coup
        /// précédent (même hauteur effective, Jokers de Verre traversés).
        /// Retourne false si la pile a moins de 2 coups pertinents, ou si un
        /// Joker Noir/Couleur est rencontré avant de trouver un coup à hauteur.
        /// </summary>
        public static bool IsPairDetected(IGameStateQuery state)
        {
            var plays = state.Pile.Plays;

            if (plays.Count < 2)
                return false;

            var lastPlay = plays[^1];

            // Le dernier coup doit être un coup à hauteur (pas un Joker seul)
            // pour pouvoir déclencher un Doublon.
            if (!lastPlay.EffectiveRank.HasValue)
                return false;

            DefRank lastRank = lastPlay.EffectiveRank.Value;

            // Remonte les coups précédents, en traversant les Jokers de Verre
            // (transparents), et en s'arrêtant sur tout autre Joker (Noir/Couleur
            // réinitialisent la hauteur/la pile : pas de Doublon possible au-delà).
            for (int i = plays.Count - 2; i >= 0; i--)
            {
                var play = plays[i];

                if (play.IsJokerPlay)
                {
                    if (play.JokerType == DefJokerType.Glass)
                        continue; // transparent, on continue de remonter

                    return false; // Noir/Couleur : rupture, pas de Doublon
                }

                // Premier coup à hauteur rencontré en remontant.
                return play.EffectiveRank!.Value == lastRank;
            }

            return false;
        }
    }
}