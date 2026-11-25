using System;
using System.Collections.Generic;

namespace Psycko.Presentation.Logic
{
    /// <summary>File FIFO sûre au partage entre producteur jeu et consommateur Unity.</summary>
    public sealed class PresentationTimeline
    {
        private readonly object _gate = new object();
        private readonly Queue<TimelineStep> _steps = new Queue<TimelineStep>();
        private bool _finished;

        public int Count
        {
            get { lock (_gate) return _steps.Count; }
        }

        public void Enqueue(TimelineStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            lock (_gate)
            {
                if (_finished) throw new InvalidOperationException("La chronologie est terminée.");
                _steps.Enqueue(step);
                if (step.Kind == TimelineStepKind.Finished) _finished = true;
            }
        }

        public bool TryDequeue(out TimelineStep step)
        {
            lock (_gate)
            {
                if (_steps.Count == 0)
                {
                    step = null;
                    return false;
                }
                step = _steps.Dequeue();
                return true;
            }
        }

        public bool TryPeek(out TimelineStep step)
        {
            lock (_gate)
            {
                if (_steps.Count == 0)
                {
                    step = null;
                    return false;
                }
                step = _steps.Peek();
                return true;
            }
        }
    }
}
