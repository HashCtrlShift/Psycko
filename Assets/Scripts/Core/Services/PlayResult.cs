using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Services.TurnManager;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Résultat typé et immuable d'un appel à GameOrchestrator.ApplyPlay.
    /// Success == false  → NewState est l'état d'entrée inchangé, RejectionReason renseigné.
    /// Success == true   → RejectionReason == null.
    /// PendingGift != null → un Don du 7 est en attente de résolution : l'appelant DOIT
    ///   fournir un GiftResolutionChoice à GameOrchestrator.ResolveGiftAndContinue(...)
    ///   avant que le tour ne soit considéré comme terminé. NewState porte alors l'état
    ///   après Step3 (main du donateur pas encore modifiée), jamais un état devinable.
    /// </summary>
    public readonly struct PlayResult
    {
        public bool Success { get; }
        public GameState NewState { get; }
        public PlayRejectionReason? RejectionReason { get; }
        public bool IsGameOver { get; }
        public IReadOnlyList<int> ForcedPickupPlayerIds { get; }
        public TurnResult? PendingGift { get; }

        private PlayResult(bool success, GameState newState, PlayRejectionReason? reason, bool isGameOver, IReadOnlyList<int>? forcedPickupPlayerIds, TurnResult? pendingGift)
        {
            Success = success;
            NewState = newState;
            RejectionReason = reason;
            IsGameOver = isGameOver;
            ForcedPickupPlayerIds = forcedPickupPlayerIds ?? new List<int>();
            PendingGift = pendingGift;
        }

        public static PlayResult Accepted(GameState newState, bool isGameOver, IReadOnlyList<int>? forcedPickupPlayerIds = null)
            => new PlayResult(true, newState, null, isGameOver, forcedPickupPlayerIds, null);

        public static PlayResult Rejected(GameState unchangedState, PlayRejectionReason reason)
            => new PlayResult(false, unchangedState, reason, false, new List<int>(), null);

        /// <summary>
        /// Don du 7 en attente : Success reste true (ce n'est pas un rejet), mais le tour
        /// n'est pas terminé. L'appelant doit résoudre via ResolveGiftAndContinue avant
        /// que NewState reflète l'état final du tour.
        /// </summary>
        public static PlayResult AwaitingGift(TurnResult pendingResult)
            => new PlayResult(true, pendingResult.State, null, false, new List<int>(), pendingResult);
    }
}