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

        private PlayResult(bool success, GameState newState, PlayRejectionReason? reason, bool isGameOver)
        {
            Success = success;
            NewState = newState;
            RejectionReason = reason;
            IsGameOver = isGameOver;
        }

        public static PlayResult Accepted(GameState newState, bool isGameOver)
            => new PlayResult(true, newState, null, isGameOver);

        public static PlayResult Rejected(GameState unchangedState, PlayRejectionReason reason)
            => new PlayResult(false, unchangedState, reason, false);
    }
}