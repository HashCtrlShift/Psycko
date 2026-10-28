using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Services;

namespace Psycko.Presentation.Controllers
{
    public enum SessionEventKind { LogEntry, StateChanged, Finished, HumanRequest }
    /// <summary>Événement de session contenant une seule charge utile typée.</summary>
    public sealed class SessionEvent
    {
        public SessionEventKind Kind { get; private set; }
        public GameLogEntry LogEntry { get; private set; }
        public GameState State { get; private set; }
        public GameRunOutcome Outcome { get; private set; }
        public HumanRequestKind RequestKind { get; private set; }
        private SessionEvent() { }
        internal static SessionEvent Log(GameLogEntry e) { return new SessionEvent { Kind = SessionEventKind.LogEntry, LogEntry = e }; }
        internal static SessionEvent Changed(GameState s) { return new SessionEvent { Kind = SessionEventKind.StateChanged, State = s }; }
        internal static SessionEvent Complete(GameRunOutcome o) { return new SessionEvent { Kind = SessionEventKind.Finished, Outcome = o }; }
        internal static SessionEvent Request(HumanRequestKind k) { return new SessionEvent { Kind = SessionEventKind.HumanRequest, RequestKind = k }; }
    }
    /// <summary>Session locale jetable; à remplacer ultérieurement par une source réseau Photon.</summary>
    public sealed class LocalGameSession
    {
        private readonly UnityHumanAgent _human;
        private readonly IReadOnlyList<IPlayerAgent> _agents;
        private readonly IReadOnlyList<string> _names;
        private readonly ConcurrentQueue<SessionEvent> _events = new ConcurrentQueue<SessionEvent>();
        private readonly object _gate = new object();
        private Task _task;
        private GameRunOutcome _outcome;
        private bool _finished;
        public LocalGameSession(UnityHumanAgent human, SeatLayoutResult layout)
        {
            _human = human ?? throw new ArgumentNullException(nameof(human));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            _agents = layout.Agents; _names = layout.Names;
            _human.RequestPending += OnRequestPending;
        }
        public bool IsRunning { get { lock (_gate) return _task != null && !_finished; } }
        public bool IsFinished { get { lock (_gate) return _finished; } }
        public GameRunOutcome Outcome { get { lock (_gate) return _outcome; } }
        public void Start(int seed)
        {
            lock (_gate)
            {
                if (_task != null) throw new InvalidOperationException("La session a déjà démarré.");
                _task = Task.Run(() =>
                {
                    var observer = new SessionObserver(_events);
                    GameRunOutcome result = GameRunner.Run(seed, _agents, _names, true, observer: observer);
                    lock (_gate) { _outcome = result; _finished = true; }
                    _events.Enqueue(SessionEvent.Complete(result));
                });
            }
        }
        public bool TryDequeue(out SessionEvent item) { return _events.TryDequeue(out item); }
        internal void Enqueue(SessionEvent e) { _events.Enqueue(e); }
        public void Cancel() { _human.Cancel(); }
        private void OnRequestPending(HumanRequestKind kind) { _events.Enqueue(SessionEvent.Request(kind)); }
        private sealed class SessionObserver : IGameObserver
        {
            private readonly ConcurrentQueue<SessionEvent> _queue;
            internal SessionObserver(ConcurrentQueue<SessionEvent> queue) { _queue = queue; }
            public void OnLogEntry(GameLogEntry entry) { _queue.Enqueue(SessionEvent.Log(entry)); }
            public void OnStateChanged(GameState state) { _queue.Enqueue(SessionEvent.Changed(state)); }
        }
    }
}
