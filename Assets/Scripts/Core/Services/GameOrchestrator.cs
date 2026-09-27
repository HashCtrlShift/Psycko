using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Seul point d'entrée autorisé à appeler IGameStateCommand.
    ///
    /// T22.a — SQUELETTE, LECTURE SEULE :
    ///   - validation des préconditions (null, bornes, tour, joueur fini, partie finie)
    ///   - aucune mutation, aucun appel aux Steps ni à TurnManager
    ///
    /// Prochains tickets :
    ///   T22.b PickUpPile (Step0) · T22.c PlayCards (Step1) · T22.d SetConstraint
    ///   T22.e DestroyPile (Step5) · T22.f SetActivePlayer/ReverseDirection (Step6)
    ///   T22.g DrawCards/AdvancePlayerPhase/EliminatePlayer + fin de partie
    /// </summary>
    public sealed class GameOrchestrator
    {
        public PlayResult ApplyPlay(GameState state, Play play, int playerIndex)
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

            // T22.b+ : Step0 → PickUpPile, TurnManager.ApplyPlay, mutations des Steps.
            GameState newState = state;

            return PlayResult.Accepted(newState, IsGameOver(newState));
        }

        /// <summary>
        /// Fin de partie : un seul joueur (ou moins) avec CurrentPhase != Finished.
        /// Responsabilité exclusive de l'Orchestrator, jamais déléguée à Step6.
        /// </summary>
        private static bool IsGameOver(GameState state)
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