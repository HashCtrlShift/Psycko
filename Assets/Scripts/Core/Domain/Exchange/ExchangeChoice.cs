using System;

namespace Psycko.Core.Domain.Exchange
{
    /// <summary>Un échange pré-partie : 1 carte de la Main contre 1 carte FaceUp du même joueur.</summary>
    public readonly struct ExchangeChoice
    {
        public Card HandCard { get; }
        public Card FaceUpCard { get; }

        public ExchangeChoice(Card handCard, Card faceUpCard)
        {
            HandCard = handCard ?? throw new ArgumentNullException(nameof(handCard));
            FaceUpCard = faceUpCard ?? throw new ArgumentNullException(nameof(faceUpCard));
        }
    }
}