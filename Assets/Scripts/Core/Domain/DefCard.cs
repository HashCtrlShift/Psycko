namespace Psycko.Core.Domain
{
    /// <summary>
    /// Définit les rangs disponibles pour les cartes standards (hors Jokers).
    /// </summary>
    public enum DefRank

    {
        Three = 0,
        Four = 1,
        Five = 2,
        Six = 3,
        Seven = 4,
        Eight = 5,
        Nine = 6,
        Ten = 7,
        Priest = 8,      // Prêtre
        Jack = 9,        // Valet
        Knight = 10,     // Cavalier
        Queen = 11,      // Dame
        King = 12,       // Roi
        Ace = 13,        // As
        Two = 14         // 2 (ne peut pas terminer une phase)
    }
    /// <summary>
    /// Définit les couleurs disponibles pour les cartes standards (hors Jokers).
    /// </summary>
    public enum DefSuit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

/// <summary>
/// Définit les types de Jokers disponibles dans le jeu.
/// </summary>
    public enum DefJokerType
    {
        Glass,   // Joker de Verre
        Black,   // Joker Noir / Passe
        Color    // Joker Couleur / Bombe
    }
}