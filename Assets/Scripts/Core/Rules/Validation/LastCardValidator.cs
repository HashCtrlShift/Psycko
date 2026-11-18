using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Validation
{
    /// <summary>
    /// Valide la règle « un 2 ne peut pas terminer une phase » (CLAUDE.md § 2 Card).
    ///
    /// Un coup est un « 2 terminal » si les DEUX conditions sont vraies :
    /// 1. Le coup est un coup de 2 (EffectiveRank == Two). Un Joker a
    ///    EffectiveRank null : un Joker en dernière carte n'est jamais concerné.
    /// 2. Après ce coup, le joueur a terminé SA PHASE COURANTE :
    ///    - Work   : plus de main ET pioche vide.
    ///    - Talent : plus de main ET plus de Face Découverte.
    ///    - Luck   : plus de main, de Face Découverte ET de Face Cachée.
    ///
    /// Le verdict est calculé sur l'état AVANT le coup (le joueur dans l'état reçu
    /// n'a pas encore posé ses cartes). Il doit être demandé AVANT
    /// TurnManager.ApplyPlay, qui applique pioche, ramassage FaceUp et transition
    /// de phase.
    ///
    /// Conséquence d'un 2 terminal : le joueur pose son 2, ramasse toute la Pile
    /// (2 compris) et passe son tour. Ni destruction, ni rejeu, ni transition de
    /// phase ; la transition reprend à la fin de son prochain tour.
    ///
    /// Consommateur : GameOrchestrator.ApplyPlay.
    /// </summary>
    public static class LastCardValidator
    {
        public static bool TerminatesOnTwo(Play play, Player player, IGameStateQuery state)
        {
            return IsTwoPlay(play) && TerminatesPhaseAfterPlay(play, player, state);
        }

        /// <summary>
        /// Un coup est un coup de 2 si sa hauteur effective est Two.
        /// Play.Create garantit l'homogénéité : N cartes de même hauteur, ou UN Joker
        /// (EffectiveRank null). Aucun mélange possible.
        /// </summary>
        private static bool IsTwoPlay(Play play)
            => play.EffectiveRank == DefRank.Two;

        /// <summary>
        /// Vrai si, après le retrait des cartes du coup, la phase courante du joueur
        /// est épuisée. Lit l'état AVANT le coup : on soustrait les cartes jouées
        /// de la couche source.
        /// </summary>
        private static bool TerminatesPhaseAfterPlay(Play play, Player player, IGameStateQuery state)
        {
            int handAfter = player.Hand.Count - (play.SourceLayer == CardLayer.Hand ? play.Count : 0);
            int faceUpAfter = player.FaceUp.Count - (play.SourceLayer == CardLayer.FaceUp ? play.Count : 0);
            int faceDownAfter = player.FaceDown.Count - (play.SourceLayer == CardLayer.FaceDown ? play.Count : 0);

            switch (player.CurrentPhase)
            {
                case DefPhase.Work:
                    return handAfter <= 0 && state.DrawPile.Count == 0;

                case DefPhase.Talent:
                    return handAfter <= 0 && faceUpAfter <= 0;

                case DefPhase.Luck:
                    return handAfter <= 0 && faceUpAfter <= 0 && faceDownAfter <= 0;

                default: // Finished
                    return false;
            }
        }
    }
}