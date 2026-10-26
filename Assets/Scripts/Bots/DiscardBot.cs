#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Comparison;

namespace Psycko.Bots
{
    /// <summary>Bot déterministe qui se débarrasse des cartes par hauteur minimale.</summary>
    public sealed class DiscardBot : IPlayerAgent
    {
        private const int LargeHandThreshold = 4;
        private const int OpponentEndgameThreshold = 3;

        private readonly ICardPlayabilityChecker playabilityChecker;

        public DiscardBot(ICardPlayabilityChecker playabilityChecker)
        {
            this.playabilityChecker = playabilityChecker
                ?? throw new ArgumentNullException(nameof(playabilityChecker));
        }

        public Play? ProposeNormalPlay(IPlayerVisibleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.SelfHand.Count == 0) return null;

            var playable = state.SelfHand
                .Select((card, index) => new IndexedCard(card, index))
                .Where(x => playabilityChecker.IsPlayable(x.Card, state))
                .ToList();
            if (playable.Count == 0) return null;

            bool hasJoker = state.SelfHand.Any(c => c.IsJoker);
            var standard = playable.Where(x => !x.Card.IsJoker && x.Card.Rank.HasValue).ToList();

            // Le 2 est l'avant-dernier recours : avec deux cartes dont un 2,
            // ou trois cartes dont deux 2, il est joué seul avant l'autre hauteur.
            int twosInHand = state.SelfHand.Count(c => !c.IsJoker && c.Rank == DefRank.Two);
            if (!hasJoker && ((state.SelfHand.Count == 2 && twosInHand >= 1)
                              || (state.SelfHand.Count == 3 && twosInHand >= 2)))
            {
                var two = playable.FirstOrDefault(x => !x.Card.IsJoker && x.Card.Rank == DefRank.Two);
                if (two.Card != null) return SingleIfNotTerminal(state, two);
            }

            if (standard.Count > 0)
                return ProposeStandard(state, standard, hasJoker);

            // Les Jokers sont des recours ultimes, dans l'ordre Noir, Verre, Couleur.
            var joker = playable
                .Where(x => x.Card.IsJoker && x.Card.JokerType.HasValue)
                .OrderBy(x => JokerPriority(x.Card.JokerType!.Value))
                .ThenBy(x => x.Index)
                .FirstOrDefault();
            return joker.Card == null
                ? null
                : Play.CreateSingle(state.SelfPlayerId, joker.Card, CardLayer.Hand);
        }

        private Play? ProposeStandard(IPlayerVisibleState state, List<IndexedCard> standard, bool hasJoker)
        {
            DefRank lowestPlayable = standard
                .Select(x => x.Card.Rank!.Value)
                .OrderBy(r => (int)r)
                .First();

            // Sécurité : sans Joker, si le rang le plus haut de la main est
            // dupliqué ET jouable, on le vide carte par carte. Jamais de
            // sélection hors de `standard` (cause du plantage historique).
            DefRank target = lowestPlayable;
            bool singleOnly = false;
            if (!hasJoker)
            {
                var ranksInHand = state.SelfHand
                    .Where(c => !c.IsJoker && c.Rank.HasValue)
                    .Select(c => c.Rank!.Value)
                    .ToList();
                if (ranksInHand.Count > 0)
                {
                    DefRank highestInHand = ranksInHand.OrderByDescending(r => (int)r).First();
                    bool duplicated = ranksInHand.Count(r => r == highestInHand) > 1;
                    bool playableNow = standard.Any(x => x.Card.Rank == highestInHand);
                    if (duplicated && playableNow)
                    {
                        target = highestInHand;
                        singleOnly = true;
                    }
                }
            }

            var sameRank = standard
                .Where(x => x.Card.Rank == target)
                .OrderBy(x => x.Card.Suit.HasValue ? (int)x.Card.Suit.Value : int.MaxValue)
                .ThenBy(x => x.Index)
                .ToList();
            if (sameRank.Count == 0) return null;

            var chosen = sameRank[0];

            // Le 2 ne doit jamais être la dernière carte consommée.
            if (chosen.Card.Rank == DefRank.Two)
                return SingleIfNotTerminal(state, chosen);

            // Anti-blocage : un As n'est pas joué en groupe tant que la main est
            // grande et qu'aucun adversaire n'approche de la fin.
            if (chosen.Card.Rank == DefRank.Ace
                && state.SelfHand.Count > LargeHandThreshold
                && !OpponentNearEnd(state))
                singleOnly = true;

            if (singleOnly)
                return Play.CreateSingle(state.SelfPlayerId, chosen.Card, CardLayer.Hand);

            return Play.Create(
                state.SelfPlayerId,
                sameRank.Take(4).Select(x => x.Card),
                CardLayer.Hand);
        }

