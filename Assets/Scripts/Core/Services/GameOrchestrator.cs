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
    /// Effets du log (T38d) : GameLogEffectDetector lit l'état APRÈS pose du coup
    /// et AVANT destruction de la Pile (Carré / Doublon relus sur Pile.Cards / Plays).
    /// Pile détruite (2, Bombe, Carré) : la contrainte est réinitialisée à
    /// (Normal, Three) — pile vide = aucune contrainte.
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
            // Le Doublon n'est loggé qu'ici (une seule fois) ; le Carré est loggé après le Don.
            if (turnResult.RequiresGiftResolution)
            {
                recorder?.Record(GameLogEntry.PlayerAction(
                    play.PlayerId,
                    ActionKind.Play,
                    play.Cards,
                    PileSnapshot(turnResult.State),
                    effects: GameLogEffectDetector.Detect(
                        play,
                        turnResult.State,
                        turnResult.DestroysPile,
                        turnResult.NextDirection,
                        skipApplied: HasMoreThanTwoActivePlayers(turnResult.State))));

                return PlayResult.AwaitingGift(turnResult);
            }

            // État après pose, avant toute destruction : sert au détecteur du log.
            var stateAfterPlacement = turnResult.State;

            var newState = ApplyTurnIntents(
                turnResult.State,
                turnResult,
                state.ActivePlayerIndex);

            recorder?.Record(GameLogEntry.PlayerAction(
                play.PlayerId,
                ActionKind.Play,
                play.Cards,
                PileSnapshot(newState),
                effects: GameLogEffectDetector.Detect(
                    play,
                    stateAfterPlacement,
                    turnResult.DestroysPile,
                    turnResult.NextDirection,
                    skipApplied: turnResult.SkipNext)));

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

            // État après Don, avant destruction : la Pile contient toujours le coup.
            var stateBeforeDestruction = remainder.State;

            var newState = ApplyTurnIntents(
                remainder.State,
                remainder,
                pendingResult.State.ActivePlayerIndex);

            // Suite du coup de 7 : seul le Carré (destruction) est loggé ici,
            // sur la ligne qui suit le Don. Le Doublon a déjà été loggé avant le Don.
            var postGiftEffects = GameLogEffectDetector.Detect(
                play,
                stateBeforeDestruction,
                remainder.DestroysPile,
                remainder.NextDirection,
                afterGift: true);

            if (postGiftEffects.Count > 0)
            {
                recorder?.Record(GameLogEntry.PlayerAction(
                    play.PlayerId,
                    ActionKind.Play,
                    new List<Card>(),
                    PileSnapshot(newState),
                    effects: postGiftEffects));
            }

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

            // État après pose, avant destruction : sert au détecteur du log.
            var stateBeforeDestruction = pileEffects.State;

            var next = pileEffects.State;
            var destroysPile = effects.DestroysPile || pileEffects.DestroysPile;

            if (destroysPile)
            {
                next = (GameState)next.DestroyPile();

                // Pile vide = aucune contrainte (couvre 2, Bombe et Carré).
                next = (GameState)next.SetConstraint(
                    HeightConstraint.Normal,
                    DefRank.Three);
            }
            else if (effects.NextConstraint.HasValue
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
                    stateBeforeDestruction,
                    destroysPile,
                    effects.NextDirection,
                    skipApplied: pileEffects.SkipNext)));

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

        /// <summary>
        /// Exécute, dans l'ordre strict, les intentions portées par un TurnResult :
        /// pioche Step2 → destruction de Pile (+ contrainte neutre) → contrainte →
        /// direction → pioche finale Step4 → transition de phase → joueur actif Step6.
        /// Factorisée : partagée par ApplyPlay et ResolveGiftAndContinue.
        /// </summary>
        private static GameState ApplyTurnIntents(
            GameState startState,
            TurnResult result,
            int activeSeatIndex)
        {
            var newState = startState;

            if (result.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    activeSeatIndex,
                    result.DrawCount);
            }

            if (result.DestroysPile)
            {
                newState = (GameState)newState.DestroyPile();

                // Pile vide = aucune contrainte (convention du projet).
                // Couvre 2, Bombe et Carré : le Carré est détecté en Step5, donc la
                // contrainte posée par Step3 (Prêtre, Valet, 9...) est obsolète.
                newState = (GameState)newState.SetConstraint(
                    HeightConstraint.Normal,
                    DefRank.Three);
            }
            else if (result.NextConstraint.HasValue
                     && result.NextRefRank.HasValue)
            {
                newState = (GameState)newState.SetConstraint(
                    result.NextConstraint.Value,
                    result.NextRefRank.Value);
            }

            if (result.NextDirection.HasValue)
            {
                newState = (GameState)newState.WithDirection(
                    result.NextDirection.Value);
            }

            if (result.FinalReconstruction.HasValue
                && result.FinalReconstruction.Value.DrawCount > 0)
            {
                newState = (GameState)newState.DrawCards(
                    activeSeatIndex,
                    result.FinalReconstruction.Value.DrawCount);
            }

            if (result.TriggersPhaseTransition)
            {
                newState = (GameState)newState.AdvancePlayerPhase(
                    activeSeatIndex);
            }

            return ExecuteActivePlayerIntent(newState, result);
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
        /// Miroir du test de Step5 (Doublon désactivé à 2 joueurs actifs ou moins).
        /// Utilisé uniquement par le log, avant que Step5 ait tourné (branche Don).
        /// </summary>
        private static bool HasMoreThanTwoActivePlayers(GameState state)
            => state.Players.Count(p => p.CurrentPhase != DefPhase.Finished) > 2;

        /// <summary>
        /// Copie figée des cartes de la pile centrale (pour le log uniquement).
        /// ⚠️ Seul endroit où le nom de la propriété pile est utilisé : adapter si besoin.
        /// </summary>
        private static IReadOnlyList<Card> PileSnapshot(GameState state)
            => state.Pile.Cards.ToList();
    }
}