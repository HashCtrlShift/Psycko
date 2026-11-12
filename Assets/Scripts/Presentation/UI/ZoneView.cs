using System.Collections.Generic;
using Psycko.Presentation.Interfaces;
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
        [Tooltip("Rayon max (px) du disque d'éparpillement.")]
        [SerializeField] private float _scatterRadius = 16f;
        [Tooltip("Rotation max (+-) des cartes sous le dessus. 180 = 360 degrés complets.")]
        [SerializeField] private float _belowMaxRotation = 180f;
        [Tooltip("Rotation max (+-) de la carte du dessus (lisibilité).")]
        [SerializeField] private float _topMaxRotation = 15f;

        private readonly List<CardView> _pool = new List<CardView>();

        public void Render(ZoneViewModel model, ICardSkin skin)
        {
            if (model == null) return;
            if (_drawCountLabel != null) _drawCountLabel.text = model.DrawPileCount.ToString();
            if (_pileCountLabel != null) _pileCountLabel.text = model.PileCardCount.ToString();
            if (_emptyPileMarker != null) _emptyPileMarker.SetActive(model.IsPileEmpty);

            if (skin == null) return;

            RenderDraw(model, skin);
            RenderPile(model, skin);
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

        private void RenderPile(ZoneViewModel model, ICardSkin skin)
        {
            if (_pileCardPrefab == null || _pileCardsRoot == null) return;

            int limit = Mathf.Clamp(_maxVisiblePileCards, 1, HardMaxVisible);
            int available = model.PileCards.Count;
            int shown = available < limit ? available : limit;
            int firstModelIndex = available - shown;

            EnsurePool(shown);

            for (int i = 0; i < _pool.Count; i++)
            {
                CardView view = _pool[i];
                if (i >= shown)
                {
                    if (view.gameObject.activeSelf) view.gameObject.SetActive(false);
                    continue;
                }

                int modelIndex = firstModelIndex + i;
                int absoluteIndex = model.PileStartIndex + modelIndex;
                bool isTop = modelIndex == available - 1;

                view.gameObject.SetActive(true);
                view.Bind(model.PileCards[modelIndex], skin);
                view.SetState(CardVisualState.Normal);
                view.SetInteractable(false);
                // Ordre d'affichage : index croissant = rendu au-dessus.
                view.transform.SetSiblingIndex(i);
                ApplyScatter(view.transform as RectTransform, absoluteIndex,
                    isTop ? _topMaxRotation : _belowMaxRotation);
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
        /// Éparpillement déterministe par index absolu : tirage uniforme dans un disque
        /// (rayon = sqrt) pour remplir la surface, pas former un anneau.
        /// Rotation Z indépendante dans [-maxRotation, +maxRotation].
        /// </summary>
        private void ApplyScatter(RectTransform rect, int pileIndex, float maxRotation)
        {
            if (rect == null) return;
            float angle = Unit(Hash(pileIndex, 1)) * (Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Unit(Hash(pileIndex, 2))) * _scatterRadius;
            float rot = (Unit(Hash(pileIndex, 3)) * 2f - 1f) * maxRotation;

            rect.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
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