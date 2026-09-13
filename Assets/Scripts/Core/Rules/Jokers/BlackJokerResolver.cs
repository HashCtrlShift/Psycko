using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Jokers
{
    /// <summary>
    /// Joker Noir / Passe — L'EXCUSE.
    /// Jouable sur n'importe quelle hauteur, dans les deux modes (Normal comme
    /// PriestReversed) : il n'est jamais bloqué par la contrainte en cours.
    /// Une fois posé, il RÉINITIALISE la contrainte : le joueur suivant joue ce
    /// qu'il veut, sans borne haute ni basse.
    /// Ne détruit pas la pile : les cartes restent en place (elles seront ramassées
    /// si un joueur doit prendre la pile).
    /// Conséquence indirecte, NON exposée ici : aucune chaîne Doublon/Carré ne le
    /// traverse. Les détections de T4 liront Pile.Plays et y verront leur borne —
    /// aucun contrat n'est figé prématurément depuis cette classe.
    /// </summary>
    public static class BlackJokerResolver
    {
        /// <summary>
        /// Toujours true : le Joker Noir est jouable quelle que soit la contrainte
        /// active, y compris sur une pile vide.
        /// </summary>
        public static bool IsAlwaysPlayable(IGameStateQuery state) => true;

        /// <summary>
        /// Contrainte transmise au joueur suivant : toujours neutre.
        /// RefRank retourne la valeur neutre DefRank.Three, non pertinente et jamais
        /// consultée — l'absence de contrainte se lit via ResetsConstraint.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.Normal, DefRank.Three);
        }

        /// <summary>
        /// Toujours true : la pose d'un Joker Noir efface la contrainte de hauteur
        /// en cours, quel que soit l'état précédent (y compris pile vide).
        /// </summary>
        public static bool ResetsConstraint(IGameStateQuery state) => true;
    }
}