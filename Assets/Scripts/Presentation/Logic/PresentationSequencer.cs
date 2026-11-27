using System;
using Psycko.Core.Domain;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Logic
{
    /// <summary>Consomme une chronologie FIFO et publie les étapes à présenter. Sans dépendance Unity.</summary>
    public sealed class PresentationSequencer
    {
        // GameplayController / BotPacing utilise actuellement 0.02 s en mode accéléré.
        private const float FastBotDelaySeconds = 0.02f;

        private readonly PresentationTimeline _timeline;
        private readonly PresentationTimingValues _timings;
        private float _remaining;
        private GameState _placedState;
        private bool _awaitingPlacedFollowup;
        private bool _previewDelayStarted;
        private bool _placedHasEffect;
        private bool _waitingForRequest;
        private bool _finished;

        private bool _forcedPickupTimerStarted;
        private bool _forcedPickupTimerElapsed;
        private float _forcedPickupRemaining;

        public GameState CurrentState { get; private set; }
        public TimelineStep CurrentStep { get; private set; }
        public bool IsWaitingForRequest { get { return _waitingForRequest; } }
        public bool IsFinished { get { return _finished; } }
        public bool IsFastMode { get; set; }
        public bool IsForcedPickupTimerElapsed { get { return _forcedPickupTimerElapsed; } }
        public event Action<TimelineStep> StepPresented;
        public event Action ForcedPickupDelayElapsed;

        public PresentationSequencer(PresentationTimeline timeline, PresentationTimingValues timings)
        {
            if (timeline == null) throw new ArgumentNullException(nameof(timeline));
            _timeline = timeline;
            _timings = timings;
        }

        /// <summary>Avance d'une durée explicite; les durées négatives sont traitées comme zéro.</summary>
        public void Tick(float dt)
        {
            float elapsed = dt < 0f ? 0f : dt;
            AdvanceForcedPickupTimer(elapsed);
            if (_finished || _waitingForRequest) return;

            _remaining = Math.Max(0f, _remaining - elapsed);
            if (_remaining > 0f) return;

            while (!_finished && !_waitingForRequest)
            {
                if (_awaitingPlacedFollowup)
                {
                    TimelineStep pending;
                    if (!_timeline.TryPeek(out pending)) return;
                    // Les logs intercalés gardent leur place FIFO sans rompre la paire Placed/Resolved.
                    if (pending.Kind == TimelineStepKind.Log)
                    {
                        _timeline.TryDequeue(out pending);
                        Publish(pending);
                        continue;
                    }
                    if (!PreparePlacedFollowup()) return;
                }

                TimelineStep step;
                if (!_timeline.TryDequeue(out step)) return;

                if (step.Kind == TimelineStepKind.SnapshotPlaced)
                {
                    _placedState = step.State;
                    _awaitingPlacedFollowup = true;
                    _previewDelayStarted = false;
                    _placedHasEffect = step.PlacementInfo.HasPreviewEffect;
                    if (!IsFastMode) Publish(step);
                    // Continue pour inspecter la suite FIFO et démarrer l'aperçu sans retard d'une frame.
                    continue;
                }

                Publish(step);
                if (step.Kind == TimelineStepKind.SnapshotResolved)
                {
                    _remaining = IsFastMode ? FastBotDelaySeconds : _timings.BotDelaySeconds;
                    if (_remaining > 0f) return;
                    continue;
                }

                if (step.Kind == TimelineStepKind.Request)
                {
                    _waitingForRequest = true;
                    return;
                }
                if (step.Kind == TimelineStepKind.Finished)
                {
                    _finished = true;
                    return;
                }
            }
        }

        /// <summary>Avance avec l'horloge injectée.</summary>
        public void Tick(ISequencerClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            Tick(clock.DeltaTimeSeconds);
        }

        /// <summary>Reprend après réponse à une étape Request.</summary>
        public void Resume()
        {
            _waitingForRequest = false;
        }

        /// <summary>Alias explicite de Resume pour le code de traitement des requêtes.</summary>
        public void NotifyRequestAnswered()
        {
            Resume();
        }

        /// <summary>
        /// Arme une fois par instance le délai de ramassage forcé. Le callback est un signal seulement;
        /// l'exécution effective du ramassage appartient à T65-f.
        /// </summary>
        public bool StartForcedPickupTimer()
        {
            if (_forcedPickupTimerStarted) return false;
            _forcedPickupTimerStarted = true;
            _forcedPickupRemaining = _timings.ForcedPickupDelaySeconds;
            if (_forcedPickupRemaining <= 0f) FireForcedPickupSignal();
            return true;
        }

        private bool PreparePlacedFollowup()
        {
            TimelineStep following;
            // Maintient Placed affiché tant que la résolution n'est pas encore arrivée en FIFO.
            if (!_timeline.TryPeek(out following)) return false;

            bool shrinking = following.Kind == TimelineStepKind.SnapshotResolved
                && WillPileShrink(_placedState, following.State);
            bool pickupFollows = following.Kind == TimelineStepKind.Request
                && following.RequestKind == HumanRequestKind.Pickup;
            bool shouldPreview = shrinking || _placedHasEffect || pickupFollows;
            if (shouldPreview && !IsFastMode && !_previewDelayStarted)
            {
                _previewDelayStarted = true;
                _remaining = _timings.PreviewBeforeClearSeconds;
            }
            if (_remaining > 0f) return false;
            _awaitingPlacedFollowup = false;
            _previewDelayStarted = false;
            _placedState = null;
            return true;
        }

        private void AdvanceForcedPickupTimer(float elapsed)
        {
            if (!_forcedPickupTimerStarted || _forcedPickupTimerElapsed) return;
            _forcedPickupRemaining = Math.Max(0f, _forcedPickupRemaining - elapsed);
            if (_forcedPickupRemaining <= 0f) FireForcedPickupSignal();
        }

        private void FireForcedPickupSignal()
        {
            if (_forcedPickupTimerElapsed) return;
            _forcedPickupTimerElapsed = true;
            Action handler = ForcedPickupDelayElapsed;
            if (handler != null) handler();
        }

        private void Publish(TimelineStep step)
        {
            CurrentStep = step;
            if (step.State != null) CurrentState = step.State;
            Action<TimelineStep> handler = StepPresented;
            if (handler != null) handler(step);
        }

        private static bool WillPileShrink(GameState placed, GameState resolved)
        {
            return placed != null && resolved != null
                && resolved.Pile.Count < placed.Pile.Count;
        }
    }
}
