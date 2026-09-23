using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 2 — Reconstruction de la main (HAND RECONSTRUCTION).
    /// Appelée après validation de la pose par Step1. DÉCIDE sans jamais muter :
    ///   1. combien de cartes piocher (sous-temps 1, phase Work uniquement) ;
    ///   2. si la transition Work→Talent se déclenche : main vide ET pioche épuisée
    ///      (le joueur doit vider sa main avant de pouvoir ramasser ses FaceUp) ;
    ///   3. si la transition Talent→Luck se déclenche : main vide (aucune pioche en Talent).
    ///
    /// La garde composée « main vide ET pioche épuisée » est concentrée ICI, volontairement.
    /// WorkPhaseResolver.ShouldTransitionToNextPhase(Player) n'est pas invoqué : il ne
    /// reçoit qu'un Player (pas d'accès à la pioche) et l'état n'étant pas encore muté
    /// à ce stade, player.Hand.Count reflète la main AVANT la pose. Step2 re-dérive donc
    /// le même prédicat (main == 0) sur les compteurs projetés, augmenté de la garde pioche.
    ///
    /// Phase Luck / Finished : hors périmètre → exception explicite.
    /// La repioche éventuelle liée aux effets de carte (ex. 7) relève de Step3/Step4.
    /// </summary>
    public static class Step2_HandReconstructionResolver
    {
        private const int HandTarget = 3;

        public static TurnResult Resolve(GameState state, Play play)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            var activePlayer = state.GetActivePlayer();
            if (play.PlayerId != activePlayer.Id)
                throw new InvalidOperationException(
                    $"Seul le joueur actif (ID={activePlayer.Id}) peut déclencher la reconstruction. " +
                    $"Le coup proposé vient du joueur ID={play.PlayerId}.");

            if (play.SourceLayer != CardLayer.Hand)
                throw new ArgumentException(
                    $"Step2 ne reconstruit la main qu'après une pose depuis Hand. " +
                    $"SourceLayer reçu : {play.SourceLayer}.",
                    nameof(play));

            int pileRemaining = state.DrawPile.Count;
            if (pileRemaining < 0)
                throw new InvalidOperationException(
                    $"Invariant d'état violé : GameState.DrawPile.Count négatif ({pileRemaining}).");

            switch (activePlayer.CurrentPhase)
            {
                case DefPhase.Work:
                    return BuildWorkResult(state, activePlayer, play, pileRemaining);

                case DefPhase.Talent:
                    if (activePlayer.FaceUp.Count != 0)
                        throw new InvalidOperationException(
                            $"Invariant violé : en phase Talent, FaceUp doit être vide " +
                            $"(ramassées lors de la transition Work→Talent). " +
                            $"FaceUp.Count={activePlayer.FaceUp.Count} pour le joueur ID={activePlayer.Id}.");
                    return BuildTalentResult(state, activePlayer, play);

                default:
                    throw new InvalidOperationException(
                        $"Step2 est hors périmètre pour la phase {activePlayer.CurrentPhase}.");
            }
        }

        // ---------------------------------------------------------------- Work

        /// <summary>
        /// Sous-temps 1 : pioche AVANT toute transition, pioche encore intacte.
        ///   drawBefore    = clamp(HandTarget - handAfterPlay, 0, pileRemaining)
        ///   handAfterDraw = handAfterPlay + drawBefore
        ///   pileAfterDraw = pileRemaining - drawBefore
        /// Transition Work→Talent ssi handAfterDraw == 0 ET pileAfterDraw == 0.
        /// Sous-temps 2 (ramassage FaceUp) : intention levée uniquement si transition.
        /// Sous-temps 3 : aucune pioche possible après (pile vide par construction).
        /// </summary>
        private static TurnResult BuildWorkResult(GameState state, Player player, Play play, int pileRemaining)
        {
            int handAfterPlay = HandCountAfterPlay(player, play);
            int drawBefore = DrawCountFor(handAfterPlay, pileRemaining);
            int handAfterDraw = handAfterPlay + drawBefore;
            int pileAfterDraw = pileRemaining - drawBefore;

            bool transition = handAfterDraw == 0 && pileAfterDraw == 0;

            return new TurnResult(
                state: state,
                skipNext: false,
                replay: false,
                isPickup: false,
                drawCount: drawBefore,
                triggersFaceUpPickup: transition,
                triggersPhaseTransition: transition,
                targetPhase: transition ? DefPhase.Talent : (DefPhase?)null);
        }

        // -------------------------------------------------------------- Talent

        /// <summary>Phase Talent : aucune pioche. Transition Talent→Luck ssi main vide après pose.</summary>
        private static TurnResult BuildTalentResult(GameState state, Player player, Play play)
        {
            int handAfterPlay = HandCountAfterPlay(player, play);
            bool transition = handAfterPlay == 0;

            return new TurnResult(
                state: state,
                skipNext: false,
                replay: false,
                isPickup: false,
                drawCount: 0,
                triggersFaceUpPickup: false,
                triggersPhaseTransition: transition,
                targetPhase: transition ? DefPhase.Luck : (DefPhase?)null);
        }

        // ------------------------------------------------------------- Helpers

        /// <summary>Main restante après pose. Exception explicite si incohérent (Step1 aurait dû filtrer).</summary>
        private static int HandCountAfterPlay(Player player, Play play)
        {
            int before = player.Hand.Count;
            int after = before - play.Count;

            if (after < 0)
                throw new InvalidOperationException(
                    $"La pose référence plus de cartes ({play.Count}) que la main n'en contient " +
                    $"({before}) pour le joueur ID={player.Id}. Step1 aurait dû rejeter ce cas.");

            return after;
        }

        /// <summary>Cartes à piocher (sous-temps 1), bornées par le reliquat de pioche commune.</summary>
        private static int DrawCountFor(int handAfterPlay, int pileRemaining)
        {
            int needed = HandTarget - handAfterPlay;
            if (needed <= 0) return 0;
            return needed < pileRemaining ? needed : pileRemaining;
        }
    }
}