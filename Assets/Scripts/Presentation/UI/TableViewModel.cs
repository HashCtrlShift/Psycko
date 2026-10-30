using System;
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Presentation.UI
{
    public enum SeatPosition { Bottom, Left, Top, Right }

    /// <summary>Siège adverse : QUANTITÉS uniquement, aucune carte.</summary>
    public sealed class OpponentSeatViewModel
    {
        public int PlayerId { get; }
        public string Name { get; }
        public SeatPosition Position { get; }
        public int HandCount { get; }
        public int FaceUpCount { get; }
        public int FaceDownCount { get; }
        public bool IsActive { get; }

        public OpponentSeatViewModel(int playerId, string name, SeatPosition position,
            int handCount, int faceUpCount, int faceDownCount, bool isActive)
        {
            PlayerId = playerId;
            Name = name ?? string.Empty;
            Position = position;
            HandCount = handCount;
            FaceUpCount = faceUpCount;
            FaceDownCount = faceDownCount;
            IsActive = isActive;
        }
    }

    /// <summary>Siège humain : couches complètes (visibles pour soi).</summary>
    public sealed class SelfSeatViewModel
    {
        public int PlayerId { get; }
        public SeatPosition Position => SeatPosition.Bottom;
        public IReadOnlyList<Card> Hand { get; }
        public IReadOnlyList<Card> FaceUp { get; }
        public IReadOnlyList<Card> FaceDown { get; }
        public bool IsActive { get; }

        public SelfSeatViewModel(int playerId, IReadOnlyList<Card> hand,
            IReadOnlyList<Card> faceUp, IReadOnlyList<Card> faceDown, bool isActive)
        {
            PlayerId = playerId;
            Hand = hand ?? Array.Empty<Card>();
            FaceUp = faceUp ?? Array.Empty<Card>();
            FaceDown = faceDown ?? Array.Empty<Card>();
            IsActive = isActive;
        }
    }

    /// <summary>Pioche et pile : compteurs seulement (le rendu des cartes viendra en T53).</summary>
    public sealed class ZoneViewModel
    {
        public int DrawPileCount { get; }
        public int PileCardCount { get; }
        public bool IsPileEmpty { get; }

        public ZoneViewModel(int drawPileCount, int pileCardCount)
        {
            DrawPileCount = drawPileCount < 0 ? 0 : drawPileCount;
            PileCardCount = pileCardCount < 0 ? 0 : pileCardCount;
            IsPileEmpty = PileCardCount == 0;
        }
    }

    public sealed class TableViewModel
    {
        public SelfSeatViewModel Self { get; }
        public IReadOnlyList<OpponentSeatViewModel> Opponents { get; }
        public ZoneViewModel Zone { get; }

        public TableViewModel(SelfSeatViewModel self,
            IReadOnlyList<OpponentSeatViewModel> opponents, ZoneViewModel zone)
        {
            Self = self;
            Opponents = opponents;
            Zone = zone;
        }
    }
}