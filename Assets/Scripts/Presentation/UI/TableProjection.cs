using System;
using System.Collections.Generic;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.UI
{
    /// <summary>
    /// Projection pure IPlayerVisibleState -> TableViewModel. Sans UnityEngine, testable en EditMode.
    /// Les adversaires sont placés dans l'ordre de siège : Left, Top, Right.
    /// </summary>
    public static class TableProjection
    {
        private static readonly SeatPosition[] OpponentSlots =
        {
            SeatPosition.Left, SeatPosition.Top, SeatPosition.Right
        };

        public static TableViewModel Build(IPlayerVisibleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            int activeId = state.GetActivePlayerId();

            var self = new SelfSeatViewModel(
                state.SelfPlayerId,
                state.SelfHand,
                state.SelfFaceUp,
                state.SelfFaceDown,
                state.SelfPlayerId == activeId);

            var opponents = new List<OpponentSeatViewModel>();
            IReadOnlyList<IOpponentVisibleInfo> source = state.Opponents;
            int count = source == null ? 0 : Math.Min(source.Count, OpponentSlots.Length);
            for (int i = 0; i < count; i++)
            {
                IOpponentVisibleInfo o = source[i];
                opponents.Add(new OpponentSeatViewModel(
                    o.PlayerId,
                    o.Name,
                    OpponentSlots[i],
                    o.HandCount,
                    o.FaceUp == null ? 0 : o.FaceUp.Count,
                    o.FaceDownCount,
                    o.PlayerId == activeId));
            }

            int pileCount = state.Pile == null ? 0 : state.Pile.Count;
            var zone = new ZoneViewModel(state.DrawPileCount, pileCount);

            return new TableViewModel(self, opponents.AsReadOnly(), zone);
        }
    }
}