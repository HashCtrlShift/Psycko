using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Affiche les compteurs de pioche et de pile. Aucune logique de jeu.</summary>
    public sealed class ZoneView : MonoBehaviour
    {
        [SerializeField] private Text _drawCountLabel;
        [SerializeField] private Text _pileCountLabel;
        [SerializeField] private GameObject _emptyPileMarker;

        public void Render(ZoneViewModel model)
        {
            if (model == null) return;
            if (_drawCountLabel != null) _drawCountLabel.text = model.DrawPileCount.ToString();
            if (_pileCountLabel != null) _pileCountLabel.text = model.PileCardCount.ToString();
            if (_emptyPileMarker != null) _emptyPileMarker.SetActive(model.IsPileEmpty);
        }
    }
}