        private static bool OpponentNearEnd(IPlayerVisibleState state)
        {
            return state.Opponents.Any(o =>
                o.CurrentPhase != DefPhase.Finished
                && o.HandCount + o.FaceUp.Count + o.FaceDownCount <= OpponentEndgameThreshold);
        }

        private Play? SingleIfNotTerminal(IPlayerVisibleState state, IndexedCard chosen)
        {
            // Un 2 qui viderait les trois couches est interdit : on laisse le
            // Core déclencher le ramassage/fallback.
            if (state.SelfHand.Count == 1 && state.SelfFaceUp.Count == 0 && state.SelfFaceDown.Count == 0)
                return null;
            return Play.CreateSingle(state.SelfPlayerId, chosen.Card, CardLayer.Hand);
        }

        public bool DecidePickup(IPlayerVisibleState state, bool isForced)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return isForced;
        }

        /// <summary>Choisit toujours la position 0, à l'aveugle (aucun contenu accessible).</summary>
        public int? ProposeFaceDownIndex(IFaceDownBlindView view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            return view.SelfFaceDownCount == 0 ? (int?)null : 0;
        }

        public GiftResolutionChoice ResolveGift(IPlayerVisibleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.SelfHand.Count == 0)
                throw new InvalidOperationException("Impossible de résoudre le Don : la main du joueur est vide.");

            IndexedCard smallest = state.SelfHand
                .Select((card, index) => new IndexedCard(card, index))
                .OrderBy(x => x.Card.IsJoker ? 1 : 0)
                .ThenBy(x => x.Card.Rank.HasValue ? (int)x.Card.Rank.Value : int.MaxValue)
                .ThenBy(x => x.Card.Suit.HasValue ? (int)x.Card.Suit.Value : int.MaxValue)
                .ThenBy(x => x.Index)
                .First();

            var recipient = state.Opponents
                .Where(o => o.CurrentPhase != DefPhase.Finished)
                .OrderBy(o => o.FaceDownCount)
                .ThenBy(o => o.FaceUp.Count)
                .ThenBy(o => o.HandCount)
                .ThenBy(o => o.PlayerId)
                .FirstOrDefault();
            if (recipient == null)
                throw new InvalidOperationException("Impossible de résoudre le Don : aucun adversaire actif disponible.");
            return new GiftResolutionChoice(smallest.Card, recipient.PlayerId);
        }

        public IReadOnlyList<ExchangeChoice> ProposeExchanges(IPlayerVisibleState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return Array.Empty<ExchangeChoice>();
        }

        private static int JokerPriority(DefJokerType type)
        {
            switch (type)
            {
                case DefJokerType.Black: return 0;
                case DefJokerType.Glass: return 1;
                case DefJokerType.Color: return 2;
                default: return int.MaxValue;
            }
        }

        private readonly struct IndexedCard
        {
            public Card Card { get; }
            public int Index { get; }
            public IndexedCard(Card card, int index) { Card = card; Index = index; }
        }
    }
}