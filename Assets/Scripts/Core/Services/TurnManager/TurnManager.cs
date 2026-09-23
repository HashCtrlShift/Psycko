using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Orchestrateur du tour : enchaîne les 7 étapes de l'Ordre Strict
    /// défini dans CLAUDE.md, sans aucune logique métier propre.
    /// Chaque étape est implémentée dans son propre Resolver (Step0 → Step6).
    /// Le ramassage (forcé ou volontaire) est une branche exclusive qui court-circuite
    /// entièrement les étapes 1 à 5 : détecté par Step0, exécuté ailleurs (GameOrchestrator),
    /// puis directement Step6.
    /// Aucun Resolver ne mute l'état : les intentions (pioche, ramassage FaceUp,
    /// transition de phase) produites par Step2 sont portées par TurnResult et
    /// exécutées par GameOrchestrator via IGameStateCommand.
    /// </summary>
    public static class TurnManager
    {
        /// <param name="voluntaryPickup">
        /// True si le joueur a explicitement déclenché le ramassage (bouton "Ramasser"
        /// côté Présentation), transmis depuis GameOrchestrator.
        /// </param>
        public static TurnResult ApplyPlay(GameState state, Play play, bool voluntaryPickup)
        {
            var pickupDecision = Step0_PickupResolver.Resolve(state, play.PlayerId, voluntaryPickup);

            if (pickupDecision.IsPickup)
            {
                // Le ramassage réel (IGameStateCommand.PickUpPile) est exécuté par
                // GameOrchestrator — TurnManager ne mute jamais l'état lui-même.
                // On enchaîne directement sur Step6 avec l'état reçu.
                var pickupResult = Step6_AdvanceTurnResolver.Resolve(
                    pickupDecision.State, pickupDecision.SkipNext, pickupDecision.Replay);

                return pickupResult.WithIsPickup(true);
            }

            var result = Step1_PlaceCardsResolver.Resolve(state, play);

            // Step2 porte les intentions de reconstruction (DrawCount, FaceUpPickup, TargetPhase).
            var reconstruction = Step2_HandReconstructionResolver.Resolve(result.State, play);

            // Les étapes suivantes ne connaissent que State/SkipNext/Replay : on conserve
            // les intentions Step2 en ne remplaçant que l'état à chaque passage.
            result = reconstruction.WithState(Step3_CardEffectsResolver.Resolve(reconstruction.State, play).State);
            result = result.WithState(Step4_FinalDrawResolver.Resolve(result.State, play.PlayerId).State);
            result = result.WithState(Step5_PileEffectsResolver.Resolve(result.State, play).State);

            var advance = Step6_AdvanceTurnResolver.Resolve(result.State, result.SkipNext, result.Replay);
            return result
                .WithState(advance.State)
                .WithSkipNext(advance.SkipNext)
                .WithReplay(advance.Replay);
        }
    }
}