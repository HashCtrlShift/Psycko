using System;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Logic
{
    /// <summary>Nature d'une étape immuable de la chronologie de présentation.</summary>
    public enum TimelineStepKind
    {
        SnapshotPlaced,
        SnapshotResolved,
        Request,
        Log,
        Finished
    }

    /// <summary>Valeur immuable transportant un événement de présentation.</summary>
    public sealed class TimelineStep
    {
        public TimelineStepKind Kind { get; }
        public GameState State { get; }
        public PlacementInfo PlacementInfo { get; }
        public HumanRequestKind RequestKind { get; }
        public IPlayerVisibleState VisibleState { get; }
        public IFaceDownBlindView BlindView { get; }
        public GameLogEntry LogEntry { get; }

        private TimelineStep(TimelineStepKind kind, GameState state,
            HumanRequestKind requestKind, IPlayerVisibleState visibleState,
            IFaceDownBlindView blindView, GameLogEntry logEntry, PlacementInfo placementInfo = default(PlacementInfo))
        {
            Kind = kind;
            State = state;
            RequestKind = requestKind;
            VisibleState = visibleState;
            BlindView = blindView;
            LogEntry = logEntry;
            PlacementInfo = placementInfo;
        }

        public static TimelineStep SnapshotPlaced(GameState state, PlacementInfo info)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new TimelineStep(TimelineStepKind.SnapshotPlaced, state, HumanRequestKind.None, null, null, null, info);
        }

        public static TimelineStep SnapshotResolved(GameState state)
        {
            return Snapshot(TimelineStepKind.SnapshotResolved, state);
        }

        private static TimelineStep Snapshot(TimelineStepKind kind, GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new TimelineStep(kind, state, HumanRequestKind.None, null, null, null);
        }

        public static TimelineStep Request(HumanRequestKind requestKind, IPlayerVisibleState visibleState)
        {
            if (visibleState == null) throw new ArgumentNullException(nameof(visibleState));
            if (requestKind == HumanRequestKind.None) throw new ArgumentOutOfRangeException(nameof(requestKind));
            return new TimelineStep(TimelineStepKind.Request, null, requestKind, visibleState, null, null);
        }

        public static TimelineStep BlindRequest(HumanRequestKind requestKind, IFaceDownBlindView blindView)
        {
            if (blindView == null) throw new ArgumentNullException(nameof(blindView));
            if (requestKind == HumanRequestKind.None) throw new ArgumentOutOfRangeException(nameof(requestKind));
            return new TimelineStep(TimelineStepKind.Request, null, requestKind, null, blindView, null);
        }

        public static TimelineStep Log(GameLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return new TimelineStep(TimelineStepKind.Log, null, HumanRequestKind.None, null, null, entry);
        }

        public static TimelineStep Finished(GameState finalState = null)
        {
            return new TimelineStep(TimelineStepKind.Finished, finalState, HumanRequestKind.None, null, null, null);
        }
    }
}
