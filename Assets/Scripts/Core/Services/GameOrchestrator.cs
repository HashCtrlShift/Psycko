using System;
using System.Linq;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.Turn;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Source unique d'exécution des intentions, notamment Step6 : le resolver
    /// calcule, cet orchestrateur appelle SetActivePlayer au plus une fois.
    /// Logging (T38b) : side-effect pur via IGameLogRecorder optionnel,
    /// aucune branche logique ne dépend du recorder.
    /// </summary>
    public sealed class GameOrchestrator
    {
        public PlayResult ApplyPlay(
            GameState state,
            Play play,
            int playerIndex,
            IGameLogRecorder recorder = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (play == null) throw new ArgumentNullException(nameof(play));
            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (GameResultCalculator.IsGameOver(state))
                return PlayResult.Rejected(state, PlayRejectionReason.GameAlreadyOver);

            if (playerIndex != state.ActivePlayerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            if (state.Players[playerIndex].CurrentPhase == DefPhase.Finished)
                return PlayResult.Rejected(state, PlayRejectionReason.PlayerFinished);

            if (state.GetSeatIndex(play.PlayerId) != playerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            var beginTurn = TurnManager.BeginTurn(state);

            if (beginTurn.IsPickup)
            {
                var pickup = TurnManager.ResolvePickup(state, play.PlayerId);
                if (!pickup.IsAccepted)
                {
                    if (!pickup.RejectionReason.HasValue)
                        throw new InvalidOperationException(
                            "Invariant violé : un PickupResolution rejeté doit fournir une raison.");

                    return PlayResult.Rejected(state, pickup.RejectionReason.Value);
                }

                var seatIndex = state.GetSeatIndex(play.PlayerId);

                return ExecutePickup(
                    state,
                    seatIndex,
                    new List<int> { play.PlayerId },
                    ActionKind.PickupPile,
                    recorder);
            }

            var turnResult = TurnManager.ApplyPlay(state, play);

            // Don du 7 : le tour est suspendu après Step3. Aucune mutation n'est appliquée ici
            // (ni pioche, ni destruction, ni changement de joueur) : tout est rejoué par
            // ResolveGiftAndContinue via ResolveRemainder. Le coup est loggé tout de suite,
            // avec la pile telle qu'elle est à ce stade (une destruction éventuelle viendra après le Don).
            if (turnResult.RequiresGiftResolution)
            {
                recorder?.Record(GameLogEntry.PlayerAction(
                    play.PlayerId,
                    ActionKind.Play,
                    play.Cards,
                    PileSnapshot(turnResult.State),
                    effects: GameLogEffectDetector.Detect(
                        play,
                        turnResult.DestroysPile,
                        turnResult.NextConstraint,
                        turnResult.NextDirection?.ToString())));

                return PlayResult.AwaitingGift(turnResult);
            }

            var newState = turnResult.State;

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

            newState = ExecuteActivePlayerIntent(newState, turnResult);

            recorder?.Record(GameLogEntry.PlayerAction(
                play.PlayerId,
                ActionKind.Play,
                play.Cards,
                PileSnapshot(newState),
                effects: GameLogEffectDetector.Detect(
                    play,
                    turnResult.DestroysPile,
                    turnResult.NextConstraint,
                    turnResult.NextDirection?.ToString())));

            return Accept(newState, recorder);
        }

        /// <summary>
        /// Résout le Don obligatoire puis reprend exactement à Step4.
        /// TransferCard est exécuté avant ResolveRemainder (correctif T18-bis).
        /// </summary>
        public PlayResult ResolveGiftAndContinue(
            GameState state,
            Play play,
            TurnResult pendingResult,
            GiftResolutionChoice choice,
            IGameLogRecorder recorder = null)
        {
            if (!pendingResult.RequiresGiftResolution)
                throw new InvalidOperationException(
                    "ResolveGiftAndContinue requiert un TurnResult en attente de Don.");

            var donorIndex = pendingResult.State.ActivePlayerIndex;
            var donor = pendingResult.State.Players[donorIndex];

            if (!donor.Hand.Contains(choice.CardToGive))
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);

            int recipientSeatIndex;
            try
            {
                recipientSeatIndex = pendingResult.State.GetSeatIndex(choice.RecipientPlayerId);
            }
            catch (ArgumentException)
            {
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);
            }

            if (recipientSeatIndex == donorIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);

            var recipient = pendingResult.State.Players[recipientSeatIndex];

            if (recipient.CurrentPhase == DefPhase.Finished)
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);

            var transferred = (GameState)((IGameStateCommand)pendingResult.State)
                .TransferCard(donorIndex, recipientSeatIndex, choice.CardToGive);

            recorder?.Record(GameLogEntry.PlayerAction(
                donor.Id,
                ActionKind.GiftCard,
                new[] { choice.CardToGive },
                PileSnapshot(transferred),
                targetPlayerId: choice.RecipientPlayerId));

            var remainder = TurnManager.ResolveRemainder(
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

            return Accept(newState, recorder);
        }

        /// <summary>
        /// Révèle et résout une carte de la couche FaceDown en phase Luck.
        /// </summary>
        public BlindPlayResolution ApplyBlindPlay(
            GameState state,
            int playerIndex,
            int faceDownIndex,
            IGameLogRecorder recorder = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (faceDownIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(faceDownIndex));

            if (GameResultCalculator.IsGameOver(state))
                throw new InvalidOperationException("GameAlreadyOver: impossible de jouer, la partie est terminée.");

            if (playerIndex != state.ActivePlayerIndex)
                throw new InvalidOperationException("NotYourTurn: ce n'est pas le tour de ce joueur.");

            var player = state.Players[playerIndex];

            if (player.CurrentPhase != DefPhase.Luck)
                throw new InvalidOperationException("InvalidCards: le joueur n'est pas en phase Luck.");

            if (player.Hand.Count != 0)
                throw new InvalidOperationException("InvalidCards: la main doit être vide en phase Luck.");

            if (faceDownIndex >= player.FaceDown.Count)
                throw new ArgumentOutOfRangeException(nameof(faceDownIndex), "InvalidCards: index FaceDown invalide.");

            var card = player.FaceDown[faceDownIndex];
            var play = Play.CreateSingle(player.Id, card, CardLayer.FaceDown);

            var playable =
                Psycko.Core.Rules.Validation.CardPlayability.IsPlayable(card, state);

            var terminatesOnTwo =
                play.EffectiveRank == DefRank.Two
                && player.FaceDown.Count == 1;

            var revealedState = (GameState)state.PlayCards(play);

            if (!playable || terminatesOnTwo)
            {
                // Entrée 1 : "[PlayerId] retourne [carte]" (sans effet).
                recorder?.Record(GameLogEntry.PlayerAction(
                    player.Id, ActionKind.BlindPlay, new[] { card }, PileSnapshot(revealedState)));

                // Entrée 2 : "[PlayerId] ramasse la Pile" (écrite par ExecutePickup).
                var pickupResult = ExecutePickup(
                    revealedState,
                    playerIndex,
                    new List<int>(),
                    ActionKind.PickupPile,
                    recorder);

                return BlindPlayResolution.Of(pickupResult, card);
            }

            var effects = Step3_CardEffectsResolver.Resolve(revealedState, play);

            var pileEffects = Step5_PileEffectsResolver.Resolve(
                revealedState,
                play,
                effects.WithState(revealedState));

            var next = pileEffects.State;
            var destroysPile = effects.DestroysPile || pileEffects.DestroysPile;

            if (destroysPile)
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
                next = (GameState)next.WithDirection(effects.NextDirection.Value);
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

            // Entrée unique : "[PlayerId] retourne [carte]" + effets, pile après résolution.
            recorder?.Record(GameLogEntry.PlayerAction(
                player.Id,
                ActionKind.BlindPlay,
                new[] { card },
                PileSnapshot(next),
                effects: GameLogEffectDetector.Detect(
                    play,
                    destroysPile,
                    effects.NextConstraint,
                    effects.NextDirection?.ToString())));

            return BlindPlayResolution.Of(Accept(next, recorder), card);
        }

        /// <summary>
        /// Ramassage volontaire de la pile (séquence T23 via ExecutePickup).
        /// </summary>
        public static PlayResult RequestPickup(
            GameState state,
            int playerId,
            IGameLogRecorder recorder = null)
        {
            var pickup = TurnManager.ResolvePickup(state, playerId);
            if (!pickup.IsAccepted)
            {
                if (!pickup.RejectionReason.HasValue)
                    throw new InvalidOperationException(
                        "Invariant violé : un PickupResolution rejeté doit fournir une raison.");

                return PlayResult.Rejected(state, pickup.RejectionReason.Value);
            }

            var seatIndex = state.GetSeatIndex(playerId);

            return ExecutePickup(
                state,
                seatIndex,
                new List<int>(),
                ActionKind.RequestPickup,
                recorder);
        }

        private static GameState ExecuteActivePlayerIntent(
            GameState state,
            TurnResult step6Result)
        {
            if (!step6Result.NextActivePlayerIndex.HasValue)
                return state;

            return (GameState)((IGameStateCommand)state).SetActivePlayer(
                step6Result.NextActivePlayerIndex.Value);
        }

        /// <summary>
        /// Séquence de ramassage T23 : PickUpPile → SetConstraint(Normal, Three)
        /// → Step6 → intention SetActivePlayer. La direction n'est jamais modifiée.
        /// </summary>
        private static PlayResult ExecutePickup(
            GameState state,
            int seatIndex,
            IReadOnlyList<int> forcedPickupPlayerIds,
            ActionKind logKind,
            IGameLogRecorder recorder)
        {
            var pickedCards = PileSnapshot(state);

            var afterPickup = (GameState)state.PickUpPile(seatIndex);

            recorder?.Record(GameLogEntry.PlayerAction(
                state.Players[seatIndex].Id, logKind, pickedCards, PileSnapshot(afterPickup)));

            afterPickup = (GameState)afterPickup.SetConstraint(
                HeightConstraint.Normal,
                DefRank.Three);

            var advance = Step6_AdvanceTurnResolver.Resolve(
                afterPickup,
                skipNext: false,
                replay: false);

            afterPickup = ExecuteActivePlayerIntent(afterPickup, advance);

            var isGameOver = GameResultCalculator.IsGameOver(afterPickup);
            if (isGameOver)
                recorder?.Record(GameLogEntry.GameEnd(
                    GameResultCalculator.GetPsyckoPlayerId(afterPickup)));

            return PlayResult.Accepted(afterPickup, isGameOver, forcedPickupPlayerIds);
        }

        /// <summary>Construit le PlayResult accepté et logge la fin de partie si besoin.</summary>
        private static PlayResult Accept(GameState newState, IGameLogRecorder recorder)
        {
            var isGameOver = GameResultCalculator.IsGameOver(newState);
            if (isGameOver)
                recorder?.Record(GameLogEntry.GameEnd(
                    GameResultCalculator.GetPsyckoPlayerId(newState)));

            return PlayResult.Accepted(newState, isGameOver);
        }

        /// <summary>
        /// Copie figée des cartes de la pile centrale (pour le log uniquement).
        /// ⚠️ Seul endroit où le nom de la propriété pile est utilisé : adapter si besoin.
        /// </summary>
        private static IReadOnlyList<Card> PileSnapshot(GameState state)
            => state.Pile.Cards.ToList();
    }
}