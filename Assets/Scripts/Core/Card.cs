using System;
using System.Collections.Generic;

public struct Card : IEquatable<Card>
{
    // Cas 1 : Carte standard (rank + suit, jokerType inutilisé)
    public CardRank Rank { get; }
    public CardSuit Suit { get; }

    // Cas 2 : Joker (jokerType seul, rank/suit sans sens)
    public JokerType? JokerType { get; }

    // Constructeur pour carte standard
    public Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
        JokerType = null;
    }

    // Constructeur pour Joker
    public Card(JokerType jokerType)
    {
        Rank = CardRank.Three;  // Valeur par défaut, non utilisée
        Suit = CardSuit.Spades; // Valeur par défaut, non utilisée
        JokerType = jokerType;
    }

    public bool IsJoker => JokerType.HasValue;

    public bool IsStandardCard => !IsJoker;

    public override bool Equals(object obj) => obj is Card c && Equals(c);

    public bool Equals(Card other)
    {
        if (IsJoker && other.IsJoker)
            return JokerType == other.JokerType;

        if (!IsJoker && !other.IsJoker)
            return Rank == other.Rank && Suit == other.Suit;

        return false;
    }

    public override int GetHashCode()
    {
        if (IsJoker)
            return HashCode.Combine("Joker", JokerType);
        return HashCode.Combine(Rank, Suit);
    }

    public override string ToString()
    {
        if (IsJoker)
            return $"Joker({JokerType})";

        string rankStr = Rank switch
        {
            CardRank.Priest => "Prêtre",
            CardRank.Jack => "Valet",
            CardRank.Knight => "Cavalier",
            CardRank.Queen => "Dame",
            CardRank.King => "Roi",
            CardRank.Ace => "As",
            CardRank.Two => "2",
            _ => Rank.ToString()
        };

        string suitStr = Suit switch
        {
            CardSuit.Spades => "♠",
            CardSuit.Hearts => "♥",
            CardSuit.Clubs => "♣",
            CardSuit.Diamonds => "♦",
            _ => "?"
        };

        return $"{rankStr}{suitStr}";
    }

    // Opérateurs de comparaison pour struct IEquatable
    public static bool operator ==(Card left, Card right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Card left, Card right)
    {
        return !left.Equals(right);
    }
}