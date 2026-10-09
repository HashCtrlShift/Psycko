using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain.Log
{
    /// <summary>Entrée immuable décrivant une action et l'état de la Pile avant cette action.</summary>
    public sealed class GameLogEntry
    {
        public int PlayerId { get; }
        public ActionKind Kind { get; }
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>Pile telle qu'elle était AVANT l'action (jamais null).</summary>
        public IReadOnlyList<Card> PileBefore { get; }

        public int? TargetPlayerId { get; }

        /// <summary>Effets déclenchés par l'action. Jamais null.</summary>
        public IReadOnlyList<EffectLogDetail> Effects { get; }

        /// <summary>Précision textuelle (raison du premier joueur, transition de phase). Null sinon.</summary>
        public string Detail { get; }

        private GameLogEntry(int playerId, ActionKind kind, IEnumerable<Card> cards,
            IEnumerable<Card> pileBefore, int? targetPlayerId,
            IEnumerable<EffectLogDetail> effects, string detail)
        {
            PlayerId = playerId;
            Kind = kind;
            Cards = (cards ?? Enumerable.Empty<Card>()).ToList().AsReadOnly();
            PileBefore = (pileBefore ?? Enumerable.Empty<Card>()).ToList().AsReadOnly();
            TargetPlayerId = targetPlayerId;
            Effects = (effects ?? Enumerable.Empty<EffectLogDetail>()).ToList().AsReadOnly();
            Detail = detail;
        }

        /// <summary>Crée une action de joueur.</summary>
        public static GameLogEntry PlayerAction(int playerId, ActionKind kind,
            IEnumerable<Card> cards, IEnumerable<Card> pileBefore,
            int? targetPlayerId = null, IEnumerable<EffectLogDetail> effects = null,
            string detail = null)
            => new GameLogEntry(playerId, kind, cards, pileBefore, targetPlayerId, effects, detail);

        /// <summary>Crée le marqueur de début de partie (premier joueur + raison).</summary>
        public static GameLogEntry GameStart(int firstPlayerId, string reason)
            => new GameLogEntry(firstPlayerId, ActionKind.GameStart, null, null, null, null, reason);

        /// <summary>Crée une transition de phase d'un joueur.</summary>
        public static GameLogEntry PhaseChange(int playerId, string detail, IEnumerable<Card> pileBefore)
            => new GameLogEntry(playerId, ActionKind.PhaseChange, null, pileBefore, null, null, detail);

        /// <summary>Crée le marqueur de fin de partie (psyckoPlayerId null = partie en erreur).</summary>
        public static GameLogEntry GameEnd(int? psyckoPlayerId)
            => new GameLogEntry(psyckoPlayerId ?? -1, ActionKind.GameEnd, null, null, null, null, null);
    }
}