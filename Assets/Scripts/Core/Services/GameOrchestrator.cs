using System;
using System.Linq;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.TurnManager;
using TurnManagerService = Psycko.Core.Services.TurnManager.TurnManager;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Source unique d'exécution des intentions, notamment Step6 : le resolver
    /// calcule, cet orchestrateur appelle SetActivePlayer au plus une fois.
    /// </summary>
    public sealed class GameOrchestrator
    {
        public PlayResult ApplyPlay(
            GameState state,
            Play play,
            int playerIndex,
            bool voluntaryPickupRequested)
        {
            // --- Erreurs de programmation → exceptions ---
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (play == null) throw new ArgumentNullException(nameof(play));
            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            // --- Rejets métier → résultat typé ---
            if (IsGameOver(state))
                return PlayResult.Rejected(state, PlayRejectionReason.GameAlreadyOver);

            if (playerIndex != state.ActivePlayerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            if (state.Players[playerIndex].CurrentPhase == DefPhase.Finished)
                return PlayResult.Rejected(state, PlayRejectionReason.PlayerFinished);

            if (state.GetSeatIndex(play.PlayerId) != playerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            var beginTurn = TurnManagerService.BeginTurn(state);
            var forcedPickup = beginTurn.IsPickup;
            var pickupRequested = forcedPickup || voluntaryPickupRequested;

            if (pickupRequested)
            {
                var pickup = TurnManagerService.ResolvePickup(state, play.PlayerId);
                if (!pickup.IsAccepted)
                    return PlayResult.Rejected(state, pickup.RejectionReason!.Value);

                var seatIndex = state.GetSeatIndex(play.PlayerId);
                var afterPickup = (GameState)state.PickUpPile(seatIndex);
                afterPickup = (GameState)afterPickup.SetConstraint(
                    HeightConstraint.Normal,
                    DefRank.Three);

                var advance = Step6_AdvanceTurnResolver.Resolve(
                    afterPickup,
                    skipNext: false,
                    replay: false);

                afterPickup = ExecuteActivePlayerIntent(afterPickup, advance);

                IReadOnlyList<int> forcedIds = forcedPickup
                    ? new List<int> { play.PlayerId }
                    : new List<int>();

                return PlayResult.Accepted(
                    afterPickup,
                    IsGameOver(afterPickup),
                    forcedIds);
            }

            var turnResult = TurnManagerService.ApplyPlay(state, play);
            var newState = turnResult.State;

            // Ordre respecté : pioche Step2 (avant Don), effets Step3 (contrainte,
            // direction, destruction de pile), pioche Step4 (après Don éventuel).
            // Si RequiresGiftResolution est vrai, TurnManager s'est arrêté après Step3.
            // La résolution du Don est explicite via ResolveGiftAndContinue : elle exécute
            // TransferCard puis réinjecte l'état avant ResolveRemainder.

            if (turnResult.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    state.ActivePlayerIndex,
                    turnResult.DrawCount);
            }

            if (turnResult.DestroysPile)
            {
                newState = (GameState)newState.DestroyPile();
            }

            if (turnResult.NextConstraint.HasValue
                && turnResult.NextRefRank.HasValue)
            {
                newState = (GameState)newState.SetConstraint(
                    turnResult.NextConstraint.Value,
                    turnResult.NextRefRank.Value);
            }

            if (turnResult.NextDirection.HasValue)
            {
                newState = (GameState)newState.WithDirection(
                    turnResult.NextDirection.Value);
            }

            if (turnResult.FinalReconstruction.HasValue
                && turnResult.FinalReconstruction.Value.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    state.ActivePlayerIndex,
                    turnResult.FinalReconstruction.Value.DrawCount);
            }

            if (turnResult.TriggersPhaseTransition)
            {
                newState = (GameState)newState.AdvancePlayerPhase(
                    state.ActivePlayerIndex);
            }

            // GameOrchestrator est la source unique d'exécution de l'intention Step6.
            newState = ExecuteActivePlayerIntent(newState, turnResult);

            return PlayResult.Accepted(
                newState,
                IsGameOver(newState));
        }

        /// <summary>
        /// Résout le Don obligatoire puis reprend exactement à Step4.
        /// Cette réinjection structurelle corrige T18-bis : TransferCard est exécuté
        /// avant ResolveRemainder, de sorte que Step4_FinalDrawResolver ne lit jamais
        /// l'état obsolète produit par Step3. Le cas « RequiresGiftResolution vrai
        /// mais main déjà vide » ne peut donc plus produire un état incohérent : la
        /// main est réellement mutée avant la repioche finale.
        /// </summary>
        public PlayResult ResolveGiftAndContinue(
            GameState state,
            Play play,
            TurnResult pendingResult,
            GiftResolutionChoice choice)
        {
            if (!pendingResult.RequiresGiftResolution)
                throw new InvalidOperationException(
                    "ResolveGiftAndContinue requiert un TurnResult en attente de Don.");

            var donorIndex = pendingResult.State.ActivePlayerIndex;
            var donor = pendingResult.State.Players[donorIndex];

            if (!donor.Hand.Contains(choice.CardToGive))
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.InvalidCards);

            var transferred = (GameState)((IGameStateCommand)pendingResult.State)
                .TransferCard(
                    donorIndex,
                    choice.RecipientSeatIndex,
                    choice.CardToGive);

            var remainder = TurnManagerService.ResolveRemainder(
                pendingResult.WithState(transferred),
                play);

            var newState = remainder.State;

            if (remainder.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    pendingResult.State.ActivePlayerIndex,
                    remainder.DrawCount);
            }

            if (remainder.DestroysPile)
            {
                newState = (GameState)newState.DestroyPile();
            }

            if (remainder.NextConstraint.HasValue
                && remainder.NextRefRank.HasValue)
            {
                newState = (GameState)newState.SetConstraint(
                    remainder.NextConstraint.Value,
                    remainder.NextRefRank.Value);
            }

            if (remainder.NextDirection.HasValue)
            {
                newState = (GameState)newState.WithDirection(
                    remainder.NextDirection.Value);
            }

            if (remainder.FinalReconstruction.HasValue
                && remainder.FinalReconstruction.Value.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    pendingResult.State.ActivePlayerIndex,
                    remainder.FinalReconstruction.Value.DrawCount);
            }

            if (remainder.TriggersPhaseTransition)
            {
                newState = (GameState)newState.AdvancePlayerPhase(
                    pendingResult.State.ActivePlayerIndex);
            }

            newState = ExecuteActivePlayerIntent(newState, remainder);

            return PlayResult.Accepted(
                newState,
                IsGameOver(newState));
        }

        /// <summary>
        /// Révèle et résout une carte de la couche FaceDown en phase Luck.
        /// L'index est l'index courant dans FaceDown : aucune sélection par Id de siège
        /// n'est déduite implicitement.
        /// </summary>
        public PlayResult ApplyBlindPlay(
            GameState state,
            int playerIndex,
            int faceDownIndex)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (faceDownIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(faceDownIndex));

            if (IsGameOver(state))
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.GameAlreadyOver);

            if (playerIndex != state.ActivePlayerIndex)
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.NotYourTurn);

            var player = state.Players[playerIndex];

            if (player.CurrentPhase != DefPhase.Luck)
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.InvalidCards);

            if (player.Hand.Count != 0)
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.InvalidCards);

            if (faceDownIndex >= player.FaceDown.Count)
                return PlayResult.Rejected(
                    state,
                    PlayRejectionReason.InvalidCards);

            var card = player.FaceDown[faceDownIndex];
            var play = Play.CreateSingle(
                player.Id,
                card,
                CardLayer.FaceDown);

            // The revealed card is evaluated before mutation. A final 2 is forbidden
            // in Luck, just like in Work and Talent; it therefore follows pickup flow.
            var playable =
                Psycko.Core.Rules.Validation.CardPlayability.IsPlayable(card, state);

            var terminatesOnTwo =
                play.EffectiveRank == DefRank.Two
                && player.FaceDown.Count == 1;

            if (!playable || terminatesOnTwo)
            {
                var revealed = (GameState)state.PlayCards(play);
                var picked = (GameState)revealed.PickUpPile(playerIndex);

                picked = (GameState)picked.SetConstraint(
                    HeightConstraint.Normal,
                    DefRank.Three);

                var advance = Step6_AdvanceTurnResolver.Resolve(
                    picked,
                    skipNext: false,
                    replay: false);

                picked = ExecuteActivePlayerIntent(picked, advance);

                return PlayResult.Accepted(
                    picked,
                    IsGameOver(picked));
            }

            var revealedState = (GameState)state.PlayCards(play);
            var effects = Step3_CardEffectsResolver.Resolve(
                revealedState,
                play);

            // Step 5 must inspect the revealed card while it is still on the pile.
            // Destruction (2/quad) is applied only after quad/pair detection.
            var pileEffects = Step5_PileEffectsResolver.Resolve(
                revealedState,
                play,
                effects.WithState(revealedState));

            var next = pileEffects.State;

            if (effects.DestroysPile || pileEffects.DestroysPile)
            {
                next = (GameState)next.DestroyPile();
            }

            if (effects.NextConstraint.HasValue
                && effects.NextRefRank.HasValue)
            {
                next = (GameState)next.SetConstraint(
                    effects.NextConstraint.Value,
                    effects.NextRefRank.Value);
            }

            if (effects.NextDirection.HasValue)
            {
                next = (GameState)next.WithDirection(
                    effects.NextDirection.Value);
            }

            var updatedPlayer = next.Players[playerIndex];

            if (!updatedPlayer.HasCards)
            {
                next = (GameState)next.AdvancePlayerPhase(playerIndex);
            }

            var advanceValid = Step6_AdvanceTurnResolver.Resolve(
                next,
                pileEffects.SkipNext,
                pileEffects.Replay);

            next = ExecuteActivePlayerIntent(next, advanceValid);

            return PlayResult.Accepted(
                next,
                IsGameOver(next));
        }

        /// <summary>
        /// Exécute une seule fois l'intention calculée par Step6. Une intention nulle
        /// (rejeu) signifie que l'index reste strictement inchangé et ne déclenche
        /// aucun SetActivePlayer.
        /// </summary>
        private static GameState ExecuteActivePlayerIntent(
            GameState state,
            TurnResult step6Result)
        {
            if (!step6Result.NextActivePlayerIndex.HasValue)
                return state;

            return (GameState)((IGameStateCommand)state).SetActivePlayer(
                step6Result.NextActivePlayerIndex.Value);
        }

        public PlayResult ApplyPlay(
            GameState state,
            Play play,
            int playerIndex)
            => ApplyPlay(
                state,
                play,
                playerIndex,
                voluntaryPickupRequested: false);

        internal static bool IsGameOver(GameState state)
        {
            int stillPlaying = 0;

            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.Players[i].CurrentPhase != DefPhase.Finished)
                    stillPlaying++;
            }

            return stillPlaying <= 1;
        }
    }
}