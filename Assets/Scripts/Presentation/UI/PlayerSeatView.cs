using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Vue d'un siège. Les cartes adverses privées ne sont jamais demandées ni rendues.</summary>
    public sealed class PlayerSeatView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _handCountLabel;
        [SerializeField] private Text _faceUpCountLabel;
        [SerializeField] private Text _faceDownCountLabel;
        [SerializeField] private GameObject _activeIndicator;
        [SerializeField] private CardView _cardViewPrefab;
        [SerializeField] private RectTransform _handContainer;
        [SerializeField] private RectTransform _faceUpContainer;
        [SerializeField] private RectTransform _faceDownContainer;
        [SerializeField] private Button _seatButton;

        private readonly List<CardView> _handViews = new List<CardView>();
        private readonly List<CardView> _faceUpViews = new List<CardView>();
        private readonly List<CardView> _faceDownViews = new List<CardView>();
        private int _playerId = -1;

        public event Action<Card, CardView> HandCardClicked;
        public event Action<Card, CardView> FaceUpCardClicked;
        public event Action<int> FaceDownIndexClicked;
        public event Action<int> SeatClicked;

        public IReadOnlyList<CardView> HandViews { get { return _handViews.AsReadOnly(); } }
        public IReadOnlyList<CardView> FaceUpViews { get { return _faceUpViews.AsReadOnly(); } }
        public IReadOnlyList<CardView> FaceDownViews { get { return _faceDownViews.AsReadOnly(); } }
        public int PlayerId { get { return _playerId; } }

        private void Awake()
        {
            if (_seatButton == null) _seatButton = GetComponent<Button>();
            if (_seatButton != null) _seatButton.onClick.AddListener(RaiseSeatClicked);
        }

        private void OnDestroy()
        {
            if (_seatButton != null) _seatButton.onClick.RemoveListener(RaiseSeatClicked);
            Unsubscribe(_handViews, OnHandClicked);
            Unsubscribe(_faceUpViews, OnFaceUpClicked);
            Unsubscribe(_faceDownViews, OnFaceDownClicked);
        }

        private void RaiseSeatClicked() { if (SeatClicked != null && _playerId >= 0) SeatClicked(_playerId); }

        public void SetSeatClickable(bool clickable)
        {
            if (_seatButton != null) _seatButton.interactable = clickable;
            else if (clickable) Debug.LogWarning("[PlayerSeatView] Assigner un Button au siège pour activer les clics.");
        }

        /// <summary>Masque le siège (null) ou affiche un adversaire. Seules les cartes FaceUp publiques sont rendues.</summary>
        public void RenderOpponent(OpponentSeatViewModel model, ICardSkin skin)
        {
            if (model == null)
            {
                gameObject.SetActive(false);
                _playerId = -1;
                return;
            }

            gameObject.SetActive(true);
            _playerId = model.PlayerId;
            Set(_nameLabel, model.Name);
            Set(_handCountLabel, model.HandCount.ToString());
            Set(_faceUpCountLabel, model.FaceUpCount.ToString());
            Set(_faceDownCountLabel, model.FaceDownCount.ToString());
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);

            if (skin != null)
            {
                RenderCards(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked);
                for (int i = 0; i < _faceUpViews.Count; i++) _faceUpViews[i].SetInteractable(false);
            }
        }

        public void RenderSelf(SelfSeatViewModel model, string displayName, ICardSkin skin)
        {
            if (model == null) { gameObject.SetActive(false); _playerId = -1; return; }
            gameObject.SetActive(true);
            _playerId = model.PlayerId;
            Set(_nameLabel, displayName);
            Set(_handCountLabel, model.Hand.Count.ToString());
            Set(_faceUpCountLabel, model.FaceUp.Count.ToString());
            Set(_faceDownCountLabel, model.FaceDownCount.ToString()); // T55-5
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
            RenderCards(_handViews, _handContainer, model.Hand, skin, OnHandClicked);
            RenderCards(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked);
            // Jamais de Card liée aux FaceDown : uniquement le nombre et l'index aveugle.
            RenderFaceDownSlots(model.FaceDownCount, skin); // T55-5
        }

        private void RenderFaceDownSlots(int count, ICardSkin skin)
        {
            EnsureCount(_faceDownViews, _faceDownContainer, count, OnFaceDownClicked);
            for (int i = 0; i < _faceDownViews.Count; i++)
            {
                _faceDownViews[i].Bind(null, skin);
                _faceDownViews[i].SetState(CardVisualState.FaceDown);
            }
        }

        private void RenderCards(List<CardView> views, RectTransform container,
            IReadOnlyList<Card> cards, ICardSkin skin, Action<CardView> handler)
        {
            int count = cards == null ? 0 : cards.Count;
            if (_cardViewPrefab == null || container == null)
            {
                if (count > 0) Debug.LogWarning("[PlayerSeatView] Prefab ou conteneur de cartes non assigné.");
                return;
            }
            EnsureCount(views, container, count, handler);
            for (int i = 0; i < count; i++)
            {
                views[i].Bind(cards[i], skin);
                views[i].SetState(CardVisualState.Normal);
            }
        }

        private void EnsureCount(List<CardView> views, RectTransform container, int count, Action<CardView> handler)
        {
            if (_cardViewPrefab == null || container == null) return;
            while (views.Count < count)
            {
                var view = Instantiate(_cardViewPrefab, container);
                views.Add(view);
                view.Clicked += handler;
            }
            while (views.Count > count)
            {
                int last = views.Count - 1;
                views[last].Clicked -= handler;
                Destroy(views[last].gameObject);
                views.RemoveAt(last);
            }
        }

        private static void Unsubscribe(List<CardView> views, Action<CardView> handler)
        {
            foreach (var view in views) if (view != null) view.Clicked -= handler;
        }

        private void OnHandClicked(CardView view) { if (HandCardClicked != null) HandCardClicked(view.Card, view); }
        private void OnFaceUpClicked(CardView view) { if (FaceUpCardClicked != null) FaceUpCardClicked(view.Card, view); }
        private void OnFaceDownClicked(CardView view)
        {
            int index = _faceDownViews.IndexOf(view);
            if (index >= 0 && FaceDownIndexClicked != null) FaceDownIndexClicked(index);
        }

        private static void Set(Text label, string value) { if (label != null) label.text = value; }
    }
}