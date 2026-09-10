using System;
using Psycko.Core.Domain;

namespace PsyckoConsole.Formatting
{
    /// <summary>
    /// Formatter d'affichage console en français.
    /// Réutilise les symboles universels (CardSymbols) et n'ajoute que
    /// les noms de rangs/Jokers en français.
    /// </summary>
    public sealed class CardFormatterFrench : ICardFormatter
    {
        public string Format(Card card)
        {
            if (card.IsJoker)
            {
                return FormatJoker(card.JokerType!.Value);
            }

            string rankLabel = FormatRank(card.Rank!.Value);
            string suitSymbol = CardSymbols.GetSuitSymbol(card.Suit!.Value);

            return $"{rankLabel}{suitSymbol}";
        }

        private static string FormatRank(CardRank rank) => rank switch
        {
            CardRank.Three => "3",
            CardRank.Four => "4",
            CardRank.Five => "5",
            CardRank.Six => "6",
            CardRank.Seven => "7",
            CardRank.Eight => "8",
            CardRank.Nine => "9",
            CardRank.Ten => "10",
            CardRank.Priest => "Prêtre",
            CardRank.Jack => "Valet",
            CardRank.Knight => "Cavalier",
            CardRank.Queen => "Dame",
            CardRank.King => "Roi",
            CardRank.Ace => "As",
            CardRank.Two => "2",
            _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, null)
        };

        private static string FormatJoker(JokerType jokerType) => jokerType switch
        {
            JokerType.Glass => "Joker de Verre",
            JokerType.Black => "Joker Noir",
            JokerType.Color => "Joker Couleur",
            _ => throw new ArgumentOutOfRangeException(nameof(jokerType), jokerType, null)
        };
    }
}