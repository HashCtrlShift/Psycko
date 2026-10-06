using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services.Log.Format
{
    /// <summary>Symboles de couleur universels (aucune langue).</summary>
    public static class CardSymbols
    {
        public const string Hearts   = "♥";
        public const string Diamonds = "♦";
        public const string Clubs    = "♣";
        public const string Spades   = "♠";

        public static string Of(DefSuit suit) => suit switch
        {
            DefSuit.Hearts   => Hearts,
            DefSuit.Diamonds => Diamonds,
            DefSuit.Clubs    => Clubs,
            DefSuit.Spades   => Spades,
            _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
        };
    }
}