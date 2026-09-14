using System;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.SpecialCards
{
    /// <summary>
    /// Carte 7 — LE DON.
    /// Le 7 est une hauteur BASSE et ordinaire (3 < 4 < 5 < 6 < 7 < 8 < …) :
    /// il ne modifie ni le sens de jeu, ni la pile, et n'accorde aucun rejeu.
    /// Son effet propre est le DON : le poseur donne UNE carte de sa main à un
    /// adversaire de son choix. Le 7 est donc un avantage double pour qui le pose
    /// — il se défausse d'une carte ET handicape un adversaire.
    /// Premier handler du projet dont l'effet touche les MAINS des joueurs, et non
    /// la contrainte / la pile / le sens. Conformément au contrat de la couche
    /// Rules, ce handler DÉCLARE qu'un Don est dû ; il ne le RÉALISE jamais.
    /// Il ne désigne ni le destinataire, ni la carte donnée : ces deux choix
    /// appartiennent au joueur et remontent via Interfaces jusqu'à la Présentation.
    /// Le transfert de carte lui-même est une transition d'état, à la charge de
    /// Services/ — aucune commande de don n'existe à ce jour dans IGameStateCommand.
    ///
    /// MOMENT D'APPEL — CRITIQUE.
    /// IsGiftTriggered DOIT être interrogé APRÈS la reconstitution complète de la
    /// main du poseur (pioche si main < 3 et pioche non épuisée, passage de phase
    /// et ramassage des Face Découverte le cas échéant), et AVANT l'évaluation
    /// Doublon/Carré. Séquence complète, à la charge de Rules/Phase :
    ///   1. pose du (ou des) 7
    ///   2. reconstitution de la main (DrawCards / AdvancePlayerPhase + ramassage FaceUp)
    ///   3. Don          ← appel de ce handler
    ///   4. re-pioche si main < 3 et pioche non épuisée
    ///   5. évaluation Doublon/Carré en relisant Pile.Cards
    /// Interroger ce handler à l'étape 1 produirait des Dons silencieux à tort :
    /// un joueur posant son dernier 7 doit donner la carte qu'il vient de piocher,
    /// et un joueur passant en Phase 2 doit donner une Face Découverte ramassée,
    /// celles-ci devenant sa main.
    /// </summary>
    public static class SevenHandler
    {
        /// <summary>
        /// Contrainte transmise après la pose d'un 7 : hauteur de référence 7,
        /// en mode Normal. Le 7 n'inverse pas la comparaison (seul le Prêtre le fait)
        /// et n'écrase aucune contrainte particulière.
        /// </summary>
        public static (HeightConstraint Mode, DefRank RefRank) ResolveConstraint(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return (HeightConstraint.Normal, DefRank.Seven);
        }

        /// <summary>
        /// Le 7 ne détruit pas la pile. Seuls le 2, le Carré et le Joker Couleur/Bombe
        /// le font — le Don est un transfert de main, sans effet sur la pile.
        /// </summary>
        public static bool DestroysPile => false;

        /// <summary>
        /// Le 7 n'accorde aucun rejeu. Le Don consomme l'effet du coup,
        /// puis le tour progresse normalement (sous réserve d'un Doublon/Carré,
        /// évalué séparément et après le Don).
        /// </summary>
        public static bool GrantsReplay => false;

        /// <summary>
        /// Indique si un Don est dû après ce coup.
        /// Le Don est OBLIGATOIRE dès qu'il est possible : le joueur choisit quelle
        /// carte il donne et à qui, mais jamais s'il donne. Le bouton de la couche
        /// Présentation sert à CONFIRMER la carte choisie, non à refuser le Don.
        ///
        /// Le Don est dû si et seulement si la main du poseur est non vide au moment
        /// de la résolution. La PHASE n'est volontairement PAS un critère : elle
        /// détermine seulement si la main peut se reconstituer (pioche, ramassage des
        /// Face Découverte en Phase 2), reconstitution qui a déjà eu lieu lorsque ce
        /// handler est interrogé. Une fois celle-ci résolue, « main vide ou non »
        /// couvre à elle seule tous les cas, sans branche par phase.
        ///
        /// Exception verrouillée (CLAUDE.md) — 7 révélé depuis la Face Cachée en
        /// Phase 3 : aucun Don (effet silencieux), le joueur n'ayant pas de main
        /// constituée pour ce 7. Play ne portant pas sa couche d'origine, le drapeau
        /// isFromFaceDown est fourni par Rules/Phase, seule couche à savoir d'où sort
        /// la carte. Lorsque Play portera un jour un SourceLayer, ce paramètre pourra
        /// être déduit ici et retiré de la signature.
        /// Un 7 joué DEPUIS LA MAIN en Phase 3 — main > 0 cartes
        ///  conserve son effet de Don : c'est bien la couche
        /// d'origine qui compte, et non la phase. — main =0 cartes => pas de don.
        /// Détermine si l'effet de Don doit être déclenché pour ce Play de 7.
        /// Exceptions verrouillées CLAUDE.md :
        /// - Aucun Don si le(s) 7 proviennent de la Couche 3 (Face Cachée, Phase 3) :
        ///   le joueur n'a pas de main constituée pour ce 7 (effet silencieux).
        /// - Aucun Don si, après jeu du 7 (et reconstitution éventuelle : pioche puis
        ///   ramassage des Face Découverte en transition Phase 1→2), la main du
        ///   joueur reste vide : aucune carte disponible à donner.
        /// </summary>
        public static bool IsGiftTriggered(IGameStateQuery state, Play play)
        {
            if (state is null)
                throw new ArgumentNullException(nameof(state));
            if (play is null)
                throw new ArgumentNullException(nameof(play));

            // Exception 1 : 7 révélé depuis Face Cachée → aucun Don
            if (play.SourceLayer == CardLayer.FaceDown)
                return false;

            // Exception 2 : Main vide → aucune carte à donner
            if (state.Players[play.PlayerId].Hand.Count == 0)
                return false;

            // Sinon, Don obligatoire
            return true;
        }

        /// <summary>
        /// Nombre de cartes données : TOUJOURS une seule, quel que soit le nombre de
        /// 7 contenus dans le coup. « On applique une seule fois l'effet d'un groupe
        /// de cartes » : c'est le Play qui compte, pas la carte.
        /// À ne pas confondre avec le Doublon/Carré, qui lui se déduit de Pile.Cards
        /// APRÈS que le Don a été effectué. Un Carré de 7 donne donc lieu à un Don
        /// unique, PUIS à la destruction de la pile.
        /// </summary>
        public static int ResolveGiftCardCount(Play play)
        {
            if (play is null)
                throw new System.ArgumentNullException(nameof(play));

            return 1;
        }

        /// <summary>Le 7 n'altère pas le sens de jeu.</summary>
        public static PlayDirection ResolveDirection(IGameStateQuery state)
        {
            if (state is null)
                throw new System.ArgumentNullException(nameof(state));

            return state.Direction;
        }
    }
}