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