using System;
using Psycko.Core.Domain;

namespace Psycko.Presentation.UI
{
    /// <summary>
    /// Mapping pur Card -> clé de sprite.
    /// Standard : "{Suit}_{Rank}" (ex: "Hearts_Ace").
    /// Joker    : "Joker_{JokerType}" (ex: "Joker_Glass").
    /// Le dos de carte n'a pas de clé (ICardSkin.GetBack()).
    /// </summary>
    public static class CardSpriteKey
    {
        public static string From(Card card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));

            if (card.IsJoker)
                return "Joker_" + card.JokerType.Value;

            return card.Suit.Value + "_" + card.Rank.Value;
        }

        /// <summary>Libellé lisible pour le placeholder.</summary>
        public static string Label(Card card)
        {
            return From(card).Replace('_', ' ');
        }
    }
}