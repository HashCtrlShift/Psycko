using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Domain.Log;
using Psycko.Presentation.Models;
using Psycko.Presentation.Controllers;
using Psycko.Presentation.Interfaces;
using UnityEngine;

namespace Psycko.Presentation.UI
{
    /// <summary>
    /// Vue de table en paysage. S'abonne à GameplayController.StateChanged et CardsPlaced,
    /// construit la vue du joueur humain, projette, puis pousse vers les sièges et la zone.
    /// RenderVisible permet aussi de dessiner sans GameState (phase d'échange pré-partie).
    /// </summary>
    public sealed class GameTableView : MonoBehaviour
    {
        [SerializeField] private GameplayController _controller;
        [SerializeField] private PlayerSeatView _selfSeat;
        [SerializeField] private PlayerSeatView _leftSeat;
        [SerializeField] private PlayerSeatView _topSeat;
        [SerializeField] private PlayerSeatView _rightSeat;
        [SerializeField] private ZoneView _zone;
        [SerializeField] private string _selfDisplayName = "Vous";
        [SerializeField] private Psycko.Presentation.Skins.SkinRegistry _skinRegistry;

        public event System.Action Rendered;
        public PlayerSeatView SelfSeat { get { return _selfSeat; } }
        public PlayerSeatView[] AllSeatViews { get { return new[] { _selfSeat, _leftSeat, _topSeat, _rightSeat }; } }

        private void OnEnable()
        {
            if (_controller == null) return;
            _controller.StateChanged += OnStateChanged;
            _controller.CardsPlaced += OnCardsPlaced;
            if (_controller.LastState != null) OnStateChanged(_controller.LastState);
        }

        private void OnDisable()
        {
            if (_controller == null) return;
            _controller.StateChanged -= OnStateChanged;
            _controller.CardsPlaced -= OnCardsPlaced;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null || state.Players.Count == 0) return;
            RenderVisible(PlayerVisibleState.From(state, state.Players[0].Id));
        }

        /// <summary>
        /// Aperçu juste après la pose : seule la zone est redessinée depuis la Pile de l'état
        /// d'aperçu, les sièges restent tels quels jusqu'au StateChanged résolu.
        /// </summary>
        private void OnCardsPlaced(GameState preview)
        {
            if (_zone == null || preview == null) return;
            ICardSkin skin = _skinRegistry != null ? _skinRegistry.Current : null;
            _zone.RenderPlacedPile(preview.Pile.Cards, skin);
        }

        /// <summary>Rend la table depuis une vue visible (ex. phase d'échange, sans GameState).</summary>
        public void RenderVisible(IPlayerVisibleState visible)
        {
            if (visible == null) return;
            Render(TableProjection.Build(visible));
        }

        public void Render(TableViewModel model)
        {
            if (model == null) return;
            ICardSkin skin = _skinRegistry != null ? _skinRegistry.Current : null;

            if (_selfSeat != null) _selfSeat.RenderSelf(model.Self, _selfDisplayName, skin);
            if (_zone != null) _zone.Render(model.Zone, skin);

            HideOpponentSeats();
            for (int i = 0; i < model.Opponents.Count; i++)
            {
                OpponentSeatViewModel o = model.Opponents[i];
                PlayerSeatView view = SeatFor(o.Position);
                if (view != null) view.RenderOpponent(o, skin);
            }
            if (Rendered != null) Rendered();
        }

        private void HideOpponentSeats()
        {
            if (_leftSeat != null) _leftSeat.RenderOpponent(null, null);
            if (_topSeat != null) _topSeat.RenderOpponent(null, null);
            if (_rightSeat != null) _rightSeat.RenderOpponent(null, null);
        }

        private PlayerSeatView SeatFor(SeatPosition position)
        {
            switch (position)
            {
                case SeatPosition.Left: return _leftSeat;
                case SeatPosition.Top: return _topSeat;
                case SeatPosition.Right: return _rightSeat;
                default: return null;
            }
        }
    }
}