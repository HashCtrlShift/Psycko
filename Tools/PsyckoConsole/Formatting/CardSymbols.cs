using System;
using Psycko.Core.Domain;

namespace PsyckoConsole.Formatting
{
    /// <summary>
    /// Symboles universels des couleurs de cartes (♣ ♦ ♥ ♠).
    /// Indépendant de toute langue — réutilisable par n'importe quel formatter.
    /// </summary>
    public static class CardSymbols
    {
        public static string GetSuitSymbol(Suit suit) => suit switch
        {
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            Suit.Hearts => "♥",
            Suit.Spades => "♠",
            _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
        };
    }
}