using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Jokers
{
    /// <summary>
    /// Joker Couleur / Bombe — DESTRUCTION DE PILE.
    /// Détruit inconditionnellement la pile de jeu, y compris lorsqu'il est posé
    /// sur une pile vide (il est alors lui-même la seule carte détruite) : aucun
    /// cas dégénéré, aucun no-op.
    /// ⚠️ DIVERGENCE ASSUMÉE AVEC LE CARRÉ : après un Carré, c'est le POSEUR qui
    /// rejoue et ouvre la nouvelle pile. Après une Bombe, c'est le JOUEUR SUIVANT.
    /// Cette asymétrie est volontaire — ne pas « corriger »
    /// par alignement sur QuadDetection.
    /// La nouvelle pile étant vide, aucune contrainte de hauteur ne subsiste.
    /// </summary>
    public static class ColorJokerResolver
    {
        /// <summary>
        /// Toujours true : la Bombe est jouable quelle que soit la contrainte active,
        /// y compris sur une pile vide.
        /// </summary>
        public static bool IsAlwaysPlayable => true;

        /// <summary>
        /// Toujours true : la pile est intégralement détruite après la pose.
        /// L'appelant (Services/) applique la transition via Pile.Cleared().
        /// </summary>
        public static bool DestroysPile => true;

        /// <summary>
        /// Toujours false : contrairement au Carré, le poseur de la Bombe NE rejoue PAS.
        /// C'est le joueur suivant qui ouvre la nouvelle pile.
        /// </summary>
        public static bool GrantsReplay => false;

        /// <summary>
        /// Contrainte transmise au joueur suivant : toujours neutre, la nouvelle pile
        /// étant vide. RefRank retourne la valeur neutre DefRank.Three, non pertinente.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.Normal, DefRank.Three);
        }
    }
}