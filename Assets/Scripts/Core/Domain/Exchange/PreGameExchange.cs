using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain.Exchange
{
    /// <summary>
    /// État immuable de la phase d'échange pré-partie : joueurs distribués (ordre de siège),
    /// pioche restante, et ensemble des joueurs « Prêts ». Aucun GameState n'existe encore :
    /// il est créé après l'échange pour que le premier joueur soit désigné sur les mains finales.
    /// </summary>
    public sealed class PreGameExchangeState
    {
        private readonly List<Player> _players;
        private readonly List<Card> _drawPile;
        private readonly HashSet<int> _readyIds;

        public IReadOnlyList<Player> Players => _players;
        public IReadOnlyList<Card> DrawPile => _drawPile;
        public IReadOnlyCollection<int> ReadyPlayerIds => _readyIds;

        /// <summary>True quand l'échange est terminé (tous prêts ou clôture forcée).</summary>
        public bool IsClosed { get; }

        public bool IsReady(int playerId) => _readyIds.Contains(playerId);

        public bool AllReady => _players.All(p => _readyIds.Contains(p.Id));

        public PreGameExchangeState(
            IEnumerable<Player> players,
            IEnumerable<Card> drawPile,
            IEnumerable<int> readyPlayerIds = null,
            bool isClosed = false)
        {
            if (players is null) throw new ArgumentNullException(nameof(players));
            if (drawPile is null) throw new ArgumentNullException(nameof(drawPile));

            _players = players.ToList();
            _drawPile = drawPile.ToList();
            _readyIds = new HashSet<int>(readyPlayerIds ?? Enumerable.Empty<int>());
            IsClosed = isClosed;
        }

        public PreGameExchangeState WithPlayers(IEnumerable<Player> players)
            => new PreGameExchangeState(players, _drawPile, _readyIds, IsClosed);

        public PreGameExchangeState WithReady(int playerId)
        {
            var ready = new HashSet<int>(_readyIds) { playerId };
            return new PreGameExchangeState(_players, _drawPile, ready, IsClosed);
        }

        public PreGameExchangeState WithClosed()
            => new PreGameExchangeState(_players, _drawPile, _readyIds, true);
    }
}