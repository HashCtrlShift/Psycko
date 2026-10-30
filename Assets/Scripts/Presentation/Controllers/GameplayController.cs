using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Domain.Log;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using UnityEngine;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Adaptateur Unity fin: transfère les événements de session à l'interface.</summary>
    public sealed class GameplayController : MonoBehaviour
    {
        [SerializeField] private bool _verboseDiagnostics = true;

        private UnityHumanAgent _human;
        private LocalGameSession _session;
        private CardPlayabilityChecker _checker;
        private IReadOnlyList<Card> _humanHand;
        private GameState _lastState;

        public event Action<GameState> StateChanged;
        public event Action<GameLogEntry> LogEntryReceived;
        public event Action<HumanRequestKind> HumanRequestPending;
        public event Action<GameRunOutcome> GameFinished;

        public UnityHumanAgent Human { get { return _human; } }

        /// <summary>Dernier état reçu (permet à une vue qui s'abonne tard de se synchroniser).</summary>
        public GameState LastState { get { return _lastState; } }

        public List<Card> HumanHandSorted { get { return HandSorter.Sort(_humanHand); } }

        public static List<Card> GetSortedHand(Psycko.Core.Interfaces.IPlayerVisibleState state)
        {
            return HandSorter.Sort(state == null ? null : state.SelfHand);
        }

        internal void Begin()
        {
            _checker = new CardPlayabilityChecker();
            _human = new UnityHumanAgent();
            var layout = new SeatLayout(new System.Random()).Create(_human, _checker);
            _session = new LocalGameSession(_human, layout);
            _session.Start(Environment.TickCount);
            Log("Session démarrée");
        }

        internal void Attach(UnityHumanAgent human, LocalGameSession session)
        {
            _human = human;
            _session = session;
        }

        internal void Tick() { Pump(); }

        private void OnEnable() { HumanRequestPending += OnHumanRequest; }

        private void OnDisable() { HumanRequestPending -= OnHumanRequest; }

        private void Start() { Begin(); }

        private void Update() { Pump(); }

        private void Pump()
        {
            if (_session == null) return;

            SessionEvent item;
            while (_session.TryDequeue(out item))
            {
                Log("Event: " + item.Kind);

                switch (item.Kind)
                {
                    case SessionEventKind.LogEntry:
                        if (LogEntryReceived != null) LogEntryReceived(item.LogEntry);
                        break;

                    case SessionEventKind.StateChanged:
                        HandleStateChanged(item.State);
                        break;

                    case SessionEventKind.HumanRequest:
                        Log("HumanRequest kind = " + item.RequestKind
                            + ", abonnés = " + (HumanRequestPending != null));
                        if (HumanRequestPending != null) HumanRequestPending(item.RequestKind);
                        break;

                    case SessionEventKind.Finished:
                        if (item.Outcome != null && item.Outcome.Error != null)
                            Debug.LogError("[Gameplay] Partie plantée : " + item.Outcome.Error);
                        if (GameFinished != null) GameFinished(item.Outcome);
                        break;
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            _lastState = state;

            if (state != null && state.Players.Count > 0)
            {
                _humanHand = PlayerVisibleState.From(state, state.Players[0].Id).SelfHand;
            }

            if (StateChanged != null) StateChanged(state);
        }

        /// <summary>
        /// Réponse par défaut à la demande d'échange pré-partie : aucun échange (Prêt).
        /// Comportement volontaire tant que l'UI d'échange n'existe pas.
        /// </summary>
        private void OnHumanRequest(HumanRequestKind kind)
        {
            if (kind != HumanRequestKind.Exchanges) return;
            if (_human == null) return;

            Log("Échange pré-partie : aucun échange (réponse par défaut, UI d'échange à venir)");
            _human.SubmitExchanges(new List<ExchangeChoice>().AsReadOnly());
        }

        private void Log(string message)
        {
            if (_verboseDiagnostics) Debug.Log("[Gameplay] " + message);
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Cancel();
        }
    }
}
