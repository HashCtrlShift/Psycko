using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Résultat typé et immuable d'un appel à GameOrchestrator.ApplyPlay.
    /// Success == false  → NewState est l'état d'entrée inchangé, RejectionReason renseigné.
    /// Success == true   → RejectionReason == null.
    /// </summary>
    public readonly struct PlayResult
    {
        public bool Success { get; }
        public GameState NewState { get; }
        public PlayRejectionReason? RejectionReason { get; }
        public bool IsGameOver { get; }
        public IReadOnlyList<int> ForcedPickupPlayerIds { get; }

        private PlayResult(bool success, GameState newState, PlayRejectionReason? reason, bool isGameOver, IReadOnlyList<int> forcedPickupPlayerIds)
        {
            Success = success;
            NewState = newState;
            RejectionReason = reason;
            IsGameOver = isGameOver;
            ForcedPickupPlayerIds = forcedPickupPlayerIds ?? new List<int>();
        }

        public static PlayResult Accepted(GameState newState, bool isGameOver, IReadOnlyList<int> forcedPickupPlayerIds = null)
            => new PlayResult(true, newState, null, isGameOver, forcedPickupPlayerIds);

        public static PlayResult Rejected(GameState unchangedState, PlayRejectionReason reason)
            => new PlayResult(false, unchangedState, reason, false, new List<int>());
    }
}