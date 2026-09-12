namespace Psycko.Core.Domain
{
    /// <summary>
    /// Contrainte de hauteur active sur la Pile pour le joueur qui va jouer.
    /// Normal = doit jouer >= RefRank (mode standard).
    /// PriestReversed = doit jouer <= RefRank (RefRank = le Prêtre qui a posé la contrainte).
    /// </summary>
    public enum HeightConstraint
    {
        Normal = 0,
        PriestReversed = 1
    }
}