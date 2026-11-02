using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    public enum CardVisualState { Normal, Playable, Selected, FaceDown }

    /// <summary>Vue de carte : rendu + émission d'un clic, sans décision de règles.</summary>
    public sealed class CardView : MonoBehaviour
    {
        private static readonly HashSet<string> _warnedKeys = new HashSet<string>();

        [Header("Références (enfants du prefab)")]
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private Image _faceImage;
        [SerializeField] private Image _highlight;
        [SerializeField] private Image _placeholderBackground;
        [SerializeField] private Text _placeholderLabel;

        [Header("Interaction et affichage")]
        [SerializeField] private Button _button;
        [SerializeField] private float _selectedLift = 24f;
        [SerializeField] private Color _placeholderColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        private Card _card;
        private Card _previewCard;
        private bool _hasPreview;
        private ICardSkin _skin;
        private CardVisualState _state = CardVisualState.Normal;

        public Card Card { get { return _card; } }
        public CardVisualState State { get { return _state; } }
        public event Action<CardView> Clicked;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(RaiseClicked);
        }

        private void OnDestroy() { if (_button != null) _button.onClick.RemoveListener(RaiseClicked); }

        private void RaiseClicked() { if (Clicked != null) Clicked(this); }

        public void Bind(Card card, ICardSkin skin) { _card = card; _previewCard = null; _hasPreview = false; _skin = skin; Refresh(); }
        public void SetPreview(Card displayCard) { _previewCard = displayCard; _hasPreview = true; Refresh(); }
        public void ClearPreview() { _previewCard = null; _hasPreview = false; Refresh(); }
        public void SetState(CardVisualState state) { _state = state; Refresh(); }
        public void SetInteractable(bool value) { if (_button != null) _button.interactable = value; }

        public void Refresh()
        {
            if (_skin == null) return;
            Card renderedCard = _hasPreview ? _previewCard : _card;
            bool faceDown = _state == CardVisualState.FaceDown || renderedCard == null;
            Sprite sprite = faceDown ? _skin.GetBack() : _skin.GetFace(renderedCard);

            if (_faceImage != null) { _faceImage.sprite = sprite; _faceImage.enabled = sprite != null; }

            bool missing = sprite == null;
            if (_placeholderBackground != null) _placeholderBackground.enabled = missing;
            if (_placeholderLabel != null) _placeholderLabel.enabled = missing;
            if (missing && _placeholderLabel != null)
            {
                if (_placeholderBackground != null) _placeholderBackground.color = _placeholderColor;
                _placeholderLabel.text = faceDown ? "DOS MANQUANT" : CardSpriteKey.Label(renderedCard);
                if (_skin.FontSize > 0) { _placeholderLabel.resizeTextForBestFit = false; _placeholderLabel.fontSize = _skin.FontSize; }
                else _placeholderLabel.resizeTextForBestFit = true;
                if (_skin.Font != null) _placeholderLabel.font = _skin.Font;
                string key = faceDown ? "Back" : CardSpriteKey.From(renderedCard);
                if (_warnedKeys.Add(key)) Debug.LogWarning("[CardView] Sprite absent pour la clé '" + key + "' (placeholder affiché).");
            }

            if (_highlight != null) _highlight.enabled = _state == CardVisualState.Playable || _state == CardVisualState.Selected;

            // Seul le visuel est levé : la hitbox (racine + Button) reste immobile.
            if (_visualRoot != null)
            {
                Vector2 pos = _visualRoot.anchoredPosition;
                pos.y = _state == CardVisualState.Selected ? _selectedLift : 0f;
                _visualRoot.anchoredPosition = pos;
            }
        }
    }
}