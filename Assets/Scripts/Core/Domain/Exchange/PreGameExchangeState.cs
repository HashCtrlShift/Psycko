using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Core.Domain.Exchange
{
    /// <summary>État C# pur des paires d’échange pré-partie effectivement réalisées.</summary>
    public sealed class PreGameExchangeState
    {
        private readonly List<Card> _hand;
        private readonly List<Card> _faceUp;
        private readonly List<ExchangeChoice> _completed = new List<ExchangeChoice>();
        private Card _pendingHand;
        private Card _pendingFaceUp;

        /// <summary>Émis immédiatement après chaque permutation validée (jamais pour un clic ou Ready).</summary>
        public event Action<ExchangeChoice> ExchangeApplied;

        public PreGameExchangeState(IEnumerable<Card> hand, IEnumerable<Card> faceUp)
        {
            _hand = new List<Card>(hand ?? Enumerable.Empty<Card>());
            _faceUp = new List<Card>(faceUp ?? Enumerable.Empty<Card>());
        }

        public IReadOnlyList<Card> Hand { get { return _hand.AsReadOnly(); } }
        public IReadOnlyList<Card> FaceUp { get { return _faceUp.AsReadOnly(); } }
        public IReadOnlyList<ExchangeChoice> Completed { get { return _completed.AsReadOnly(); } }
        public Card PendingHand { get { return _pendingHand; } }
        public Card PendingFaceUp { get { return _pendingFaceUp; } }

        public bool SelectHand(Card card)
        {
            if (card == null || !_hand.Contains(card)) return false;
            if (_pendingFaceUp != null) return Swap(card, _pendingFaceUp);
            _pendingHand = _pendingHand == card ? null : card;
            return true;
        }

        public bool SelectFaceUp(Card card)
        {
            if (card == null || !_faceUp.Contains(card)) return false;
            if (_pendingHand != null) return Swap(_pendingHand, card);
            _pendingFaceUp = _pendingFaceUp == card ? null : card;
            return true;
        }

        /// <summary>Annule la sélection partielle seulement ; les échanges déjà réalisés sont conservés.</summary>
        public void AbandonPendingSelection()
        {
            _pendingHand = null;
            _pendingFaceUp = null;
        }

        /// <summary>Retourne une copie en lecture seule des paires terminées destinées au Core.</summary>
        public IReadOnlyList<ExchangeChoice> GetCompletedExchanges()
        {
            return _completed.ToList().AsReadOnly();
        }

        private bool Swap(Card handCard, Card faceUpCard)
        {
            int handIndex = _hand.IndexOf(handCard);
            int faceUpIndex = _faceUp.IndexOf(faceUpCard);
            if (handIndex < 0 || faceUpIndex < 0) return false;
            _completed.Add(new ExchangeChoice(handCard, faceUpCard));
            _hand[handIndex] = faceUpCard;
            _faceUp[faceUpIndex] = handCard;
            AbandonPendingSelection();
            Action<ExchangeChoice> handler = ExchangeApplied;
            if (handler != null) handler(_completed[_completed.Count - 1]);
            return true;
        }
    }
}
