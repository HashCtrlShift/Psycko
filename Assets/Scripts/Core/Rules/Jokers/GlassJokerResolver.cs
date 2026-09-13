using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Jokers
{
    /// <summary>
    /// Joker de Verre — TRANSPARENCE PURE.
    /// Ne possède aucune hauteur propre et n'altère jamais la contrainte en cours :
    /// il laisse passer tel quel le couple (mode, rang de référence) de la première
    /// carte non-Verre située en dessous de lui.
    /// Exemple : Prêtre puis Joker de Verre → le joueur suivant se voit appliquer
    /// (PriestReversed, Priest), exactement comme si le Verre n'existait pas.
    /// Plusieurs Verre empilés sont traversés d'un seul tenant.
    /// Aucun effet propre : jamais de rejeu, jamais de destruction de pile.
    /// </summary>
    public static class GlassJokerResolver
    {
        /// <summary>
        /// Calcule la contrainte transmise au joueur suivant après la pose d'un
        /// Joker de Verre, en remontant la pile jusqu'au premier coup non-Verre.
        /// La contrainte courante de l'état est restituée inchangée dès qu'un coup
        /// porteur de hauteur est trouvé.
        /// Si aucun coup non-Verre n'existe (pile vide, ou composée uniquement de
        /// Jokers), retourne la contrainte neutre : le joueur suivant joue ce qu'il
        /// veut (voir ResolvesToNoConstraint).
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return HasHeightReference(state.Pile)
                ? (state.Constraint, state.RefRank)
                : (HeightConstraint.Normal, DefRank.Three);
        }

        /// <summary>
        /// True si la pose du Joker de Verre ne transmet aucune contrainte de hauteur
        /// (aucun coup porteur de hauteur en dessous). Le joueur suivant est alors
        /// libre, comme après un Joker Noir.
        /// </summary>
        public static bool ResolvesToNoConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return !HasHeightReference(state.Pile);
        }

        /// <summary>
        /// Remonte les coups du plus récent au plus ancien et retourne true dès qu'un
        /// coup porteur d'une hauteur (EffectiveRank non null) est rencontré.
        /// Les coups Joker (Verre, Noir, Couleur) sont sautés : aucun ne porte de hauteur.
        /// </summary>
        private static bool HasHeightReference(Pile pile)
        {
            if (pile is null || pile.IsEmpty)
                return false;

            for (int i = pile.Plays.Count - 1; i >= 0; i--)
            {
                if (pile.Plays[i].EffectiveRank.HasValue)
                    return true;
            }

            return false;
        }
    }
}