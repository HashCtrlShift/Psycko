using System;

namespace Psycko.Core.Domain
{
    /// <summary>Choix explicite et immuable de la carte et du joueur destinataire du Don.</summary>
    public readonly struct GiftResolutionChoice
    {
        public Card CardToGive { get; }

        /// <summary>
        /// Id stable du joueur destinataire (jamais un seat index) : seul le
        /// donneur connaît la carte donnée et le destinataire choisi ; la
        /// résolution Id → seat index est une responsabilité de Core
        /// (GameOrchestrator, via IGameStateQuery.GetSeatIndex), jamais de
        /// l'agent qui produit ce choix.
        /// </summary>
        public int RecipientPlayerId { get; }

        public GiftResolutionChoice(Card cardToGive, int recipientPlayerId)
        {
            if (cardToGive is null)
                throw new ArgumentNullException(nameof(cardToGive));
            if (recipientPlayerId < 0)
                throw new ArgumentOutOfRangeException(nameof(recipientPlayerId));

            CardToGive = cardToGive;
            RecipientPlayerId = recipientPlayerId;
        }
    }
}
