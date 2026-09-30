using System;

namespace Psycko.Core.Domain
{
    /// <summary>Choix explicite et immuable de la carte et du siège destinataire du Don.</summary>
    public readonly struct GiftResolutionChoice
    {
        public Card CardToGive { get; }
        public int RecipientSeatIndex { get; }

        public GiftResolutionChoice(Card cardToGive, int recipientSeatIndex)
        {
            if (cardToGive is null)
                throw new ArgumentNullException(nameof(cardToGive));
            if (recipientSeatIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(recipientSeatIndex));

            CardToGive = cardToGive;
            RecipientSeatIndex = recipientSeatIndex;
        }
    }
}
