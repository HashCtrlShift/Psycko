using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Entrée immuable du log de partie. Soit une action joueur, soit la fin de partie.
    /// </summary>
    public sealed class GameLogEntry
    {
        public bool IsGameEnd { get; }
        public int PlayerId { get; }
        public ActionKind Kind { get; }
        public IReadOnlyList<Card> Cards { get; }
        public IReadOnlyList<Card> PileAfter { get; }
        public int? TargetPlayerId { get; }
        public int? PsyckoPlayerId { get; }

        /// <summary>Effets déclenchés par l'action (colonne "Effets"). Jamais null.</summary>
        public IReadOnlyList<EffectLogDetail> Effects { get; }

        private GameLogEntry(bool isGameEnd, int playerId, ActionKind kind,
            IReadOnlyList<Card> cards, IReadOnlyList<Card> pileAfter,
            int? targetPlayerId, int? psyckoPlayerId,
            IReadOnlyList<EffectLogDetail> effects)
        {
            IsGameEnd = isGameEnd;
            PlayerId = playerId;
            Kind = kind;
            Cards = cards;
            PileAfter = pileAfter;
            TargetPlayerId = targetPlayerId;
            PsyckoPlayerId = psyckoPlayerId;
            Effects = effects;
        }

        public static GameLogEntry PlayerAction(
            int playerId,
            ActionKind kind,
            IEnumerable<Card> cards,
            IEnumerable<Card> pileAfter,
            int? targetPlayerId = null,
            IEnumerable<EffectLogDetail> effects = null)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (pileAfter == null) throw new ArgumentNullException(nameof(pileAfter));

            return new GameLogEntry(false, playerId, kind,
                cards.ToList().AsReadOnly(),
                pileAfter.ToList().AsReadOnly(),
                targetPlayerId, null,
                (effects ?? Enumerable.Empty<EffectLogDetail>()).ToList().AsReadOnly());
        }

        public static GameLogEntry GameEnd(int? psyckoPlayerId)
            => new GameLogEntry(true, -1, default, Array.Empty<Card>(),
                Array.Empty<Card>(), null, psyckoPlayerId,
                Array.Empty<EffectLogDetail>());
    }
}
