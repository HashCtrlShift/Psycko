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
            => IsPlayable(card, state.Pile, state.Constraint, state.RefRank);

        /// <summary>
        /// Consommateur : Bots/Presentation — valide un coup à partir de la vue filtrée
        /// d'un joueur (IPlayerVisibleState), sans jamais exposer l'état complet.
        /// </summary>
        public static bool IsPlayable(Card card, IPlayerVisibleState state)
            => IsPlayable(card, state.Pile, state.Constraint, state.RefRank);

        /// <summary>
        /// Cœur unique de la règle de jouabilité, partagé par les deux surcharges
        /// publiques ci-dessus. Ne doit jamais être dupliqué ailleurs.
        /// </summary>
        private static bool IsPlayable(Card card, Pile pile, HeightConstraint constraint, DefRank refRank)
        {
            // Pile vide : aucune contrainte de hauteur, tout est jouable.
            if (pile.IsEmpty)
            {
                return true;
            }

            // Les Jokers sont toujours jouables, quelle que soit la contrainte active.
            if (card.IsJoker)
            {
                return true;
            }

            // Une carte standard sans rang est une corruption de l'état interne,
            // pas un rejet métier : les Card sont construites par leurs factories.
            if (!card.Rank.HasValue)
                throw new System.InvalidOperationException(
                    "Invariant violé : une carte non-Joker doit toujours avoir un rang.");

            DefRank candidate = card.Rank.Value;

            return constraint switch
            {
                HeightConstraint.PriestReversed => HeightComparison.IsLessOrEqual(candidate, refRank),
                _ => HeightComparison.IsGreaterOrEqual(candidate, refRank),
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

        /// <summary>Surcharge Bots/Presentation de GetPlayableCards, via IPlayerVisibleState.</summary>
        public static IEnumerable<Card> GetPlayableCards(IReadOnlyList<Card> cards, IPlayerVisibleState state)
        {
            return cards.Where(card => IsPlayable(card, state));
        }
    }
}