using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Presentation.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    public enum CardVisualState { Normal, Playable, Selected, FaceDown }

    /// <summary>Affiche une carte. Ne décide de rien.</summary>
    public sealed class CardView : MonoBehaviour
    {
        private static readonly HashSet<string> _warnedKeys = new HashSet<string>();

        [Header("Références (enfants du prefab)")]
        [SerializeField] private RectTransform _visualRoot;   // enfant déplacé pour l'état Selected
        [SerializeField] private Image _faceImage;
        [SerializeField] private Image _highlight;            // contour, désactivé par défaut
        [SerializeField] private Image _placeholderBackground;
        [SerializeField] private Text _placeholderLabel;

        [Header("Réglages visuels")]
        [SerializeField] private float _selectedLift = 24f;   // pixels, appliqué à _visualRoot seulement
        [SerializeField] private Color _placeholderColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        private Card _card;
        private ICardSkin _skin;
        private CardVisualState _state = CardVisualState.Normal;

        public Card Card { get { return _card; } }
        public CardVisualState State { get { return _state; } }

        public void Bind(Card card, ICardSkin skin)
        {
            _card = card;
            _skin = skin;
            Refresh();
        }

        public void SetState(CardVisualState state)
        {
            _state = state;
            Refresh();
        }

        public void Refresh()
        {
            if (_skin == null) return;

            bool faceDown = _state == CardVisualState.FaceDown || _card == null;
            Sprite sprite = faceDown ? _skin.GetBack() : _skin.GetFace(_card);

            _faceImage.sprite = sprite;
            _faceImage.enabled = sprite != null;

            bool missing = sprite == null;
            _placeholderBackground.enabled = missing;
            _placeholderLabel.enabled = missing;

            if (missing)
            {
                string label = faceDown ? "DOS MANQUANT" : CardSpriteKey.Label(_card);
                _placeholderBackground.color = _placeholderColor;
                _placeholderLabel.text = label;
                if (_skin.FontSize > 0)
                {
                    _placeholderLabel.resizeTextForBestFit = false;
                    _placeholderLabel.fontSize = _skin.FontSize;
                }
                else
                {
                    _placeholderLabel.resizeTextForBestFit = true;
                }
                if (_skin.Font != null) _placeholderLabel.font = _skin.Font;
                WarnOnce(faceDown ? "Back" : CardSpriteKey.From(_card));
            }

            _highlight.enabled = _state == CardVisualState.Playable
                              || _state == CardVisualState.Selected;

            // Lift sur l'enfant uniquement : la racine (hitbox) ne bouge jamais.
            Vector2 pos = _visualRoot.anchoredPosition;
            pos.y = _state == CardVisualState.Selected ? _selectedLift : 0f;
            _visualRoot.anchoredPosition = pos;
        }

        private void WarnOnce(string key)
        {
            if (_warnedKeys.Add(key))
                Debug.LogWarning("[CardView] Sprite absent pour la clé '" + key + "' (placeholder affiché).");
        }
    }
}