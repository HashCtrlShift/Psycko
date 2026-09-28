using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Services.TurnManager;
using TurnManagerService = Psycko.Core.Services.TurnManager.TurnManager;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Seul point d'entrée autorisé à appeler IGameStateCommand.
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
                afterPickup = advance.State;

                IReadOnlyList<int> forcedIds = forcedPickup
                    ? new List<int> { play.PlayerId }
                    : new List<int>();
                return PlayResult.Accepted(afterPickup, IsGameOver(afterPickup), forcedIds);
            }

            var turnResult = TurnManagerService.ApplyPlay(state, play);
            var newState = turnResult.State;

            // Les intentions de mutation produites par les Steps (DrawCards,
            // DestroyPile, Don, etc.) nécessitent les résolveurs/contrats non
            // fournis dans cette passe. Elles ne sont donc pas devinées ici.
            return PlayResult.Accepted(newState, IsGameOver(newState));
        }


        /// <summary>
        /// Révèle et résout une carte de la couche FaceDown en phase Luck.
        /// L'index est l'index courant dans FaceDown : aucune sélection par Id de siège
        /// n'est déduite implicitement.
        /// </summary>
        public PlayResult ApplyBlindPlay(GameState state, int playerIndex, int faceDownIndex)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            if (faceDownIndex < 0) throw new ArgumentOutOfRangeException(nameof(faceDownIndex));

            if (IsGameOver(state))
                return PlayResult.Rejected(state, PlayRejectionReason.GameAlreadyOver);
            if (playerIndex != state.ActivePlayerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            var player = state.Players[playerIndex];
            if (player.CurrentPhase != DefPhase.Luck)
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);
            if (player.Hand.Count != 0)
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);
            if (faceDownIndex >= player.FaceDown.Count)
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);

            var card = player.FaceDown[faceDownIndex];
            var play = Play.CreateSingle(player.Id, card, CardLayer.FaceDown);

            // The revealed card is evaluated before mutation. A final 2 is forbidden
            // in Luck, just like in Work and Talent; it therefore follows pickup flow.
            var playable = Psycko.Core.Rules.Validation.CardPlayability.IsPlayable(card, state);
            var terminatesOnTwo = play.EffectiveRank == DefRank.Two && player.FaceDown.Count == 1;
            if (!playable || terminatesOnTwo)
            {
                var revealed = (GameState)state.PlayCards(play);
                var picked = (GameState)revealed.PickUpPile(playerIndex);
                picked = (GameState)picked.SetConstraint(HeightConstraint.Normal, DefRank.Three);
                var advance = Step6_AdvanceTurnResolver.Resolve(picked, false, false);
                return PlayResult.Accepted(advance.State, IsGameOver(advance.State));
            }

            var revealedState = (GameState)state.PlayCards(play);
            var effects = Step3_CardEffectsResolver.Resolve(revealedState, play);
            // Step 5 must inspect the revealed card while it is still on the pile.
            // Destruction (2/quad) is applied only after quad/pair detection.
            var pileEffects = Step5_PileEffectsResolver.Resolve(
                revealedState, play, effects.WithState(revealedState));
            var next = pileEffects.State;
            if (effects.DestroysPile || pileEffects.DestroysPile)
                next = (GameState)next.DestroyPile();
            if (effects.NextConstraint.HasValue && effects.NextRefRank.HasValue)
                next = (GameState)next.SetConstraint(effects.NextConstraint.Value, effects.NextRefRank.Value);
            if (effects.NextDirection.HasValue)
                next = (GameState)next.WithDirection(effects.NextDirection.Value);

            var updatedPlayer = next.Players[playerIndex];
            if (!updatedPlayer.HasCards)
                next = (GameState)next.AdvancePlayerPhase(playerIndex);

            var advanceValid = Step6_AdvanceTurnResolver.Resolve(
                next, pileEffects.SkipNext, pileEffects.Replay);
            return PlayResult.Accepted(advanceValid.State, IsGameOver(advanceValid.State));
        }

        public PlayResult ApplyPlay(GameState state, Play play, int playerIndex)
            => ApplyPlay(state, play, playerIndex, voluntaryPickupRequested: false);

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