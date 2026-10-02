using Psycko.Core.Domain;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services.Turn
{
    /// <summary>
    /// Orchestrateur du tour : enchaîne les 7 étapes de l'Ordre Strict
    /// défini dans CLAUDE.md, sans aucune logique métier propre.
    /// Chaque étape est implémentée dans son propre Resolver (Step0 → Step6).
    ///
    /// Deux branches de sortie avant la fin naturelle :
    /// 1. Ramassage (forcé ou volontaire) : détecté par Step0, exécuté ailleurs
    ///    (GameOrchestrator), puis directement Step6 — court-circuite 1 à 5.
    /// 2. Don du 7 (RequiresGiftResolution) : détecté par Step3, ApplyPlay
    ///    s'arrête net et retourne. GameOrchestrator résout le Don via
    ///    IGameStateCommand puis appelle ResolveRemainder(play original) pour
    ///    reprendre à Step4.
    ///
    /// Aucun Resolver ne mute l'état : les intentions (pioche, ramassage FaceUp,
    /// transition de phase, contrainte/direction suivantes, destruction de pile,
    /// rejeu, Don) produites par Step2/Step3 sont portées par TurnResult et
    /// exécutées par GameOrchestrator via IGameStateCommand.
    /// </summary>
    public static class TurnManager
    {
        private static readonly CardPlayabilityChecker PlayabilityChecker = new CardPlayabilityChecker();

        public static PickupResolution BeginTurn(GameState state)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));

            var player = state.GetActivePlayer();
            if (player.CurrentPhase != DefPhase.Work && player.CurrentPhase != DefPhase.Talent)
                return PickupResolution.NoPickup(state);

            var forced = !PlayabilityChecker.HasAnyPlayableCard(player, state);
            return forced
                ? PickupResolution.Accepted(state, isForced: true)
                : PickupResolution.NoPickup(state);
        }

        public static PickupResolution ResolvePickup(GameState state, int playerId)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));

            if (state.GetSeatIndex(playerId) != state.ActivePlayerIndex)
                return PickupResolution.Rejected(state, PlayRejectionReason.NotYourTurn);

            var player = state.GetActivePlayer();
            if (player.CurrentPhase == DefPhase.Finished)
                return PickupResolution.Rejected(state, PlayRejectionReason.PlayerFinished);

            if (IsGameOver(state))
                return PickupResolution.Rejected(state, PlayRejectionReason.GameAlreadyOver);

            var beginTurn = BeginTurn(state);
            return PickupResolution.Accepted(state, isForced: beginTurn.IsPickup);
        }

        private static bool IsGameOver(GameState state)
        {
            var activePlayers = 0;
            for (var i = 0; i < state.Players.Count; i++)
                if (state.Players[i].CurrentPhase != DefPhase.Finished)
                    activePlayers++;
            return activePlayers <= 1;
        }

        /// <remarks>
        /// Exécute Step1 → Step2 → Step3. Si Step3 signale RequiresGiftResolution,
        /// s'arrête immédiatement après Step3 : Step4/Step5/Step6 ne sont ni exécutés
        /// ni devinés. GameOrchestrator doit alors résoudre le Don puis appeler
        /// ResolveRemainder(result, play) pour reprendre à Step4.
        /// </remarks>
        public static TurnResult ApplyPlay(GameState state, Play play)
        {
            var result = Step1_PlaceCardsResolver.Resolve(state, play);

            // Step2 porte les intentions de reconstruction (DrawCount, FaceUpPickup, TargetPhase).
            var reconstruction = Step2_HandReconstructionResolver.Resolve(result.State, play);
            result = reconstruction;

            // Step3 porte les intentions d'effets (contrainte, direction, destruction
            // de pile, rejeu, Don). Fusion explicite : on ne perd ni les intentions
            // Step2 (non concurrencées par Step3) ni les nouvelles intentions Step3.
            var step3 = Step3_CardEffectsResolver.Resolve(reconstruction.State, play);
            if (!step3.NextConstraint.HasValue
                || !step3.NextRefRank.HasValue
                || !step3.NextDirection.HasValue)
            {
                throw new System.InvalidOperationException(
                    "Invariant violé : Step3 doit toujours produire une contrainte, un rang de référence et une direction.");
            }
            result = result
                .WithState(step3.State)
                .WithNextConstraint(step3.NextConstraint!.Value, step3.NextRefRank.Value)
                .WithNextDirection(step3.NextDirection.Value)
                .WithDestroysPile(step3.DestroysPile)
                .WithGrantsReplay(step3.GrantsReplay)
                .WithRequiresGiftResolution(step3.RequiresGiftResolution);

            if (result.RequiresGiftResolution)
                return result;

            return ResolveRemainder(result, play);
        }

        /// <summary>
        /// Reprend l'enchaînement à Step4 (repioche finale) → Step5 (effets de pile,
        /// Doublon/Carré, dont le rejeu sur Carré) → Step6 (avancement de tour).
        /// Appelée directement par ApplyPlay quand aucun Don n'est requis, ou par
        /// GameOrchestrator après résolution du Don (main déjà mutée via IGameStateCommand ;
        /// le paramètre play reste le Play original de la pose ayant déclenché le tour,
        /// nécessaire à Step5 pour relire Pile.Cards dans son contexte).
        ///
        /// CONTRAT OBLIGATOIRE POUR GAMEORCHESTRATOR : lorsque cette méthode est appelée
        /// après résolution d'un Don, GameOrchestrator DOIT réinjecter l'état réellement
        /// muté (mains du donateur et du receveur mises à jour via IGameStateCommand)
        /// dans le TurnResult AVANT l'appel, c.-à-d. appeler
        /// ResolveRemainder(result.WithState(stateAprèsDon), play).
        /// Si cette réinjection est omise, Step4 travaillera sur l'état obsolète
        /// d'avant le Don (celui produit par Step3), et sa décision de repioche/
        /// transition de phase sera fausse.
        ///
        /// Fusionne explicitement SkipNext/Replay (OR logique) entre l'état entrant
        /// (issu de Step3, ex. GrantsReplay du Valet) et Step5 (ex. Replay du Carré) :
        /// aucune étape n'écrase silencieusement les drapeaux de l'autre.
        /// </summary>
        public static TurnResult ResolveRemainder(TurnResult previous, Play play)
        {
            var result = previous;

            var step4 = Step4_FinalDrawResolver.Resolve(result, play);
            result = result
                .WithState(step4.State)
                .WithFinalReconstruction(step4.FinalReconstruction ?? HandReconstructionResult.None);

            var step5 = Step5_PileEffectsResolver.Resolve(result.State, play, result);
            result = step5
                .WithState(step5.State)
                .WithSkipNext(result.SkipNext || step5.SkipNext)
                .WithReplay(result.Replay || step5.Replay);

            var advance = Step6_AdvanceTurnResolver.Resolve(result.State, result.SkipNext, result.Replay);
            return result
                .WithState(advance.State)
                .WithSkipNext(advance.SkipNext)
                .WithReplay(advance.Replay);
        }
    }
}