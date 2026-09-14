using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.SpecialCards
{
    /// <summary>
    /// Valet — INVERSION DU SENS DE JEU.
    /// Se comporte comme une carte standard du point de vue de la hauteur :
    /// il pose une contrainte Normal avec sa propre hauteur en référence
    /// (le joueur suivant doit jouer Valet ou plus).
    /// Son unique effet propre est de renverser le sens de jeu.
    /// Aucune destruction de pile, aucun rejeu.
    /// Ce handler DÉCLARE l'effet : il ne l'applique pas (voir Rules/Phase et TurnManager).
    /// </summary>
    public static class JackHandler
    {
        /// <summary>
        /// Contrainte transmise au joueur suivant après la pose d'un Valet :
        /// (Normal, Jack). Identique à n'importe quelle carte standard.
        /// L'état courant n'est pas consulté — un Valet écrase toujours la
        /// contrainte précédente, y compris une contrainte Prêtre.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.Normal, DefRank.Jack);
        }

        /// <summary>
        /// Sens de jeu après la pose d'un Valet : l'inverse du sens courant.
        /// Un Doublon/Carré de Valets est UN SEUL coup (Play) : l'inversion
        /// ne s'applique qu'une fois, conformément à la règle
        /// « on applique une seule fois l'effet d'un groupe de cartes ».
        /// </summary>
        public static PlayDirection ResolveDirection(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return Reverse(state.Direction);
        }

        /// <summary>Renverse un sens de jeu.</summary>
        public static PlayDirection Reverse(PlayDirection direction)
            => direction == PlayDirection.Clockwise
                ? PlayDirection.CounterClockwise
                : PlayDirection.Clockwise;

        /// <summary>Le Valet ne détruit jamais la pile.</summary>
        public static bool DestroysPile => false;

        /// <summary>Le Valet n'accorde jamais de rejeu.</summary>
        public static bool GrantsReplay => false;
    }
}