using Psycko.Presentation.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Pioche et pile : compteurs + dos/face. Aucune logique de jeu.</summary>
    public sealed class ZoneView : MonoBehaviour
    {
        [Header("Compteurs")]
        [SerializeField] private Text _drawCountLabel;
        [SerializeField] private Text _pileCountLabel;
        [SerializeField] private GameObject _emptyPileMarker;

        [Header("Cartes")]
        [SerializeField] private CardView _drawCardView;
        [SerializeField] private CardView _pileCardView;

        public void Render(ZoneViewModel model, ICardSkin skin)
        {
            if (model == null) return;
            if (_drawCountLabel != null) _drawCountLabel.text = model.DrawPileCount.ToString();
            if (_pileCountLabel != null) _pileCountLabel.text = model.PileCardCount.ToString();
            if (_emptyPileMarker != null) _emptyPileMarker.SetActive(model.IsPileEmpty);

            if (skin == null) return;

            if (_drawCardView != null)
            {
                bool hasDraw = model.DrawPileCount > 0;
                _drawCardView.gameObject.SetActive(hasDraw);
                if (hasDraw)
                {
                    _drawCardView.Bind(null, skin);
                    _drawCardView.SetState(CardVisualState.FaceDown);
                    _drawCardView.SetInteractable(false);
                }
            }

            if (_pileCardView != null)
            {
                bool hasPile = model.TopPileCard != null;
                _pileCardView.gameObject.SetActive(hasPile);
                if (hasPile)
                {
                    _pileCardView.Bind(model.TopPileCard, skin);
                    _pileCardView.SetState(CardVisualState.Normal);
                    _pileCardView.SetInteractable(false);
                }
            }
        }
    }
}