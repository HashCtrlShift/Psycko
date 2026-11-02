using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Presentation.Models;
using Psycko.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Relie les interactions UGUI aux réponses de UnityHumanAgent, sans appliquer les règles de jeu.</summary>
    public sealed class InputHandler : MonoBehaviour
    {
        [SerializeField] private GameplayController _controller;
        [SerializeField] private GameTableView _table;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _pickupButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private Text _timerLabel;
        [SerializeField] private float _exchangeSeconds = 90f;

        private readonly HumanSelectionState _selection = new HumanSelectionState();
        private readonly List<ExchangeChoice> _exchanges = new List<ExchangeChoice>();
        private readonly Dictionary<int, IOpponentVisibleInfo> _opponents = new Dictionary<int, IOpponentVisibleInfo>();
        private HumanRequestKind _request;
        private IPlayerVisibleState _pending;
        private IFaceDownBlindView _blind;
        private Card _pendingExchangeHand;
        private Card _giftCard;
        private float _exchangeRemaining;
        private bool _submitted;

        private void OnEnable()
        {
            if (_controller != null) { _controller.HumanRequestPending += OnRequest; _controller.StateChanged += OnState; }
            if (_table != null) _table.Rendered += OnTableRendered;
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_pickupButton != null) _pickupButton.onClick.AddListener(OnPickupClicked);
            if (_readyButton != null) _readyButton.onClick.AddListener(OnReadyClicked);
            SubscribeSeats(true);
            SetAllSeatsClickable(false);
            RefreshButtons();
        }

        private void OnDisable()
        {
            if (_controller != null) { _controller.HumanRequestPending -= OnRequest; _controller.StateChanged -= OnState; }
            if (_table != null) _table.Rendered -= OnTableRendered;
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_pickupButton != null) _pickupButton.onClick.RemoveListener(OnPickupClicked);
            if (_readyButton != null) _readyButton.onClick.RemoveListener(OnReadyClicked);
            SubscribeSeats(false);
            SetAllSeatsClickable(false);
        }

        private void SubscribeSeats(bool add)
        {
            if (_table == null) return;
            foreach (var seat in _table.AllSeatViews)
            {
                if (seat == null) continue;
                if (add) { seat.HandCardClicked += OnHandCard; seat.FaceUpCardClicked += OnFaceUpCard; seat.FaceDownIndexClicked += OnFaceDownIndex; seat.SeatClicked += OnSeatClicked; }
                else { seat.HandCardClicked -= OnHandCard; seat.FaceUpCardClicked -= OnFaceUpCard; seat.FaceDownIndexClicked -= OnFaceDownIndex; seat.SeatClicked -= OnSeatClicked; }
            }
        }

        private void OnTableRendered() { RefreshVisibleState(); ReapplyCardStates(); }

        private void OnState(GameState state)
        {
            // GameplayController dépile la file dans Update : on est sur le Main Thread.
            RefreshVisibleState(); ReapplyCardStates();
        }

        private void OnRequest(HumanRequestKind kind)
        {
            _request = kind; _submitted = false;
            _pending = _controller == null ? null : _controller.VisibleState;
            _blind = _controller == null || _controller.Human == null ? null : _controller.Human.PendingBlindView;
            if (kind == HumanRequestKind.NormalPlay) { _selection.Clear(); _giftCard = null; }
            if (kind == HumanRequestKind.Gift)
            {
                _giftCard = null; _selection.Clear();
                // La main a changé (cartes posées) : on redessine depuis la vue de la requête.
                if (_table != null && _pending != null) _table.RenderVisible(_pending);
            }
            if (kind == HumanRequestKind.Exchanges)
            {
                ClearExchangePreview(); _exchanges.Clear(); _pendingExchangeHand = null;
                _exchangeRemaining = Mathf.Max(0f, _exchangeSeconds);
                // Pendant l'échange, le Core n'émet aucun StateChanged : on dessine depuis la vue de la requête.
                if (_table != null && _pending != null) _table.RenderVisible(_pending);
            }
            RefreshVisibleState(); ReapplyCardStates(); RefreshButtons();
        }

        private void RefreshVisibleState()
        {
            if (_pending == null && _controller != null) _pending = _controller.VisibleState;
            _opponents.Clear();
            if (_pending != null) foreach (var opponent in _pending.Opponents) _opponents[opponent.PlayerId] = opponent;
            UpdateSeatClickability();
        }

        private void Update()
        {
            if (_request != HumanRequestKind.Exchanges || _submitted) return;
            _exchangeRemaining = Mathf.Max(0f, _exchangeRemaining - Time.deltaTime);
            if (_timerLabel != null) _timerLabel.text = Mathf.CeilToInt(_exchangeRemaining) + " s";
            if (_exchangeRemaining <= 0f) SubmitExchanges();
        }

        private void OnHandCard(Card card, CardView view)
        {
            if (card == null || view == null) return;
            if (_request == HumanRequestKind.NormalPlay)
            {
                _selection.Toggle(card);
                ReapplyCardStates();
            }
            else if (_request == HumanRequestKind.Gift)
            {
                // Garde : seule une carte réellement en main peut être donnée.
                if (_pending == null || !_pending.SelfHand.Contains(card)) return;
                _giftCard = card;
                UpdateSeatClickability(); ReapplyCardStates();
            }
            else if (_request == HumanRequestKind.Exchanges)
            {
                if (_exchanges.Any(x => x.HandCard == card)) return;
                _pendingExchangeHand = card; ApplyExchangePreview();
            }
        }

        private void OnFaceUpCard(Card card, CardView view)
        {
            if (_request != HumanRequestKind.Exchanges || card == null || _pendingExchangeHand == null) return;
            if (_exchanges.Any(x => x.FaceUpCard == card)) return;
            _exchanges.Add(new ExchangeChoice(_pendingExchangeHand, card)); _pendingExchangeHand = null;
            ApplyExchangePreview(); RefreshButtons();
        }

        private void OnFaceDownIndex(int index)
        {
            if (_request != HumanRequestKind.FaceDownIndex || _blind == null || _controller.Human == null || _submitted) return;
            if (index < 0 || index >= _blind.SelfFaceDownCount) return;
            _submitted = true; _controller.Human.SubmitFaceDownIndex(index); HideActions();
        }

        private void OnSeatClicked(int playerId) { ChooseGiftRecipient(playerId); }

        private void ChooseGiftRecipient(int playerId)
        {
            IOpponentVisibleInfo opponent;
            if (_request != HumanRequestKind.Gift || _submitted || _giftCard == null || _pending == null || _controller.Human == null) return;
            if (playerId == _pending.SelfPlayerId || !_opponents.TryGetValue(playerId, out opponent) || opponent.CurrentPhase == DefPhase.Finished) return;
            _submitted = true;
            _controller.Human.SubmitGift(new GiftResolutionChoice(_giftCard, playerId));
            HideActions(); SetAllSeatsClickable(false);
        }

        private void OnPlayClicked()
        {
            if (_request != HumanRequestKind.NormalPlay || _submitted || _selection.IsEmpty || !EverySelectedCardPlayable()
                || _controller == null || _controller.Human == null || _pending == null) return;
            _submitted = true;
            _controller.Human.SubmitPlay(Play.Create(_pending.SelfPlayerId, _selection.Cards));
            _selection.Clear(); HideActions();
        }

        private void OnPickupClicked()
        {
            if (_submitted || _controller == null || _controller.Human == null) return;
            // GameRunner passe par ProposeNormalPlay : null = ramassage volontaire.
            if (_request == HumanRequestKind.NormalPlay) { _submitted = true; _controller.Human.SubmitNoPlay(); }
            else if (_request == HumanRequestKind.Pickup) { _submitted = true; _controller.Human.SubmitPickup(true); }
            else return;
            HideActions();
        }

        private void OnReadyClicked() { if (_request == HumanRequestKind.Exchanges) SubmitExchanges(); }

        private void SubmitExchanges()
        {
            if (_request != HumanRequestKind.Exchanges || _submitted || _controller == null || _controller.Human == null) return;
            _submitted = true; ClearExchangePreview();
            _controller.Human.SubmitExchanges(_exchanges.AsReadOnly());
            _request = HumanRequestKind.None; HideActions();
        }

        private void ReapplyCardStates()
        {
            if (_table == null) return;
            var self = _table.SelfSeat;
            if (self != null)
            {
                var hand = self.HandViews;
                if (_request == HumanRequestKind.NormalPlay)
                    _selection.Prune(hand.Where(v => v != null).Select(v => v.Card), IsPlayable);
                foreach (var view in hand)
                {
                    if (view == null || view.Card == null) continue;
                    bool giftMode = _request == HumanRequestKind.Gift && !_submitted;
                    // Don : toute carte de la main est donnable, donc surlignée ; la choisie est levée.
                    bool playable = (_request == HumanRequestKind.NormalPlay && IsPlayable(view.Card)) || giftMode;
                    bool selected = (_request == HumanRequestKind.NormalPlay && _selection.Cards.Contains(view.Card))
                        || (giftMode && view.Card == _giftCard);
                    view.SetState(selected ? CardVisualState.Selected : playable ? CardVisualState.Playable : CardVisualState.Normal);
                    view.SetInteractable(_request == HumanRequestKind.NormalPlay || _request == HumanRequestKind.Gift || _request == HumanRequestKind.Exchanges);
                }
                foreach (var view in self.FaceUpViews) if (view != null) view.SetInteractable(_request == HumanRequestKind.Exchanges);
                foreach (var view in self.FaceDownViews) if (view != null) view.SetInteractable(_request == HumanRequestKind.FaceDownIndex);
            }
            RefreshButtons();
        }

        private bool IsPlayable(Card card)
        {
            ICardPlayabilityChecker checker = _controller == null ? null : _controller.PlayabilityChecker;
            return card != null && _pending != null && checker != null && checker.IsPlayable(card, _pending);
        }

        private bool EverySelectedCardPlayable() { return !_selection.IsEmpty && _selection.Cards.All(IsPlayable); }

        private void RefreshButtons()
        {
            bool play = _request == HumanRequestKind.NormalPlay && !_submitted;
            bool pickup = (_request == HumanRequestKind.NormalPlay || _request == HumanRequestKind.Pickup) && !_submitted;
            bool exchanges = _request == HumanRequestKind.Exchanges && !_submitted;
            SetButton(_playButton, play, play && EverySelectedCardPlayable());
            SetButton(_pickupButton, pickup, pickup);
            SetButton(_readyButton, exchanges, exchanges);
            if (_timerLabel != null) _timerLabel.gameObject.SetActive(exchanges);
            if (!exchanges && _timerLabel != null) _timerLabel.text = string.Empty;
        }

        private static void SetButton(Button button, bool visible, bool interactable)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible); button.interactable = interactable;
        }

        private void HideActions() { RefreshButtons(); SetAllSeatsClickable(false); }

        private void UpdateSeatClickability()
        {
            if (_table == null) return;
            foreach (var seat in _table.AllSeatViews)
            {
                if (seat == null || seat == _table.SelfSeat) continue;
                IOpponentVisibleInfo opponent;
                bool allowed = _request == HumanRequestKind.Gift && !_submitted && _giftCard != null
                    && _opponents.TryGetValue(seat.PlayerId, out opponent) && opponent.CurrentPhase != DefPhase.Finished;
                seat.SetSeatClickable(allowed);
            }
        }

        private void SetAllSeatsClickable(bool clickable)
        {
            if (_table != null) foreach (var seat in _table.AllSeatViews) if (seat != null) seat.SetSeatClickable(clickable);
        }

        private void ApplyExchangePreview()
        {
            ClearExchangePreview();
            if (_table == null || _table.SelfSeat == null) return;
            foreach (var choice in _exchanges)
            {
                var hand = _table.SelfSeat.HandViews.FirstOrDefault(v => v != null && v.Card == choice.HandCard);
                var face = _table.SelfSeat.FaceUpViews.FirstOrDefault(v => v != null && v.Card == choice.FaceUpCard);
                if (hand != null) hand.SetPreview(choice.FaceUpCard);
                if (face != null) face.SetPreview(choice.HandCard);
            }
            // Aperçu local uniquement : rien n'est échangé dans le Core avant SubmitExchanges.
            ReapplyCardStates();
        }

        private void ClearExchangePreview()
        {
            if (_table == null || _table.SelfSeat == null) return;
            foreach (var view in _table.SelfSeat.HandViews) if (view != null) view.ClearPreview();
            foreach (var view in _table.SelfSeat.FaceUpViews) if (view != null) view.ClearPreview();
        }
    }
}