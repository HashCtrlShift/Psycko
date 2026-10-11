#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Vue immuable et filtrée d'un adversaire : FaceUp public, seulement les COMPTES
    /// pour la Main et les FaceDown. Aucune référence vers les cartes cachées.
    /// </summary>
    public sealed class OpponentVisibleInfo : IOpponentVisibleInfo
    {
        public int PlayerId { get; }
        public string Name { get; }
        public IReadOnlyList<Card> FaceUp { get; }
        public int HandCount { get; }
        public int FaceDownCount { get; }
        public DefPhase CurrentPhase { get; }

        public OpponentVisibleInfo(
            int playerId,
            string name,
            IEnumerable<Card> faceUp,
            int handCount,
            int faceDownCount,
            DefPhase currentPhase)
        {
            if (faceUp is null) throw new ArgumentNullException(nameof(faceUp));
            if (handCount < 0) throw new ArgumentOutOfRangeException(nameof(handCount));
            if (faceDownCount < 0) throw new ArgumentOutOfRangeException(nameof(faceDownCount));

            PlayerId = playerId;
            Name = name ?? string.Empty;
            FaceUp = faceUp.ToList().AsReadOnly();
            HandCount = handCount;
            FaceDownCount = faceDownCount;
            CurrentPhase = currentPhase;
        }

        /// <summary>Construit la vue filtrée d'un joueur adverse.</summary>
        public static OpponentVisibleInfo From(Player opponent)
        {
            if (opponent is null) throw new ArgumentNullException(nameof(opponent));

            return new OpponentVisibleInfo(
                opponent.Id,
                opponent.Name,
                opponent.FaceUp,
                opponent.Hand.Count,
                opponent.FaceDown.Count,
                opponent.CurrentPhase);
        }
    }
}