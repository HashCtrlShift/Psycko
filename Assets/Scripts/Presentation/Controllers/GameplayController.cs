using System;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using UnityEngine;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Adaptateur Unity fin: transfère les événements de session à l'interface.</summary>
    public sealed class GameplayController : MonoBehaviour
    {
        private UnityHumanAgent _human;
        private LocalGameSession _session;
        private CardPlayabilityChecker _checker;
        public event Action<GameState> StateChanged;
        public event Action<GameLogEntry> LogEntryReceived;
        public event Action<HumanRequestKind> HumanRequestPending;
        public event Action<GameRunOutcome> GameFinished;
        public UnityHumanAgent Human { get { return _human; } }
        private System.Collections.Generic.IReadOnlyList<Card> _humanHand;
        public System.Collections.Generic.List<Card> HumanHandSorted { get { return HandSorter.Sort(_humanHand); } }
        public static System.Collections.Generic.List<Card> GetSortedHand(Psycko.Core.Interfaces.IPlayerVisibleState state) { return HandSorter.Sort(state == null ? null : state.SelfHand); }

        internal void Begin()
        {
            _checker = new CardPlayabilityChecker(); _human = new UnityHumanAgent();
            var layout = new SeatLayout(new System.Random()).Create(_human, _checker);
            _session = new LocalGameSession(_human, layout); _session.Start(Environment.TickCount);
        }
        internal void Attach(UnityHumanAgent human, LocalGameSession session) { _human = human; _session = session; }
        private void Start() { Begin(); }
        internal void Tick() { Pump(); }
        private void Update() { Pump(); }

        private void Pump()
        {
            if (_session == null) return;
            SessionEvent item;
            while (_session.TryDequeue(out item))
            {
                switch (item.Kind)
                {
                    case SessionEventKind.LogEntry: if (LogEntryReceived != null) LogEntryReceived(item.LogEntry); break;
                    case SessionEventKind.StateChanged:
                        if (item.State != null && item.State.Players.Count > 0)
                            _humanHand = Psycko.Core.Domain.PlayerVisibleState.From(item.State, item.State.Players[0].Id).SelfHand;
                        if (StateChanged != null) StateChanged(item.State);
                        break;
                    case SessionEventKind.HumanRequest: if (HumanRequestPending != null) HumanRequestPending(item.RequestKind); break;
                    case SessionEventKind.Finished: if (GameFinished != null) GameFinished(item.Outcome); break;
                }
            }
        }

        private void OnDestroy() { if (_session != null) _session.Cancel(); }
    }
}
