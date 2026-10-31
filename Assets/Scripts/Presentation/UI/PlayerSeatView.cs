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
        [SerializeField] private CardView _cardViewPrefab;
        [SerializeField] private RectTransform _handContainer;

        private readonly System.Collections.Generic.List<CardView> _handViews
            = new System.Collections.Generic.List<CardView>();

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

        /// <summary>Siège humain : compteurs et main (CardView).</summary>
        public void RenderSelf(SelfSeatViewModel model, string displayName, Psycko.Presentation.Interfaces.ICardSkin skin)
        {
            if (model == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Set(_nameLabel, displayName);
            Set(_handCountLabel, model.Hand.Count.ToString());
            Set(_faceUpCountLabel, model.FaceUp.Count.ToString());
            Set(_faceDownCountLabel, model.FaceDown.Count.ToString());
            if (_activeIndicator != null) _activeIndicator.SetActive(model.IsActive);
            RenderHand(model.Hand, skin);
        }

        private static void Set(Text label, string value)
        {
            if (label != null) label.text = value;
        }
        public void RenderHand(System.Collections.Generic.IReadOnlyList<Psycko.Core.Domain.Card> cards,
                       Psycko.Presentation.Interfaces.ICardSkin skin)
        {
            if (_cardViewPrefab == null || _handContainer == null)
        {
            Debug.LogWarning("[Seat] RenderHand annulé : prefab=" + (_cardViewPrefab != null)
                + ", container=" + (_handContainer != null));
            return;
        }
        Debug.Log("[Seat] RenderHand : " + cards.Count + " cartes");

            // Ajuste le nombre de vues au nombre de cartes
            while (_handViews.Count < cards.Count)
                _handViews.Add(Instantiate(_cardViewPrefab, _handContainer));
            while (_handViews.Count > cards.Count)
            {
                Destroy(_handViews[_handViews.Count - 1].gameObject);
                _handViews.RemoveAt(_handViews.Count - 1);
            }

            for (int i = 0; i < cards.Count; i++)
                _handViews[i].Bind(cards[i], skin);
        }
    }
}