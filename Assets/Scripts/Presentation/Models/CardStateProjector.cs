using System;
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Presentation.Models
{
    /// <summary>Niveau de mise en valeur d'une carte, indépendant d'UnityEngine.</summary>
    public enum CardProjection { Normal, Playable, Selected }

    /// <summary>
    /// Projection pure : sélection logique vers état visuel de chaque carte.
    /// Aucune règle de jeu : la jouabilité arrive par un délégué fourni par l'appelant
    /// (ICardPlayabilityChecker côté Core).
    /// </summary>
    public static class CardStateProjector
    {
        /// <summary>
        /// Retourne un tableau parallèle à <paramref name="cards"/>.
        /// Une carte sélectionnée est toujours Selected.
        /// Sinon Playable seulement si <paramref name="highlightPlayable"/> et que le délégué l'autorise.
        /// Les cartes nulles sont Normal.
        /// </summary>
        public static CardProjection[] Project(
            IReadOnlyList<Card> cards,
            Func<Card, bool> isSelected,
            Func<Card, bool> isPlayable,
            bool highlightPlayable)
        {
            if (cards == null) return new CardProjection[0];

            var result = new CardProjection[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i];
                if (card == null) { result[i] = CardProjection.Normal; continue; }

                if (isSelected != null && isSelected(card))
                    result[i] = CardProjection.Selected;
                else if (highlightPlayable && isPlayable != null && isPlayable(card))
                    result[i] = CardProjection.Playable;
                else
                    result[i] = CardProjection.Normal;
            }
            return result;
        }
    }
}