using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.SpecialCards
{
    /// <summary>
    /// Carte 2 — DESTRUCTION DE LA PILE ET REJEU DU POSEUR.
    /// Le 2 est la hauteur la PLUS ÉLEVÉE du jeu (… < Roi < As < 2).
    /// Une fois posé, la pile est détruite et le MÊME joueur rejoue sur une pile vide.
    /// Rappel des précédents, tous distincts : le Carré fait rejouer le poseur,
    /// le Joker Couleur/Bombe fait rejouer le joueur SUIVANT, le 2 fait rejouer le poseur.
    /// Ce handler NE DÉCIDE PAS de la jouabilité : le fait que le 2 soit injouable
    /// sous contrainte Prêtre, et interdit pour terminer une phase, relève de
    /// Rules/Validation (CardPlayability, LastCardValidator) — pas d'ici.
    /// Cas limite « dernière carte » : le joueur pose son 2 (visible de tous),
    /// puis ramasse TOUTE la pile, 2 compris, et passe son tour. Ni destruction,
    /// ni rejeu. Cette exception est arbitrée hors de ce handler.
    /// Doublon/Carré de 2 : structurellement inatteignable, la pile étant détruite
    /// dès le premier coup de 2 — aucune garde défensive n'est écrite ici.
    /// </summary>
    public static class TwoHandler
    {
        /// <summary>
        /// Contrainte transmise après la pose d'un 2.
        /// La pile étant détruite, il n'existe plus aucune contrainte de hauteur :
        /// on retourne la contrainte neutre (Normal, Three), qui est la convention
        /// du projet pour « pile vide » — « aucune contrainte » ne s'exprime
        /// JAMAIS par null, elle se signale par une pile vide.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.Normal, DefRank.Three);
        }

        /// <summary>
        /// Le 2 détruit toujours la pile en cours.
        /// L'application concrète passe par Pile.Cleared().
        /// </summary>
        public static bool DestroysPile => true;

        /// <summary>
        /// Le 2 fait rejouer le joueur qui vient de le poser, sur une pile vide.
        /// </summary>
        public static bool GrantsReplay => true;

        /// <summary>
        /// Identifiant du joueur qui rejoue après la destruction : le poseur lui-même.
        /// Se distingue du Joker Couleur/Bombe, qui fait rejouer le joueur suivant.
        /// </summary>
        public static int ResolveReplayingPlayer(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return state.ActivePlayerIndex;
        }

        /// <summary>Le 2 n'altère pas le sens de jeu.</summary>
        public static PlayDirection ResolveDirection(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return state.Direction;
        }
    }
}