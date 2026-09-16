using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Comparison;

namespace Psycko.Core.Rules.Validation
{
    /// <summary>
    /// Détermine si une carte est jouable compte tenu de l'état courant de la partie
    /// (contrainte de hauteur active, référence de hauteur, jokers, etc.).
    /// </summary>
    public static class CardPlayability
    {
        /// <summary>
        /// Consommateur : TurnManager — valide un Play précis soumis par joueur/bot.
        /// </summary>
        public static bool IsPlayable(Card card, IGameStateQuery state)
        {
            // Pile vide : aucune contrainte de hauteur, tout est jouable.
            if (state.Pile.IsEmpty)
            {
                return true;
            }

            // Les Jokers sont toujours jouables, quelle que soit la contrainte active.
            if (card.IsJoker)
            {
                return true;
            }

            // card.Rank est garanti non-null ici (IsJoker == false).
            DefRank candidate = card.Rank!.Value;

            return state.Constraint switch
            {
                HeightConstraint.PriestReversed => HeightComparison.IsLessOrEqual(candidate, state.RefRank),
                _ => HeightComparison.IsGreaterOrEqual(candidate, state.RefRank),
            };
        }

        /// <summary>
        /// Consommateur : Presentation — filtre les cartes jouables d'une couche donnée
        /// (main, FaceUp, ou FaceDown) pour mise en valeur visuelle.
        /// </summary>
        public static IEnumerable<Card> GetPlayableCards(IReadOnlyList<Card> cards, IGameStateQuery state)
        {
            return cards.Where(card => IsPlayable(card, state));
        }
    }
}