namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Effet déclenché par une carte ou une combinaison de cartes jouée.
    /// Une seule entrée de log peut combiner plusieurs effets
    /// (ex. Carré + Destruction Pile).
    /// </summary>
    public enum CardEffectType
    {
        None,
        Doublon,
        Carre,
        DestructionPile,
        ChangementDeSens,
        Don,              // SevenHandler
        InversionHauteur, // PriestHandler
        JokerVerre,
        JokerNoir,
        JokerCouleur
    }
}