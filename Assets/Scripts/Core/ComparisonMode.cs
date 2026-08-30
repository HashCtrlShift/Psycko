namespace Psycko
{
    /// <summary>
    /// Mode de comparaison de hauteur actif pour valider une pose de carte.
    /// GreaterOrEqual = règle de base (jouer ≥ sommet de pile).
    /// LessOrEqual = activé temporairement par l'effet Prêtre (jouer ≤ sommet de pile), pour un seul tour.
    /// </summary>
    public enum ComparisonMode
    {
        GreaterOrEqual,
        LessOrEqual
    }
}