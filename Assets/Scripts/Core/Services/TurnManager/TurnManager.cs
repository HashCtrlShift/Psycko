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
    /// </summary>
    public static class TurnManager
    {
        /// <param name="voluntaryPickup">
        /// True si le joueur a explicitement déclenché le ramassage (bouton "Ramasser"
        /// côté Présentation), transmis depuis GameOrchestrator.
        /// </param>
        public static GameState ApplyPlay(GameState state, Play play, bool voluntaryPickup)
        {
            var pickupDecision = Step0_PickupResolver.Resolve(state, play.PlayerId, voluntaryPickup);

            if (pickupDecision.IsPickup)
            {
                // Le ramassage réel (IGameStateCommand.PickUpPile) est exécuté par
                // GameOrchestrator avant/après cet appel — TurnManager ne mute jamais
                // l'état lui-même. On enchaîne directement sur Step6 avec l'état reçu.
                var pickupResult = Step6_AdvanceTurnResolver.Resolve(
                    pickupDecision.State, pickupDecision.SkipNext, pickupDecision.Replay);

                return pickupResult.State;
            }

            var result = Step1_PlaceCardsResolver.Resolve(state, play);
            result = Step2_ReconstructionResolver.Resolve(result.State, play.PlayerId);
            result = Step3_CardEffectsResolver.Resolve(result.State, play);
            result = Step4_FinalDrawResolver.Resolve(result.State, play.PlayerId);
            result = Step5_PileEffectsResolver.Resolve(result.State, play);
            result = Step6_AdvanceTurnResolver.Resolve(result.State, result.SkipNext, result.Replay);

            return result.State;
        }
    }
}