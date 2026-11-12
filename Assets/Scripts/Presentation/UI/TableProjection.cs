using System;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Presentation.UI
{
    /// <summary>
    /// Projection pure IPlayerVisibleState -> TableViewModel. Sans UnityEngine, testable en EditMode.
    /// Les adversaires sont placés dans l'ordre de siège : Left, Top, Right.
    /// Les FaceDown (soi et adversaires) ne sont projetés que sous forme de compteur.
    /// </summary>
    public static class TableProjection
    {
        public const int MaxVisiblePileCards = 12;

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
                state.SelfFaceDown == null ? 0 : state.SelfFaceDown.Count,
                state.SelfPlayerId == activeId,
                state.SelfFinishRank);

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
                    o.PlayerId == activeId,
                    o.FaceUp,
                    o.FinishRank));
            }

            int pileCount = state.Pile == null ? 0 : state.Pile.Count;
            Card top = state.Pile == null ? null : state.Pile.TopCard;
            int startIndex;
            IReadOnlyList<Card> tail = ReadPileTail(state.Pile, MaxVisiblePileCards, out startIndex);
            var zone = new ZoneViewModel(state.DrawPileCount, pileCount, top, tail, startIndex);

            return new TableViewModel(self, opponents.AsReadOnly(), zone);
        }

        /// <summary>
        /// Les dernières cartes publiques de la pile, dans l'ordre de pose
        /// (dernier index = dessus). startIndex = index absolu de la première retournée.
        /// </summary>
        private static IReadOnlyList<Card> ReadPileTail(Pile pile, int max, out int startIndex)
        {
            startIndex = 0;
            if (pile == null) return Array.Empty<Card>();
            IReadOnlyList<Card> cards = pile.Cards;
            if (cards == null || cards.Count == 0) return Array.Empty<Card>();

            int take = Math.Min(cards.Count, max);
            startIndex = cards.Count - take;
            var tail = new Card[take];
            for (int i = 0; i < take; i++) tail[i] = cards[startIndex + i];
            return tail;
        }
    }
}