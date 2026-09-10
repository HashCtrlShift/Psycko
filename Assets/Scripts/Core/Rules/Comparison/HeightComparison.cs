using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Comparison
{
    /// <summary>
    /// Compare la hauteur de deux DefRank selon l'ordre croissant :
    /// 3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < Prêtre < Valet < Cavalier < Dame < Roi < As < 2.
    /// Ne gère aucune notion de Joker (transparence Verre, reset Noir) —
    /// portée limitée à la comparaison brute de deux rangs standards,
    /// géré par les couches Rules/Jokers et Rules/Validation ultérieurement.
    /// </summary>
    public static class HeightComparison
    {
        /// <summary>
        /// Retourne true si candidate >= reference (ordre croissant).
        /// </summary>
        public static bool IsGreaterOrEqual(DefRank candidate, DefRank reference)
        {
            return (int)candidate >= (int)reference;
        }

        /// <summary>
        /// Retourne true si candidate <= reference (ordre croissant).
        /// </summary>
        public static bool IsLessOrEqual(DefRank candidate, DefRank reference)
        {
            return (int)candidate <= (int)reference;
        }

        /// <summary>
        /// Compare deux rangs de manière standard (a.CompareTo(b)).
        /// Retourne : < 0 si a < b, 0 si a == b, > 0 si a > b.
        /// </summary>
        public static int Compare(DefRank a, DefRank b)
        {
            return ((int)a).CompareTo((int)b);
        }
    }

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