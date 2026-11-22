using System.Collections.Generic;
using Psycko.Presentation.Interfaces;
using Psycko.Core.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Pioche et pile : compteurs + dos/face + pile éparpillée. Aucune logique de jeu.</summary>
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

        /// <summary>
        /// Affiche la Pile telle qu'elle est juste après une pose (aperçu T65).
        /// La pioche n'est pas touchée : seule la pile et son compteur sont redessinés.
        /// </summary>
        public void RenderPlacedPile(IReadOnlyList<Card> fullPile, ICardSkin skin)
        {
            if (fullPile == null) return;
            RenderPile(fullPile, 0, skin);
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

        /// <param name="cards">Cartes disponibles (tout ou partie de la Pile), du bas vers le haut.</param>
        /// <param name="startIndex">Index absolu dans la Pile de cards[0].</param>
        private void RenderPile(IReadOnlyList<Card> cards, int startIndex, ICardSkin skin)
        {
            int pileTotal = startIndex + cards.Count;
            if (_pileCountLabel != null) _pileCountLabel.text = pileTotal.ToString();
            if (_emptyPileMarker != null) _emptyPileMarker.SetActive(pileTotal == 0);

            if (_pileCardPrefab == null || _pileCardsRoot == null) return;

            int limit = Mathf.Clamp(_maxVisiblePileCards, 1, HardMaxVisible);
            int available = cards.Count;
            int shown = available < limit ? available : limit;
            int firstIndex = available - shown;

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
                // Ordre d'affichage : index croissant = rendu au-dessus.
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

        /// <summary>
        /// Pose déterministe par index absolu : décalage X/Y et rotation dans des plages
        /// fixes, indépendantes de la position dans la pile. Une carte posée ne rebouge jamais.
        /// </summary>
        private void ApplyPose(RectTransform rect, int pileIndex)
        {
            if (rect == null) return;
            float x = (Unit(Hash(pileIndex, 1)) * 2f - 1f) * _maxOffsetX;
            float y = (Unit(Hash(pileIndex, 2)) * 2f - 1f) * _maxOffsetY;
            float rot = (Unit(Hash(pileIndex, 3)) * 2f - 1f) * _maxRotation;

            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>Hash entier déterministe (mélange multiplicatif), sans état ni Random.</summary>
        private static uint Hash(int index, int salt)
        {
            unchecked
            {
                uint h = (uint)index * 2654435761u + (uint)salt * 40503u;
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>Hash -> valeur dans [0, 1].</summary>
        private static float Unit(uint h)
        {
            return (h & 0xFFFFu) / 65535f;
        }
    }
}