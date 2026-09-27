using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Décision immuable d'un ramassage. La mutation réelle reste du ressort de
    /// GameOrchestrator via IGameStateCommand.
    /// </summary>
    public readonly struct PickupResolution
    {
        public GameState State { get; }
        public bool IsAccepted { get; }
        public bool IsPickup { get; }
        public bool IsForced { get; }
        public PlayRejectionReason? RejectionReason { get; }

        private PickupResolution(
            GameState state,
            bool accepted,
            bool isPickup,
            bool isForced,
            PlayRejectionReason? rejectionReason)
        {
            State = state;
            IsAccepted = accepted;
            IsPickup = isPickup;
            IsForced = isForced;
            RejectionReason = rejectionReason;
        }

        public static PickupResolution NoPickup(GameState state)
            => new PickupResolution(state, true, false, false, null);

        public static PickupResolution Accepted(GameState state, bool isForced)
            => new PickupResolution(state, true, true, isForced, null);

        public static PickupResolution Rejected(GameState state, PlayRejectionReason reason)
            => new PickupResolution(state, false, false, false, reason);
    }
}
