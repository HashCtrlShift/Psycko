using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Presentation.Controllers;
using Psycko.Presentation.Interfaces;
using Psycko.Presentation.Models;
using UnityEngine;

namespace Psycko.Presentation.UI
{
    /// <summary>Rendu atomique des vues de table depuis un état ou une projection visible.</summary>
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

        // Ancien chemin, supprimé en T65-g : rétabli temporairement tant que le contrôleur
        // n'appelle pas encore Render(GameState) directement.
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
            // Strictement le même chemin de projection que l'ancien RenderVisible(...).
            Render(state);
        }

        /// <summary>
        /// Ancien chemin, supprimé en T65-g : l'aperçu dessine uniquement la Pile ;
        /// les sièges restent inchangés jusqu'au StateChanged résolu.
        /// </summary>
        private void OnCardsPlaced(GameState preview)
        {
            if (_zone == null || preview == null) return;
            ICardSkin skin = _skinRegistry != null ? _skinRegistry.Current : null;
            _zone.RenderPlacedPile(preview.Pile.Cards, skin);
        }

        /// <summary>Point d'entrée canonique : toutes les vues reflètent ce GameState.</summary>
        public void Render(GameState state)
        {
            if (state == null || state.Players == null || state.Players.Count == 0) return;
            IPlayerVisibleState visible = PlayerVisibleState.From(state, state.Players[0].Id);
            Render(TableProjection.Build(visible));
        }

        /// <summary>Rendu d'une vue visible (compatibilité échange pré-partie).</summary>
        public void RenderVisible(IPlayerVisibleState visible)
        {
            if (visible == null) return;
            Render(TableProjection.Build(visible));
        }

        /// <summary>API de projection conservée pour les appelants existants.</summary>
        public void Render(TableViewModel model)
        {
            if (model == null) return;
            ICardSkin skin = _skinRegistry != null ? _skinRegistry.Current : null;
            if (_selfSeat != null) _selfSeat.RenderSelf(model.Self, _selfDisplayName, skin);
            if (_zone != null) _zone.Render(model.Zone, skin);
            HideOpponentSeats();
            for (int i = 0; i < model.Opponents.Count; i++)
            {
                OpponentSeatViewModel opponent = model.Opponents[i];
                PlayerSeatView view = SeatFor(opponent.Position);
                if (view != null) view.RenderOpponent(opponent, skin);
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
