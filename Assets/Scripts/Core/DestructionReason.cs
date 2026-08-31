namespace Psycko
{
    /// <summary>
    /// Raison de la destruction/fermeture de la pile.
    /// Square et Two entraînent un rejeu du même joueur.
    /// Bomb (Joker Couleur) fait passer la main au joueur suivant.
    /// </summary>
    public enum DestructionReason
    {
        Square,   // Carré (4 cartes même rang, Joker Glass transparent, Joker Black casse la chaîne)
        Two,      // Carte "2" jouée
        Bomb      // Joker Couleur / Bombe
    }
    
}