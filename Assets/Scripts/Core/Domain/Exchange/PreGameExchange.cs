using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Core.Services.Exchange
{
    /// <summary>
    /// Résultat d'une opération d'échange : nouvel état, ou rejet motivé (état inchangé).
    /// </summary>
    public readonly struct ExchangeResolution
    {
        public PreGameExchangeState State { get; }
        public ExchangeRejectionReason Rejection { get; }
        public bool IsAccepted => Rejection == ExchangeRejectionReason.None;

        private ExchangeResolution(PreGameExchangeState state, ExchangeRejectionReason rejection)
        {
            State = state;
            Rejection = rejection;
        }

        public static ExchangeResolution Accepted(PreGameExchangeState state)
            => new ExchangeResolution(state, ExchangeRejectionReason.None);

        public static ExchangeResolution Rejected(PreGameExchangeState state, ExchangeRejectionReason reason)
            => new ExchangeResolution(state, reason);
    }

    /// <summary>
    /// Service d'échange pré-partie. Règle : 1 carte Main contre 1 carte FaceUp du même joueur,
    /// répétable tant que le joueur n'est pas prêt, Jokers inclus. Les joueurs n'agissant que sur
    /// leurs propres cartes, aucun conflit : les opérations s'appliquent séquentiellement.
    /// Le chrono (~1 min) est hors Core : l'appelant invoque Finish() à l'expiration.
    /// </summary>
    public static class PreGameExchange
    {
        public static ExchangeResolution Swap(PreGameExchangeState state, int playerId, ExchangeChoice choice)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (state.IsClosed)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.ExchangeClosed);

            int seat = FindSeat(state, playerId);
            if (seat < 0)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.UnknownPlayer);

            if (state.IsReady(playerId))
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.PlayerAlreadyReady);

            var player = state.Players[seat];

            var hand = new List<Card>(player.Hand);
            var faceUp = new List<Card>(player.FaceUp);

            int handIdx = hand.IndexOf(choice.HandCard);
            if (handIdx < 0)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.HandCardNotFound);

            int faceUpIdx = faceUp.IndexOf(choice.FaceUpCard);
            if (faceUpIdx < 0)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.FaceUpCardNotFound);

            hand[handIdx] = choice.FaceUpCard;
            faceUp[faceUpIdx] = choice.HandCard;

            var updatedPlayer = player.WithHand(hand).WithFaceUp(faceUp);
            var players = new List<Player>(state.Players) { [seat] = updatedPlayer };

            return ExchangeResolution.Accepted(state.WithPlayers(players));
        }

        public static ExchangeResolution MarkReady(PreGameExchangeState state, int playerId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (state.IsClosed)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.ExchangeClosed);
            if (FindSeat(state, playerId) < 0)
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.UnknownPlayer);
            if (state.IsReady(playerId))
                return ExchangeResolution.Rejected(state, ExchangeRejectionReason.PlayerAlreadyReady);

            var next = state.WithReady(playerId);
            return ExchangeResolution.Accepted(next.AllReady ? next.WithClosed() : next);
        }

        /// <summary>Clôture forcée (expiration du chrono) : tous les joueurs restants sont considérés prêts.</summary>
        public static PreGameExchangeState Finish(PreGameExchangeState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return state.WithClosed();
        }

        /// <summary>
        /// Crée le GameState de départ à partir de l'état d'échange clos.
        /// Le premier joueur est désigné ici (FirstPlayerResolver dans CreateInitial),
        /// donc sur les mains APRÈS échange.
        /// </summary>
        public static GameState CreateGameState(PreGameExchangeState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.IsClosed)
                throw new InvalidOperationException("L'échange pré-partie n'est pas terminé.");

            return GameState.CreateInitial(state.Players, state.DrawPile);
        }

        private static int FindSeat(PreGameExchangeState state, int playerId)
        {
            for (int i = 0; i < state.Players.Count; i++)
                if (state.Players[i].Id == playerId)
                    return i;
            return -1;
        }
    }
}