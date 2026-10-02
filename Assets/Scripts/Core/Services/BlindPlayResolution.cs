using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Résultat d'un coup joué à l'aveugle (phase Luck).
    /// Encapsule le PlayResult classique + la carte réellement révélée.
    /// Réservé au seul chemin ApplyBlindPlay : la carte n'est jamais présente
    /// sur les rejets précoces (avant lecture de la carte face-cachée).
    /// </summary>
    public readonly struct BlindPlayResolution
    {
        public PlayResult Result { get; }
        public Card RevealedCard { get; }

        private BlindPlayResolution(PlayResult result, Card revealedCard)
        {
            Result = result;
            RevealedCard = revealedCard;
        }

        public static BlindPlayResolution Of(PlayResult result, Card revealedCard)
            => new BlindPlayResolution(result, revealedCard);
    }
}