using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Presentation.Controllers;
using UnityEngine;

namespace Psycko.Presentation.UI
{
    /// <summary>
    /// Vue de table en paysage. S'abonne à GameplayController.StateChanged,
    /// construit la vue du joueur humain, projette, puis pousse vers les sièges et la zone.
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

        private void OnEnable()
        {
            if (_controller != null) _controller.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_controller != null) _controller.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null || state.Players.Count == 0) return;
            IPlayerVisibleState visible = PlayerVisibleState.From(state, state.Players[0].Id);
            Render(TableProjection.Build(visible));
        }

        public void Render(TableViewModel model)
        {
            if (model == null) return;

            if (_selfSeat != null) _selfSeat.RenderSelf(model.Self, _selfDisplayName);
            if (_zone != null) _zone.Render(model.Zone);

            // Masque d'abord, puis affiche les sièges réellement occupés.
            HideOpponentSeats();
            for (int i = 0; i < model.Opponents.Count; i++)
            {
                OpponentSeatViewModel o = model.Opponents[i];
                PlayerSeatView view = SeatFor(o.Position);
                if (view != null) view.RenderOpponent(o);
            }
        }

        private void HideOpponentSeats()
        {
            if (_leftSeat != null) _leftSeat.RenderOpponent(null);
            if (_topSeat != null) _topSeat.RenderOpponent(null);
            if (_rightSeat != null) _rightSeat.RenderOpponent(null);
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