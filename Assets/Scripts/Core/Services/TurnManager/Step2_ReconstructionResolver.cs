using System;
using Psycko.Core.Domain;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 2 — Reconstruction de la main. Couche mince : gardes d'orchestration,
    /// puis délégation intégrale à HandReconstructionPolicy. Comportement observable
    /// identique à la version précédente (T16).
    /// </summary>
    public static class Step2_HandReconstructionResolver
    {
        public static TurnResult Resolve(GameState state, Play play)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            var active = state.GetActivePlayer();
            if (play.PlayerId != active.Id)
                throw new InvalidOperationException(
                    $"Seul le joueur actif (ID={active.Id}) peut déclencher la reconstruction (reçu ID={play.PlayerId}).");
            if (play.SourceLayer != CardLayer.Hand)
                throw new ArgumentException(
                    $"Step2 ne s'applique qu'après une pose depuis Hand (reçu {play.SourceLayer}).", nameof(play));

            var r = HandReconstructionPolicy.Reconstruct(state, state.ActivePlayerIndex, play.Count);

            return new TurnResult(
                state: state,
                skipNext: false,
                replay: false,
                isPickup: false,
                drawCount: r.DrawCount,
                triggersFaceUpPickup: r.TriggersFaceUpPickup,
                triggersPhaseTransition: r.TriggersPhaseTransition,
                targetPhase: r.TargetPhase);
        }
    }
}