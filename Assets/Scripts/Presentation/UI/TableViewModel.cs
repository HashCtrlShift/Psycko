using System;
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Presentation.UI
{
    public enum SeatPosition { Bottom, Left, Top, Right }

    /// <summary>Siège adverse : quantités + FaceUp (public) + rang d'arrivée (public).</summary>
    public sealed class OpponentSeatViewModel
    {
        public int PlayerId { get; }
        public string Name { get; }
        public SeatPosition Position { get; }
        public int HandCount { get; }
        public int FaceUpCount { get; }
        public int FaceDownCount { get; }
        public bool IsActive { get; }
        public IReadOnlyList<Card> FaceUp { get; }
        public int? FinishRank { get; }

        public OpponentSeatViewModel(int playerId, string name, SeatPosition position,
            int handCount, int faceUpCount, int faceDownCount, bool isActive,
            IReadOnlyList<Card> faceUp = null, int? finishRank = null)
        {
            PlayerId = playerId;
            Name = name ?? string.Empty;
            Position = position;
            HandCount = handCount;
            FaceUpCount = faceUpCount;
            FaceDownCount = faceDownCount;
            IsActive = isActive;
            FaceUp = faceUp ?? Array.Empty<Card>();
            FinishRank = finishRank;
        }
    }

    /// <summary>
    /// Siège humain : main et FaceUp visibles. FaceDown = compteur seul (jeu à l'aveugle),
    /// la Présentation ne manipule jamais de Card FaceDown.
    /// </summary>
    public sealed class SelfSeatViewModel
    {
        public int PlayerId { get; }
        public SeatPosition Position => SeatPosition.Bottom;
        public IReadOnlyList<Card> Hand { get; }
        public IReadOnlyList<Card> FaceUp { get; }
        public int FaceDownCount { get; }
        public bool IsActive { get; }
        public int? FinishRank { get; }

        public SelfSeatViewModel(int playerId, IReadOnlyList<Card> hand,
            IReadOnlyList<Card> faceUp, int faceDownCount, bool isActive,
            int? finishRank = null)
        {
            PlayerId = playerId;
            Hand = hand ?? Array.Empty<Card>();
            FaceUp = faceUp ?? Array.Empty<Card>();
            FaceDownCount = faceDownCount < 0 ? 0 : faceDownCount;
            IsActive = isActive;
            FinishRank = finishRank;
        }
    }

    /// <summary>
    /// Pioche et pile : compteurs + dernières cartes publiques de la pile.
    /// PileCards est dans l'ordre de pose (dernier = dessus).
    /// PileStartIndex = index absolu (dans la pile complète) de PileCards[0].
    /// </summary>
    public sealed class ZoneViewModel
    {
        public int DrawPileCount { get; }
        public int PileCardCount { get; }
        public bool IsPileEmpty { get; }
        public Card TopPileCard { get; }
        public IReadOnlyList<Card> PileCards { get; }
        public int PileStartIndex { get; }

        public ZoneViewModel(int drawPileCount, int pileCardCount,
            Card topPileCard = null, IReadOnlyList<Card> pileCards = null,
            int pileStartIndex = 0)
        {
            DrawPileCount = drawPileCount < 0 ? 0 : drawPileCount;
            PileCardCount = pileCardCount < 0 ? 0 : pileCardCount;
            IsPileEmpty = PileCardCount == 0;
            TopPileCard = IsPileEmpty ? null : topPileCard;
            PileCards = IsPileEmpty ? Array.Empty<Card>() : (pileCards ?? Array.Empty<Card>());
            PileStartIndex = pileStartIndex < 0 ? 0 : pileStartIndex;
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