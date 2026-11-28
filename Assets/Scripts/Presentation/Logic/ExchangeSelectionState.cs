using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Presentation.Logic
{
    /// <summary>État C# pur des paires d'échange pré-partie effectivement réalisées.</summary>
    public sealed class ExchangeSelectionState
    {
        private readonly List<Card> _hand;
        private readonly List<Card> _faceUp;
        private readonly List<ExchangeChoice> _completed = new List<ExchangeChoice>();
        private Card _pendingHand;
        private Card _pendingFaceUp;

        /// <summary>Émis immédiatement après chaque permutation validée (jamais pour un clic ou Ready).</summary>
        public event Action<ExchangeChoice> ExchangeApplied;

        public ExchangeSelectionState(IEnumerable<Card> hand, IEnumerable<Card> faceUp)
        {
            _hand = new List<Card>(hand ?? Enumerable.Empty<Card>());
            _faceUp = new List<Card>(faceUp ?? Enumerable.Empty<Card>());
        }

        // ... reste du corps identique (propriétés, SelectHand, SelectFaceUp,
        // AbandonPendingSelection, GetCompletedExchanges, Swap)
    }
}