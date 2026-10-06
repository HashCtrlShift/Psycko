using System;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services.Log.Format
{
    /// <summary>Formateur français : "7♥", "Prêtre♣", "Joker Bombe".</summary>
    public sealed class CardFormatter : ICardFormatter
    {
        public string Format(Card card)
        {
            if (card is null) throw new ArgumentNullException(nameof(card));

            if (card.IsJoker)
            {
                if (card.JokerType is null)
                    throw new ArgumentException("Joker sans JokerType.", nameof(card));
                return JokerLabel(card.JokerType.Value);
            }

            if (card.Rank is null || card.Suit is null)
                throw new ArgumentException("Carte standard sans Rank ou Suit.", nameof(card));

            return RankLabel(card.Rank.Value) + CardSymbols.Of(card.Suit.Value);
        }

        public static string RankLabel(DefRank rank) => rank switch
        {
            DefRank.Three  => "3",
            DefRank.Four   => "4",
            DefRank.Five   => "5",
            DefRank.Six    => "6",
            DefRank.Seven  => "7",
            DefRank.Eight  => "8",
            DefRank.Nine   => "9",
            DefRank.Ten    => "10",
            DefRank.Priest => "Prêtre",
            DefRank.Jack   => "Valet",
            DefRank.Knight => "Cavalier",
            DefRank.Queen  => "Dame",
            DefRank.King   => "Roi",
            DefRank.Ace    => "As",
            DefRank.Two    => "2",
            _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, null)
        };

        public static string JokerLabel(DefJokerType type) => type switch
        {
            DefJokerType.Glass => "Joker de Verre",
            DefJokerType.Black => "Joker Noir",
            DefJokerType.Color => "Joker Bombe",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}