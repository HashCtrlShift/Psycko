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
        [SerializeField] private Text _messageLabel;
        [SerializeField] private CountdownView _giftCountdownView;
        [SerializeField] private float _giftSeconds = 30f;
        [SerializeField] private float _exchangeSeconds = 90f;
        [SerializeField] private float _messageSeconds = 2f;

        private readonly HumanSelectionState _selection = new HumanSelectionState();
        private readonly List<ExchangeChoice> _exchanges = new List<ExchangeChoice>();
        private readonly Dictionary<int, IOpponentVisibleInfo> _opponents = new Dictionary<int, IOpponentVisibleInfo>();
        private readonly CountdownTimer _giftTimer = new CountdownTimer();

        // [ÉCHANGE] Copies locales de la main / FaceUp, permutées à chaque échange (miroir de PreGameExchange.Swap).
        private readonly List<Card> _simHand = new List<Card>();
        private readonly List<Card> _simFaceUp = new List<Card>();

        private HumanRequestKind _request;
        private IPlayerVisibleState _pending;
        private IFaceDownBlindView _blind;
        private Card _pendingExchangeHand;
        private Card _giftCard;
        private float _exchangeRemaining;
        private float _messageRemaining;
        private bool _submitted;

        private void OnEnable()
        {
            _giftTimer.Expired += OnGiftTimerExpired;
            if (_controller != null) { _controller.HumanRequestPending += OnRequest; _controller.StateChanged += OnState; }
            if (_table != null) _table.Rendered += OnTableRendered;
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_pickupButton != null) _pickupButton.onClick.AddListener(OnPickupClicked);
            if (_readyButton != null) _readyButton.onClick.AddListener(OnReadyClicked);
            _selection.Changed += OnSelectionChanged;
            SubscribeSeats(true);
            SetAllSeatsClickable(false);
            ShowMessage(string.Empty);
            RefreshButtons();
        }

        private void OnDisable()
        {
            _giftTimer.Expired -= OnGiftTimerExpired;
            _giftTimer.Stop();
            if (_giftCountdownView != null) _giftCountdownView.Hide();
            if (_controller != null) { _controller.HumanRequestPending -= OnRequest; _controller.StateChanged -= OnState; }
            if (_table != null) _table.Rendered -= OnTableRendered;
            if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_pickupButton != null) _pickupButton.onClick.RemoveListener(OnPickupClicked);
            if (_readyButton != null) _readyButton.onClick.RemoveListener(OnReadyClicked);
            _selection.Changed -= OnSelectionChanged;
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

        private void OnSelectionChanged() { ReapplyCardStates(); }

        private void OnTableRendered()
        {
            RefreshVisibleState();
            // Un re-rendu remet les vues à la réalité du Core et efface les aperçus : on les rejoue.
            if (_request == HumanRequestKind.Exchanges) ApplyExchangePreview();
            else ReapplyCardStates();
        }

        private void OnState(GameState state)
        {
            // GameplayController dépile la file dans Update : on est sur le Main Thread.
            RefreshVisibleState(); ReapplyCardStates();
        }

        private void OnRequest(HumanRequestKind kind)
        {
            // Toute nouvelle requête arrête un éventuel minuteur de don précédent.
            _giftTimer.Stop();
            if (_giftCountdownView != null) _giftCountdownView.Hide();

            _request = kind; _submitted = false;
            _pending = _controller == null ? null : _controller.VisibleState;
            _blind = _controller == null || _controller.Human == null ? null : _controller.Human.PendingBlindView;
            ShowMessage(string.Empty);
            if (kind == HumanRequestKind.NormalPlay) { _selection.Clear(); _giftCard = null; _pendingExchangeHand = null; }
            if (kind == HumanRequestKind.Gift)
            {
                _giftCard = null; _pendingExchangeHand = null; _selection.Clear();
                // La main a changé (cartes posées) : on redessine depuis la vue de la requête.
                if (_table != null && _pending != null) _table.RenderVisible(_pending);
                // Durée <= 0 : illimitée, aucun décompte ni expiration.
                _giftTimer.Start(_giftSeconds);
            }
            if (kind == HumanRequestKind.Exchanges)
            {
                _exchanges.Clear(); _pendingExchangeHand = null; _giftCard = null; _selection.Clear();
                ResetSimulation(); // [ÉCHANGE]
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
            if (_messageRemaining > 0f)
            {
                _messageRemaining -= Time.deltaTime;
                if (_messageRemaining <= 0f && _messageLabel != null) _messageLabel.text = string.Empty;
            }

            // Minuteur du don : décompte tant qu'aucun don n'a été soumis.
            if (_request == HumanRequestKind.Gift && !_submitted && _giftTimer.IsRunning)
            {
                if (_giftTimer.IsUnlimited)
                {
                    if (_giftCountdownView != null) _giftCountdownView.Hide();
                }
                else
                {
                    _giftTimer.Tick(Time.deltaTime);
                    if (_giftTimer.IsRunning)
                    {
                        if (_giftCountdownView != null) _giftCountdownView.Show(_giftTimer.Remaining, _giftTimer.Total);
                    }
                    else if (_giftCountdownView != null) _giftCountdownView.Hide();
                }
            }

            if (_request != HumanRequestKind.Exchanges || _submitted) return;
            _exchangeRemaining = Mathf.Max(0f, _exchangeRemaining - Time.deltaTime);
            if (_timerLabel != null) _timerLabel.text = Mathf.CeilToInt(_exchangeRemaining) + " s";
            if (_exchangeRemaining <= 0f) SubmitExchanges();
        }

        private void OnHandCard(Card card, CardView view)
        {
            if (card == null || view == null || _submitted) return;
            if (_request == HumanRequestKind.NormalPlay)
            {
                // Le Changed de _selection déclenche ReapplyCardStates.
                _selection.Toggle(card);
            }
            else if (_request == HumanRequestKind.Gift)
            {
                // Garde : seule une carte réellement en main peut être donnée.
                if (_pending == null || !_pending.SelfHand.Contains(card)) return;
                _giftCard = _giftCard == card ? null : card;
                UpdateSeatClickability(); ReapplyCardStates();
            }
            else if (_request == HumanRequestKind.Exchanges)
            {
                // [ÉCHANGE] La vue renvoie sa carte RÉELLE : on retrouve la carte AFFICHÉE par sa position.
                if (_table == null || _table.SelfSeat == null) return;
                Card shown = SimCardAt(_table.SelfSeat.HandViews, view, _simHand);
                if (shown == null) return;
                // Une seule carte de main levée : re-clic = baisse, autre carte = déplacement.
                _pendingExchangeHand = _pendingExchangeHand == shown ? null : shown;
                ReapplyCardStates();
            }
        }

        private void OnFaceUpCard(Card card, CardView view)
        {
            if (_request != HumanRequestKind.Exchanges || _submitted || view == null || _pendingExchangeHand == null) return;
            if (_table == null || _table.SelfSeat == null) return;

            // [ÉCHANGE] Carte FaceUp AFFICHÉE à la position de la vue cliquée (pas view.Card).
            Card shownFaceUp = SimCardAt(_table.SelfSeat.FaceUpViews, view, _simFaceUp);
            if (shownFaceUp == null) return;

            int faceUpIdx = _simFaceUp.IndexOf(shownFaceUp);
            int handIdx = _simHand.IndexOf(_pendingExchangeHand);
            if (faceUpIdx < 0 || handIdx < 0) return;

            // Choix enregistré en cartes réelles à cet instant : le Core les rejoue séquentiellement.
            _exchanges.Add(new ExchangeChoice(_pendingExchangeHand, shownFaceUp));

            // Permutation locale, identique à PreGameExchange.Swap.
            _simHand[handIdx] = shownFaceUp;
            _simFaceUp[faceUpIdx] = _pendingExchangeHand;

            _pendingExchangeHand = null;
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
            // Don manuel : le minuteur est arrêté, le Fallback ne se déclenchera jamais.
            _giftTimer.Stop();
            if (_giftCountdownView != null) _giftCountdownView.Hide();
            _controller.Human.SubmitGift(new GiftResolutionChoice(_giftCard, playerId));
            HideActions(); SetAllSeatsClickable(false);
        }

        /// <summary>Expiration du don : l'agent applique son Fallback (décision hors Présentation).</summary>
        private void OnGiftTimerExpired()
        {
            if (_request != HumanRequestKind.Gift || _submitted) return;
            _submitted = true;
            if (_controller != null && _controller.Human != null) _controller.Human.ExpirePending();
            _giftTimer.Stop();
            if (_giftCountdownView != null) _giftCountdownView.Hide();
            HideActions(); SetAllSeatsClickable(false);
        }

        private void OnPlayClicked()
        {
            if (_request != HumanRequestKind.NormalPlay || _submitted || _selection.IsEmpty
                || _controller == null || _controller.Human == null || _pending == null) return;

            if (!EverySelectedCardPlayable())
            {
                // Rejet par le Core (ICardPlayabilityChecker) : message court, sélection conservée.
                ShowMessage("Coup refusé");
                return;
            }

            Play play;
            try
            {
                play = Play.Create(_pending.SelfPlayerId, _selection.Cards);
            }
            catch (ArgumentException)
            {
                // Rejet structurel du Core (ex. cartes de rangs différents) : sélection conservée.
                ShowMessage("Sélection invalide");
                return;
            }

            _submitted = true;
            ShowMessage(string.Empty);
            _controller.Human.SubmitPlay(play);
            _selection.Clear(); // déclenche ReapplyCardStates via Changed
            HideActions();
        }

        private void OnPickupClicked()
        {
            if (_submitted || _controller == null || _controller.Human == null) return;
            // GameRunner passe par ProposeNormalPlay : null = ramassage volontaire.
            if (_request == HumanRequestKind.NormalPlay) { _submitted = true; _controller.Human.SubmitNoPlay(); }
            else if (_request == HumanRequestKind.Pickup) { _submitted = true; _controller.Human.SubmitPickup(true); }
            else return;
            ShowMessage(string.Empty);
            HideActions();
        }

        private void OnReadyClicked() { if (_request == HumanRequestKind.Exchanges) SubmitExchanges(); }

        private void SubmitExchanges()
        {
            if (_request != HumanRequestKind.Exchanges || _submitted || _controller == null || _controller.Human == null) return;
            _submitted = true; _pendingExchangeHand = null; ClearExchangePreview();
            _controller.Human.SubmitExchanges(_exchanges.ToList().AsReadOnly());
            _request = HumanRequestKind.None; HideActions();
        }

        /// <summary>
        /// Point unique de projection : applique à chaque CardView l'état calculé par CardStateProjector.
        /// Appelé après tout changement de sélection, de requête, de rendu ou d'état.
        /// </summary>
        private void ReapplyCardStates()
        {
            if (_table == null) return;
            var self = _table.SelfSeat;
            if (self != null)
            {
                var hand = self.HandViews;
                if (_request == HumanRequestKind.NormalPlay)
                    _selection.Prune(hand.Where(v => v != null && v.Card != null).Select(v => v.Card), IsPlayable);

                bool play = _request == HumanRequestKind.NormalPlay && !_submitted;
                bool gift = _request == HumanRequestKind.Gift && !_submitted;
                bool exchange = _request == HumanRequestKind.Exchanges && !_submitted;

                // [ÉCHANGE] En échange, la carte affichée à l'indice i est la carte simulée, pas view.Card.
                var cards = new List<Card>(hand.Count);
                for (int i = 0; i < hand.Count; i++)
                    cards.Add(hand[i] == null ? null : DisplayedHandCard(i, hand[i]));

                Func<Card, bool> isSelected = c =>
                    (play && _selection.Contains(c))
                    || (gift && c == _giftCard)
                    || (exchange && c == _pendingExchangeHand);

                // Surbrillance Playable uniquement en pose normale ; jamais en don ni en échange.
                CardProjection[] states = CardStateProjector.Project(cards, isSelected, IsPlayable, play);

                bool handInteractive = _request == HumanRequestKind.NormalPlay
                    || _request == HumanRequestKind.Gift
                    || _request == HumanRequestKind.Exchanges;

                for (int i = 0; i < hand.Count; i++)
                {
                    var view = hand[i];
                    if (view == null || view.Card == null) continue;
                    view.SetState(ToVisual(states[i]));
                    view.SetInteractable(handInteractive);
                }

                foreach (var view in self.FaceUpViews)
                {
                    if (view == null) continue;
                    view.SetState(CardVisualState.Normal);
                    view.SetInteractable(_request == HumanRequestKind.Exchanges && !_submitted);
                }
                foreach (var view in self.FaceDownViews) if (view != null) view.SetInteractable(_request == HumanRequestKind.FaceDownIndex);
            }
            RefreshButtons();
        }

        private static CardVisualState ToVisual(CardProjection projection)
        {
            switch (projection)
            {
                case CardProjection.Selected: return CardVisualState.Selected;
                case CardProjection.Playable: return CardVisualState.Playable;
                default: return CardVisualState.Normal;
            }
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
            if ((_request != HumanRequestKind.Gift || _submitted) && _giftCountdownView != null) _giftCountdownView.Hide();
            if (_timerLabel != null) _timerLabel.gameObject.SetActive(exchanges);
            if (!exchanges && _timerLabel != null) _timerLabel.text = string.Empty;
        }

        private static void SetButton(Button button, bool visible, bool interactable)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible); button.interactable = interactable;
        }

        private void ShowMessage(string text)
        {
            _messageRemaining = string.IsNullOrEmpty(text) ? 0f : Mathf.Max(0.1f, _messageSeconds);
            if (_messageLabel != null) _messageLabel.text = text ?? string.Empty;
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

        // ── [ÉCHANGE] Simulation locale et aperçu ────────────────────────

        /// <summary>Carte simulée affichée à la position de la vue cliquée, ou null si hors limites.</summary>
        private static Card SimCardAt(IReadOnlyList<CardView> views, CardView clicked, List<Card> sim)
        {
            for (int i = 0; i < views.Count && i < sim.Count; i++)
                if (views[i] == clicked) return sim[i];
            return null;
        }

        /// <summary>Recopie la main et la FaceUp réelles de la requête : point de départ de la simulation.</summary>
        private void ResetSimulation()
        {
            _simHand.Clear(); _simFaceUp.Clear();
            if (_pending == null) return;
            _simHand.AddRange(_pending.SelfHand);
            _simFaceUp.AddRange(_pending.SelfFaceUp);
        }

        /// <summary>Carte affichée à l'indice i de la main : simulée pendant l'échange, réelle sinon.</summary>
        private Card DisplayedHandCard(int index, CardView view)
        {
            if (_request == HumanRequestKind.Exchanges && !_submitted && index < _simHand.Count) return _simHand[index];
            return view.Card;
        }

        /// <summary>
        /// Projette les listes simulées sur les vues, par position. Les vues restent liées aux cartes
        /// réelles du Core ; seul l'aperçu change. Aucun échange n'est appliqué au Core avant SubmitExchanges.
        /// </summary>
        private void ApplyExchangePreview()
        {
            ClearExchangePreview();
            if (_table == null || _table.SelfSeat == null) return;

            var hand = _table.SelfSeat.HandViews;
            for (int i = 0; i < hand.Count && i < _simHand.Count; i++)
                if (hand[i] != null && hand[i].Card != _simHand[i]) hand[i].SetPreview(_simHand[i]);

            var faceUp = _table.SelfSeat.FaceUpViews;
            for (int i = 0; i < faceUp.Count && i < _simFaceUp.Count; i++)
                if (faceUp[i] != null && faceUp[i].Card != _simFaceUp[i]) faceUp[i].SetPreview(_simFaceUp[i]);

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