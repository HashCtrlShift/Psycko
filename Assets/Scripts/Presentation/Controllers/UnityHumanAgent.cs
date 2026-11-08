using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.Controllers
{
    public enum HumanRequestKind { None, NormalPlay, Pickup, FaceDownIndex, Gift, Exchanges }

    /// <summary>Agent humain piloté par l'interface; ses appels bloquants sont conçus pour un thread de jeu dédié.</summary>
    public sealed class UnityHumanAgent : IPlayerAgent
    {
        private readonly object _gate = new object();
        private readonly TimeSpan _timeout;
        private ManualResetEventSlim _response = new ManualResetEventSlim(false);
        private HumanRequestKind _pending;
        private IPlayerVisibleState _state;
        private IFaceDownBlindView _blindView;
        private object _answer;
        private bool _cancelled;
        private bool _expired;
        private readonly Random _random;

        public event Action<HumanRequestKind> RequestPending;
        public HumanRequestKind PendingRequest { get { lock (_gate) return _pending; } }
        public IPlayerVisibleState PendingState { get { lock (_gate) return _state; } }
        public IFaceDownBlindView PendingBlindView { get { lock (_gate) return _blindView; } }
        public TimeSpan Timeout { get { return _timeout; } }

        public UnityHumanAgent() : this(System.Threading.Timeout.InfiniteTimeSpan) { }
        public UnityHumanAgent(TimeSpan timeout) : this(timeout, null) { }
        public UnityHumanAgent(TimeSpan timeout, Random random = null)
        {
            if (timeout != System.Threading.Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Le délai doit être positif ou infini.");
            _timeout = timeout;
            _random = random ?? new Random();
        }

        public Play ProposeNormalPlay(IPlayerVisibleState state) { return (Play)Request(HumanRequestKind.NormalPlay, state, null); }
        public bool DecidePickup(IPlayerVisibleState state, bool isForced) { return (bool)Request(HumanRequestKind.Pickup, state, null, isForced); }
        public int? ProposeFaceDownIndex(IFaceDownBlindView view) { return (int?)Request(HumanRequestKind.FaceDownIndex, null, view); }
        public GiftResolutionChoice ResolveGift(IPlayerVisibleState state) { return (GiftResolutionChoice)Request(HumanRequestKind.Gift, state, null); }
        public IReadOnlyList<ExchangeChoice> ProposeExchanges(IPlayerVisibleState state) { return (IReadOnlyList<ExchangeChoice>)Request(HumanRequestKind.Exchanges, state, null); }

        private object Request(HumanRequestKind kind, IPlayerVisibleState state, IFaceDownBlindView blind, object forced = null)
        {
            Action<HumanRequestKind> handler;
            lock (_gate)
            {
                if (_cancelled) return Fallback(kind, state, forced);
                if (_pending != HumanRequestKind.None) throw new InvalidOperationException("Une requête humaine est déjà en attente.");
                _response.Dispose(); _response = new ManualResetEventSlim(false);
                _pending = kind; _state = state; _blindView = blind; _answer = null; _expired = false; handler = RequestPending;
            }
            if (handler != null) handler(kind);
            bool signaled = _response.Wait(_timeout);
            lock (_gate)
            {
                if (_cancelled || !signaled || _expired) { Clear(); _expired = false; return Fallback(kind, state, forced); }
                object result = _answer; Clear(); _expired = false; return result;
            }
        }
        private object Fallback(HumanRequestKind kind, IPlayerVisibleState state, object forced)
        {
            switch (kind)
            {
                case HumanRequestKind.NormalPlay: return null;
                case HumanRequestKind.Pickup: return forced is bool && (bool)forced;
                case HumanRequestKind.FaceDownIndex: return (int?)0;
                case HumanRequestKind.Gift:
                    if (state == null || state.SelfHand == null || state.SelfHand.Count == 0) return default(GiftResolutionChoice);

                    List<Card> nonJokers = state.SelfHand.Where(card => card != null && !card.IsJoker).ToList();
                    Card cardToGive;
                    if (nonJokers.Count > 0)
                    {
                        cardToGive = nonJokers.OrderBy(card => (int)card.Rank.Value * 4 + (int)card.Suit.Value).First();
                    }
                    else
                    {
                        List<Card> jokers = state.SelfHand.Where(card => card != null && card.IsJoker).ToList();
                        if (jokers.Count == 0) return default(GiftResolutionChoice);
                        cardToGive = jokers[_random.Next(jokers.Count)];
                    }

                    List<IOpponentVisibleInfo> recipients = state.Opponents
                        .Where(opponent => opponent.CurrentPhase != DefPhase.Finished)
                        .ToList();
                    if (recipients.Count == 0) return default(GiftResolutionChoice);
                    IOpponentVisibleInfo recipient = recipients[_random.Next(recipients.Count)];
                    return new GiftResolutionChoice(cardToGive, recipient.PlayerId);
                case HumanRequestKind.Exchanges: return new List<ExchangeChoice>().AsReadOnly();
                default: return null;
            }
        }
        private void Clear() { _pending = HumanRequestKind.None; _state = null; _blindView = null; _answer = null; }
        private void Submit(HumanRequestKind expected, object answer)
        {
            lock (_gate)
            {
                if (_pending == HumanRequestKind.None || _pending != expected) throw new InvalidOperationException("Aucune requête humaine compatible n'est en attente.");
                _answer = answer; _expired = false; _response.Set();
            }
        }
        public void SubmitPlay(Play play) { if (play == null) throw new ArgumentNullException(nameof(play)); Submit(HumanRequestKind.NormalPlay, play); }
        public void SubmitNoPlay() { Submit(HumanRequestKind.NormalPlay, null); }
        public void SubmitPickup(bool pickup) { Submit(HumanRequestKind.Pickup, pickup); }
        public void SubmitFaceDownIndex(int index) { Submit(HumanRequestKind.FaceDownIndex, (int?)index); }
        public void SubmitGift(GiftResolutionChoice choice) { Submit(HumanRequestKind.Gift, choice); }
        public void SubmitExchanges(IReadOnlyList<ExchangeChoice> choices) { if (choices == null) throw new ArgumentNullException(nameof(choices)); Submit(HumanRequestKind.Exchanges, choices); }
        /// <summary>Termine la requête en attente afin qu’elle utilise sa réponse de repli.</summary>
        public void ExpirePending()
        {
            lock (_gate)
            {
                if (_pending != HumanRequestKind.None && !_cancelled && !_response.IsSet)
                {
                    _expired = true;
                    _response.Set();
                }
            }
        }

        public void Cancel()
        {
            lock (_gate) { _cancelled = true; _response.Set(); }
        }
    }
}