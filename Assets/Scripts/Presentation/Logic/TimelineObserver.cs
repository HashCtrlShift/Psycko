using System;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.Logic
{
    /// <summary>Adaptateur observateur Core vers la chronologie, sans dépendance Unity.</summary>
    public sealed class TimelineObserver : IGameObserver
    {
        private readonly PresentationTimeline _timeline;

        public TimelineObserver(PresentationTimeline timeline)
        {
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        }

        public void OnLogEntry(GameLogEntry entry)
        {
            _timeline.Enqueue(TimelineStep.Log(entry));
        }

        public void OnCardsPlaced(GameState stateAfterPlacement)
        {
            _timeline.Enqueue(TimelineStep.SnapshotPlaced(stateAfterPlacement));
        }

        public void OnStateChanged(GameState state)
        {
            _timeline.Enqueue(TimelineStep.SnapshotResolved(state));
        }
    }
}
