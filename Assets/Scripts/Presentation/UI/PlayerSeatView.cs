using UnityEngine;
using UnityEngine.UI;

namespace Psycko.Presentation.UI
{
    /// <summary>Siège : nom, compteurs main / FaceUp / FaceDown, indicateur actif.</summary>
    public sealed class PlayerSeatView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _handCountLabel;
        [SerializeField] private Text _faceUpCountLabel;
        [SerializeField] private Text _faceDownCountLabel;
        [SerializeField] private GameObject _activeIndicator;

        public void RenderOpponent(OpponentSeatViewModel model)
        {
            if (model == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Set(_nameLabel, model.Name);
            Set(_handCountLabel, model.HandCount.ToString());
            Set(_faceUpCountLabel, model.FaceUpCount.ToString());
            Set(_faceDownCountLabel, model.FaceDownCount.ToString());
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
        }

        /// <summary>Siège humain : compteurs seulement ici, les cartes seront rendues en T53.</summary>
        public void RenderSelf(SelfSeatViewModel model, string displayName)
        {
            if (model == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Set(_nameLabel, displayName);
            Set(_handCountLabel, model.Hand.Count.ToString());
            Set(_faceUpCountLabel, model.FaceUp.Count.ToString());
            Set(_faceDownCountLabel, model.FaceDown.Count.ToString());
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
        }

        private static void Set(Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}