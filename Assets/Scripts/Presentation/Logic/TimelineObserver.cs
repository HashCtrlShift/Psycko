using System;
using System.Collections.Concurrent;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.Logic
{
    /// <summary>Adaptateur observateur Core vers la chronologie, sans dépendance Unity.</summary>
    public sealed class TimelineObserver : IGameObserver
    {
        private readonly PresentationTimeline _timeline;
        private readonly ConcurrentQueue<PreGameExchangeState> _exchangeStates = new ConcurrentQueue<PreGameExchangeState>();

        public TimelineObserver(PresentationTimeline timeline)
        {
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        }

        public void OnLogEntry(GameLogEntry entry)
        {
            _timeline.Enqueue(TimelineStep.Log(entry));
        }

        public void OnCardsPlaced(GameState stateAfterPlacement, PlacementInfo info)
        {
            _timeline.Enqueue(TimelineStep.SnapshotPlaced(stateAfterPlacement, info));
        }

        public bool TryDequeueExchangeState(out PreGameExchangeState state)
        {
            return _exchangeStates.TryDequeue(out state);
        }

        public void OnExchangeStateChanged(PreGameExchangeState state)
        {
            if (state != null) _exchangeStates.Enqueue(state);
        }

        public void OnStateChanged(GameState state)
        {
            _timeline.Enqueue(TimelineStep.SnapshotResolved(state));
        }
    }
}