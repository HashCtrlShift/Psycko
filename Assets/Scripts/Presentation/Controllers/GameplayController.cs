using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Domain.Log;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services;
using UnityEngine;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Adaptateur Unity fin : transfère les événements de session à l'interface.</summary>
    public sealed class GameplayController : MonoBehaviour
    {
        /// <summary>Siège (et Id) du joueur humain : toujours 0 (voir SeatLayout).</summary>
        private const int HumanPlayerId = 0;

        [SerializeField] private bool _verboseDiagnostics = true;

        [Header("Rythme des bots (T63)")]
        [Tooltip("Délai avant l'affichage de chaque coup de bot tant que l'humain est en jeu (secondes).")]
        [SerializeField, Min(0f)] private float _botDelaySeconds = 1.0f;

        [Tooltip("Délai entre deux coups une fois l'humain sorti du jeu (gagné ou éliminé).")]
        [SerializeField, Min(0f)] private float _fastDelaySeconds = 0.02f;

        [Tooltip("Nombre maximal de coups affichés par frame en mode accéléré.")]
        [SerializeField, Min(1)] private int _fastMaxEventsPerFrame = 8;

        private UnityHumanAgent _human;
        private LocalGameSession _session;
        private CardPlayabilityChecker _checker;
        private IReadOnlyList<Card> _humanHand;
        private GameState _lastState;
        private BotPacing _pacing;

        public event Action<GameState> StateChanged;
        public event Action<GameLogEntry> LogEntryReceived;
        public event Action<HumanRequestKind> HumanRequestPending;
        public event Action<GameRunOutcome> GameFinished;

        public UnityHumanAgent Human { get { return _human; } }
        public ICardPlayabilityChecker PlayabilityChecker { get { return _checker; } }
        public IPlayerVisibleState VisibleState { get { return _human == null ? null : _human.PendingState; } }

        /// <summary>Dernier état reçu (permet à une vue qui s'abonne tard de se synchroniser).</summary>
        public GameState LastState { get { return _lastState; } }

        public List<Card> HumanHandSorted { get { return HandSorter.Sort(_humanHand); } }

        public static List<Card> GetSortedHand(IPlayerVisibleState state)
        {
            return HandSorter.Sort(state == null ? null : state.SelfHand);
        }

        internal void Begin()
        {
            _checker = new CardPlayabilityChecker();
            _human = new UnityHumanAgent();
            var layout = new SeatLayout(new System.Random()).Create(_human, _checker);
            _session = new LocalGameSession(_human, layout);
            _pacing = CreatePacing();
            _session.Start(Environment.TickCount);
            Log("Session démarrée");
        }

        internal void Attach(UnityHumanAgent human, LocalGameSession session)
        {
            _human = human;
            _session = session;
            _pacing = CreatePacing();
        }

        internal void Tick() { Pump(Time.deltaTime); }

        private void Start() { Begin(); }

        private void Update() { Pump(Time.deltaTime); }

        private BotPacing CreatePacing()
        {
            return new BotPacing(_botDelaySeconds, _fastDelaySeconds, _fastMaxEventsPerFrame);
        }

        private void Pump(float deltaTime)
        {
            if (_session == null) return;
            if (_pacing == null) _pacing = CreatePacing();

            _pacing.Advance(deltaTime);

            int stateChangesThisFrame = 0;
            SessionEvent item;
            while (_session.TryPeek(out item))
            {
                // Seuls les StateChanged sont temporisés. Les autres événements passent
                // dès qu'ils sont en tête de file (FIFO strict : rien n'est sauté ni réordonné).
                if (item.Kind == SessionEventKind.StateChanged)
                {
                    if (!_pacing.CanDisplayNext) break;
                    if (stateChangesThisFrame >= _pacing.MaxStateChangesThisFrame) break;
                }

                _session.TryDequeue(out item);
                Log("Event: " + item.Kind);

                switch (item.Kind)
                {
                    case SessionEventKind.LogEntry:
                        if (LogEntryReceived != null) LogEntryReceived(item.LogEntry);
                        break;

                    case SessionEventKind.StateChanged:
                        HandleStateChanged(item.State);
                        stateChangesThisFrame++;
                        _pacing.StartWait();
                        break;

                    case SessionEventKind.HumanRequest:
                        Log("HumanRequest kind = " + item.RequestKind + ", abonnés = " + (HumanRequestPending != null));
                        if (HumanRequestPending != null) HumanRequestPending(item.RequestKind);
                        break;

                    case SessionEventKind.Finished:
                        if (item.Outcome != null && item.Outcome.Error != null)
                            Debug.LogError("[Gameplay] Partie terminée sur erreur : " + item.Outcome.Error);
                        if (GameFinished != null) GameFinished(item.Outcome);
                        break;
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            _lastState = state;
            if (state != null && state.Players.Count > 0)
                _humanHand = PlayerVisibleState.From(state, state.Players[0].Id).SelfHand;

            // Bascule automatique en accéléré dès que l'humain a un rang (gagné ou éliminé).
            if (state != null && !_pacing.IsFast && state.GetFinishRank(HumanPlayerId).HasValue)
            {
                _pacing.EnterFastMode();
                Log("Humain sorti du jeu : mode accéléré");
            }

            if (StateChanged != null) StateChanged(state);
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