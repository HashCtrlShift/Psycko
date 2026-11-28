using System.Collections.Generic;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Logic;
using Psycko.Core.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Vue passive de la pioche et de la Pile : elle ne fait que refléter son modèle.</summary>
    public sealed class ZoneView : MonoBehaviour
    {
        private const int HardMaxVisible = TableProjection.MaxVisiblePileCards;

        [Header("Compteurs")]
        [SerializeField] private Text _drawCountLabel;
        [SerializeField] private Text _pileCountLabel;
        [SerializeField] private GameObject _emptyPileMarker;

        [Header("Cartes")]
        [SerializeField] private CardView _drawCardView;

        [Header("Pile éparpillée")]
        [Tooltip("Prefab de CardView (Raycast Target décoché sur toutes ses Image).")]
        [SerializeField] private CardView _pileCardPrefab;
        [Tooltip("Conteneur centré où sont instanciées les cartes de la pile.")]
        [SerializeField] private RectTransform _pileCardsRoot;
        [Range(1, 12)]
        [SerializeField] private int _maxVisiblePileCards = 8;
        [Tooltip("Décalage horizontal max (+-) en px. Garde le coin gauche lisible.")]
        [SerializeField] private float _maxOffsetX = 14f;
        [Tooltip("Décalage vertical max (+-) en px.")]
        [SerializeField] private float _maxOffsetY = 10f;
        [Tooltip("Rotation max (+-) en degrés. Identique pour toutes les cartes.")]
        [SerializeField] private float _maxRotation = 12f;

        private readonly List<CardView> _pool = new List<CardView>();

        public void Render(ZoneViewModel model, ICardSkin skin)
        {
            if (model == null) return;
            if (_drawCountLabel != null) _drawCountLabel.text = model.DrawPileCount.ToString();
            RenderDraw(model, skin);
            RenderPile(model.PileCards, model.PileStartIndex, skin);
        }

        private void RenderDraw(ZoneViewModel model, ICardSkin skin)
        {
            if (_drawCardView == null) return;
            bool hasDraw = model.DrawPileCount > 0;
            _drawCardView.gameObject.SetActive(hasDraw);
            if (!hasDraw) return;
            _drawCardView.Bind(null, skin);
            _drawCardView.SetState(CardVisualState.FaceDown);
            _drawCardView.SetInteractable(false);
        }

        /// <param name="cards">Cartes disponibles (du bas vers le haut).</param>
        /// <param name="startIndex">Index absolu dans la Pile de cards[0].</param>
        private void RenderPile(IReadOnlyList<Card> cards, int startIndex, ICardSkin skin)
        {
            if (cards == null) return;
            int pileTotal = startIndex + cards.Count;
            if (_pileCountLabel != null) _pileCountLabel.text = pileTotal.ToString();
            if (_emptyPileMarker != null) _emptyPileMarker.SetActive(pileTotal == 0);
            if (_pileCardPrefab == null || _pileCardsRoot == null) return;

            int limit = Mathf.Clamp(_maxVisiblePileCards, 1, HardMaxVisible);
            int shown = cards.Count < limit ? cards.Count : limit;
            int firstIndex = cards.Count - shown;
            EnsurePool(shown);

            for (int i = 0; i < _pool.Count; i++)
            {
                CardView view = _pool[i];
                if (i >= shown)
                {
                    if (view.gameObject.activeSelf) view.gameObject.SetActive(false);
                    continue;
                }
                int localIndex = firstIndex + i;
                int absoluteIndex = startIndex + localIndex;
                view.gameObject.SetActive(true);
                view.Bind(cards[localIndex], skin);
                view.SetState(CardVisualState.Normal);
                view.SetInteractable(false);
                view.transform.SetSiblingIndex(i);
                ApplyPose(view.transform as RectTransform, absoluteIndex);
            }
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
            {
                CardView view = Instantiate(_pileCardPrefab, _pileCardsRoot, false);
                view.name = "PileCard_" + _pool.Count;
                _pool.Add(view);
            }
        }

        private void ApplyPose(RectTransform rect, int pileIndex)
        {
            if (rect == null) return;
            PilePoseCalculator.Pose pose = PilePoseCalculator.Calculate(
                pileIndex, _maxOffsetX, _maxOffsetY, _maxRotation);
            rect.anchoredPosition = new Vector2(pose.X, pose.Y);
            rect.localRotation = Quaternion.Euler(0f, 0f, pose.RotationZ);
        }
    }
}
