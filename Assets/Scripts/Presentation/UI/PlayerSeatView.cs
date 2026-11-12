using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Models;
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
        [SerializeField] private Text _rankBanner;                 // T55-9
        [SerializeField] private GameObject _activeIndicator;
        [SerializeField] private CardView _cardViewPrefab;
        [SerializeField] private RectTransform _handContainer;
        [SerializeField] private RectTransform _faceUpContainer;
        [SerializeField] private RectTransform _faceDownContainer;
        [SerializeField] private Button _seatButton;

        [Header("Dos adverses (T55-8)")]
        [SerializeField] private bool _opponentBacksVertical;      // true : cartes alignées sur l'axe Y local du conteneur
        [SerializeField] private float _cardWidth = 140f;          // taille du prefab CardView
        [SerializeField] private float _cardHeight = 196f;
        [SerializeField] private float _handBackScale = 0.75f;     // échelle des dos de MAIN uniquement
        [SerializeField] private float _handDefaultSpacing = 40f;
        [SerializeField] private float _faceDownDefaultSpacing = 150f; // pas des FaceDown ET des FaceUp adverses
        [SerializeField] private float _handZoneLength = 480f;     // longueur utile de la ligne de main
        [SerializeField] private int _handMaxVisibleBacks = 8;     // plafond de dos de MAIN affichés
        [SerializeField] private float _faceDownZoneLength = 480f; // longueur utile de la ligne FaceDown / FaceUp
        [SerializeField] private float _minSpacing = HandSpacing.DefaultMinSpacing;
        [SerializeField] private float _tableRotationZ = 0f;       // 90 (ou -90) sur SeatLeft/SeatRight : FaceUp/FaceDown couchées

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

        /// <summary>
        /// Masque le siège (null) ou affiche un adversaire. Cartes FaceUp publiques rendues ;
        /// main et FaceDown : dos uniquement, à partir des compteurs (T55-8).
        /// FaceUp et FaceDown partagent les mêmes emplacements : la FaceUp i recouvre le dos i.
        /// Le label de main n'apparaît que si la main dépasse le nombre de dos affichables.
        /// </summary>
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
            UpdateRankBanner(model.FinishRank); // T55-9

            // Par défaut (skin absent), pas de dos affichés : le label ne doit pas s'afficher seul.
            int visibleHandBacks = 0;

            if (skin != null)
            {
                // Même nombre d'emplacements pour FaceUp et FaceDown : centrage identique.
                int tableSlots = Math.Max(model.FaceUpCount, model.FaceDownCount);

                // T55-8 : uniquement des compteurs, jamais de Card cachée.
                visibleHandBacks = RenderBackRow(_handViews, _handContainer, model.HandCount, skin, OnHandClicked,
                    _handBackScale, _handDefaultSpacing, _handZoneLength, _handMaxVisibleBacks, 0f, model.HandCount);
                RenderBackRow(_faceDownViews, _faceDownContainer, model.FaceDownCount, skin, OnFaceDownClicked,
                    1f, _faceDownDefaultSpacing, _faceDownZoneLength, int.MaxValue, _tableRotationZ, tableSlots);
                RenderFaceRow(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked,
                    _faceDownDefaultSpacing, _faceDownZoneLength, _tableRotationZ, tableSlots);
            }

            UpdateHandCountLabel(model.HandCount, visibleHandBacks);
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
            UpdateRankBanner(model.FinishRank); // T55-9
            // Soi-même : toutes les cartes sont visibles, le label reste affiché comme avant.
            if (_handCountLabel != null) _handCountLabel.gameObject.SetActive(true);
            RenderCards(_handViews, _handContainer, model.Hand, skin, OnHandClicked);
            RenderCards(_faceUpViews, _faceUpContainer, model.FaceUp, skin, OnFaceUpClicked);
            // Jamais de Card liée aux FaceDown : uniquement le nombre et l'index aveugle.
            RenderFaceDownSlots(model.FaceDownCount, skin); // T55-5
        }

        /// <summary>
        /// T55-9 : « #1 », « #2 », « #3 », « Psycko » (rang 4). Rang absent : bannière masquée.
        /// Affichage pur : le rang (y compris celui du Psycko) est calculé par le Core.
        /// </summary>
        private void UpdateRankBanner(int? finishRank)
        {
            if (_rankBanner == null) return;

            string text = FormatRank(finishRank);
            _rankBanner.text = text;
            _rankBanner.gameObject.SetActive(text.Length > 0);
        }

        /// <summary>Mapping d'affichage pur rang -> texte (aucun calcul de règle).</summary>
        internal static string FormatRank(int? finishRank)
        {
            switch (finishRank)
            {
                case 1: return "#1";
                case 2: return "#2";
                case 3: return "#3";
                case 4: return "Psycko";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Affiche le label de main uniquement quand il reste des cartes non représentées
        /// (compte réel supérieur au nombre de dos visibles).
        /// </summary>
        private void UpdateHandCountLabel(int realCount, int visibleBacks)
        {
            if (_handCountLabel == null) return;
            _handCountLabel.gameObject.SetActive(realCount > visibleBacks);
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

        /// <summary>
        /// T55-8 : rangée de dos centrés et positionnés par code (aucun LayoutGroup sur ce conteneur).
        /// slotCount = nombre d'emplacements servant au centrage (aligne FaceUp et FaceDown).
        /// Retourne le nombre de dos réellement affichés.
        /// </summary>
        private int RenderBackRow(List<CardView> views, RectTransform container, int count, ICardSkin skin,
            Action<CardView> handler, float scale, float defaultSpacing, float zoneLength, int maxVisible,
            float rotationZ, int slotCount)
        {
            if (_cardViewPrefab == null || container == null) return 0;

            float cardW = _cardWidth * scale;
            float cardH = _cardHeight * scale;

            HandLayoutResult layout = HandSpacing.Compute(
                Math.Max(count, slotCount), defaultSpacing, zoneLength,
                RowLength(cardW, cardH, rotationZ), _minSpacing, maxVisible);
            int visible = Math.Min(count, layout.VisibleCount);
            EnsureCount(views, container, visible, handler);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                view.Bind(null, skin);
                view.SetState(CardVisualState.FaceDown);
                view.SetInteractable(false);
                PlaceInRow((RectTransform)view.transform, i, layout.Spacing, layout.VisibleCount,
                    cardW, cardH, rotationZ);
            }

            return visible;
        }

        /// <summary>
        /// FaceUp adverses placées par code sur les mêmes emplacements que les FaceDown
        /// (la carte i recouvre exactement le dos i).
        /// </summary>
        private void RenderFaceRow(List<CardView> views, RectTransform container, IReadOnlyList<Card> cards,
            ICardSkin skin, Action<CardView> handler, float defaultSpacing, float zoneLength,
            float rotationZ, int slotCount)
        {
            int count = cards == null ? 0 : cards.Count;
            if (_cardViewPrefab == null || container == null)
            {
                if (count > 0) Debug.LogWarning("[PlayerSeatView] Prefab ou conteneur de cartes non assigné.");
                return;
            }

            HandLayoutResult layout = HandSpacing.Compute(
                Math.Max(count, slotCount), defaultSpacing, zoneLength,
                RowLength(_cardWidth, _cardHeight, rotationZ), _minSpacing, int.MaxValue);
            int visible = Math.Min(count, layout.VisibleCount);
            EnsureCount(views, container, visible, handler);

            for (int i = 0; i < views.Count; i++)
            {
                CardView view = views[i];
                view.Bind(cards[i], skin);
                view.SetState(CardVisualState.Normal);
                view.SetInteractable(false);
                PlaceInRow((RectTransform)view.transform, i, layout.Spacing, layout.VisibleCount,
                    _cardWidth, _cardHeight, rotationZ);
            }
        }

        /// <summary>Extension d'une carte le long de l'axe de la rangée, rotation prise en compte.</summary>
        private float RowLength(float cardW, float cardH, float rotationZ)
        {
            bool swapped = Mathf.Abs(Mathf.Repeat(rotationZ, 180f) - 90f) < 1f;
            float alongX = swapped ? cardH : cardW;
            float alongY = swapped ? cardW : cardH;
            return _opponentBacksVertical ? alongY : alongX;
        }

        private void PlaceInRow(RectTransform rt, int index, float step, int slotCount,
            float cardW, float cardH, float rotationZ)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(cardW, cardH);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            float total = slotCount <= 1 ? 0f : step * (slotCount - 1);
            float offset = -total * 0.5f + step * index;
            rt.anchoredPosition = _opponentBacksVertical ? new Vector2(0f, -offset) : new Vector2(offset, 0f);
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