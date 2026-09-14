using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.SpecialCards
{
    /// <summary>
    /// Prêtre — INVERSION TEMPORAIRE DE LA RÈGLE DE HAUTEUR.
    /// Pose la contrainte (PriestReversed, Priest) : le joueur suivant doit
    /// jouer une hauteur INFÉRIEURE OU ÉGALE au Prêtre.
    /// Position hiérarchique : 10 < Prêtre < Valet — sous contrainte, un 10
    /// est jouable, un Valet ne l'est pas.
    /// Ce handler DÉCLARE l'effet uniquement. La DURÉE de vie de la contrainte
    /// (un seul tour, relance par un nouveau Prêtre, persistance à travers les
    /// skips de Doublon, réinitialisation par Carré/Bombe/Joker Noir) est
    /// appliquée par la couche Phase — pas ici.
    /// Cohérence Joker de Verre : le Verre étant en transparence pure, il
    /// restitue (state.Constraint, state.RefRank) tels quels. La contrainte
    /// produite ici traverse donc un Verre sans altération.
    /// </summary>
    public static class PriestHandler
    {
        /// <summary>
        /// Contrainte transmise au joueur suivant après la pose d'un Prêtre :
        /// (PriestReversed, Priest).
        /// L'état courant n'est pas consulté : un Prêtre posé écrase toujours
        /// la contrainte précédente et redevient la référence.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.PriestReversed, DefRank.Priest);
        }

        /// <summary>
        /// True si la contrainte passée en paramètre est une contrainte Prêtre active.
        /// Lecture pure, sans effet de bord — destiné aux couches Validation/Phase.
        /// </summary>
        public static bool IsPriestConstraint(HeightConstraint mode)
            => mode == HeightConstraint.PriestReversed;

        /// <summary>Le Prêtre ne détruit jamais la pile.</summary>
        public static bool DestroysPile => false;

        /// <summary>
        /// Le Prêtre n'accorde pas de rejeu par lui-même.
        /// Le skip du joueur suivant en cas de Doublon de Prêtres relève de
        /// PairDetection et de la couche Phase, pas de ce handler.
        /// </summary>
        public static bool GrantsReplay => false;

        /// <summary>Le Prêtre n'altère pas le sens de jeu.</summary>
        public static PlayDirection ResolveDirection(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return state.Direction;
        }
    }
}