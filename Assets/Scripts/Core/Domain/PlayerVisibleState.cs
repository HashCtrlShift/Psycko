#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Vue immuable de l'état de la partie pour UN joueur : ses trois couches complètes,
    /// et pour les adversaires uniquement ce que les règles rendent public
    /// (FaceUp + comptes). Instantané : copié à la construction, jamais lié au GameState.
    /// </summary>
    public sealed class PlayerVisibleState : IPlayerVisibleState
    {
        public int SelfPlayerId { get; }
        public IReadOnlyList<Card> SelfHand { get; }
        public IReadOnlyList<Card> SelfFaceUp { get; }
        public IReadOnlyList<Card> SelfFaceDown { get; }
        public IReadOnlyList<IOpponentVisibleInfo> Opponents { get; }
        public Pile Pile { get; }
        public int DrawPileCount { get; }
        public int ActivePlayerIndex { get; }
        public PlayDirection Direction { get; }
        public HeightConstraint Constraint { get; }
        public DefRank RefRank { get; }

        private readonly int _activePlayerId;

        public int GetActivePlayerId() => _activePlayerId;

        private PlayerVisibleState(
            int selfPlayerId,
            IReadOnlyList<Card> selfHand,
            IReadOnlyList<Card> selfFaceUp,
            IReadOnlyList<Card> selfFaceDown,
            IReadOnlyList<IOpponentVisibleInfo> opponents,
            Pile pile,
            int drawPileCount,
            int activePlayerIndex,
            int activePlayerId,
            PlayDirection direction,
            HeightConstraint constraint,
            DefRank refRank)
        {
            SelfPlayerId = selfPlayerId;
            SelfHand = selfHand;
            SelfFaceUp = selfFaceUp;
            SelfFaceDown = selfFaceDown;
            Opponents = opponents;
            Pile = pile;
            DrawPileCount = drawPileCount;
            ActivePlayerIndex = activePlayerIndex;
            _activePlayerId = activePlayerId;
            Direction = direction;
            Constraint = constraint;
            RefRank = refRank;
        }

        /// <summary>
        /// Construit la vue du joueur <paramref name="playerId"/> à partir de l'état complet.
        /// Les adversaires restent dans l'ordre de siège, le joueur lui-même exclu.
        /// </summary>
        public static PlayerVisibleState From(GameState state, int playerId)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));

            int selfSeat = state.GetSeatIndex(playerId);
            Player self = state.Players[selfSeat];

            var opponents = new List<IOpponentVisibleInfo>(state.Players.Count - 1);
            for (int seat = 0; seat < state.Players.Count; seat++)
            {
                if (seat == selfSeat) continue;
                opponents.Add(OpponentVisibleInfo.From(state.Players[seat]));
            }

            return new PlayerVisibleState(
                self.Id,
                self.Hand.ToList().AsReadOnly(),
                self.FaceUp.ToList().AsReadOnly(),
                self.FaceDown.ToList().AsReadOnly(),
                opponents.AsReadOnly(),
                state.Pile,
                state.DrawPile.Count,
                state.ActivePlayerIndex,
                state.GetActivePlayer().Id,
                state.Direction,
                state.Constraint,
                state.RefRank);
        }

        /// <summary>
        /// Vue pendant l'échange pré-partie (aucun GameState n'existe encore) :
        /// pile vide, sens horaire, contrainte normale, pas de joueur actif (index 0 neutre).
        /// </summary>
        public static PlayerVisibleState FromExchange(
            Exchange.PreGameExchangeState exchange,
            int playerId)
        {
            if (exchange is null) throw new ArgumentNullException(nameof(exchange));

            int selfSeat = -1;
            for (int i = 0; i < exchange.Players.Count; i++)
            {
                if (exchange.Players[i].Id == playerId) { selfSeat = i; break; }
            }
            if (selfSeat < 0)
                throw new ArgumentException($"Aucun joueur avec l'Id {playerId}.", nameof(playerId));

            Player self = exchange.Players[selfSeat];

            var opponents = new List<IOpponentVisibleInfo>(exchange.Players.Count - 1);
            for (int seat = 0; seat < exchange.Players.Count; seat++)
            {
                if (seat == selfSeat) continue;
                opponents.Add(OpponentVisibleInfo.From(exchange.Players[seat]));
            }

            return new PlayerVisibleState(
                self.Id,
                self.Hand.ToList().AsReadOnly(),
                self.FaceUp.ToList().AsReadOnly(),
                self.FaceDown.ToList().AsReadOnly(),
                opponents.AsReadOnly(),
                Pile.Empty,
                exchange.DrawPile.Count,
                0,
                exchange.Players[0].Id,
                PlayDirection.Clockwise,
                HeightConstraint.Normal,
                DefRank.Three);
        }
    }
}