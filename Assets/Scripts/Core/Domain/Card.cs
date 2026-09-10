using System;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Représente une carte unique et immuable du paquet Psycko (63 cartes).
    /// Construction exclusivement via les factory methods CreateStandard / CreateJoker.
    /// </summary>
    public sealed record Card
    {
        public bool IsJoker { get; }
        public DefSuit? Suit { get; }
        public DefRank? Rank { get; }
        public DefJokerType? JokerType { get; }

        private Card(bool isJoker, DefSuit? suit, DefRank? rank, DefJokerType? jokerType)
        {
            IsJoker = isJoker;
            Suit = suit;
            Rank = rank;
            JokerType = jokerType;
        }

        /// <summary>
        /// Crée une carte standard (Rank x Suit), aucune notion de Joker.
        /// </summary>
        public static Card CreateStandard(DefSuit suit, DefRank rank)
        {
            return new Card(isJoker: false, suit: suit, rank: rank, jokerType: null);
        }

        /// <summary>
        /// Crée un Joker unique (Verre, Noir, Couleur). Pas de Suit, pas de Rank.
        /// </summary>
        public static Card CreateJoker(DefJokerType jokerType)
        {
            return new Card(isJoker: true, suit: null, rank: null, jokerType: jokerType);
        }
    }
}