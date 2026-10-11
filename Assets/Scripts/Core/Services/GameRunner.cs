#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.Exchange;
using Psycko.Core.Services.Log;

namespace Psycko.Core.Services
{
    /// <summary>Résultat d'une partie jouée par GameRunner.</summary>
    public sealed class GameRunOutcome
    {
        public GameResult Result { get; }
        /// <summary>Null si le log n'a pas été demandé.</summary>
        public GameLog? Log { get; }
        /// <summary>Exception ayant interrompu la partie (null si OK).</summary>
        public Exception? Error { get; }

        public GameRunOutcome(GameResult result, GameLog? log, Exception? error)
        {
            Result = result;
            Log = log;
            Error = error;
        }
    }

    /// <summary>
    /// Joue une partie complète : seed → Deck → distribution → échange → boucle de tours → GameResult.
    /// Une seule boucle pour Console, Unity et réseau. Aucune règle ici : tout passe par
    /// PreGameExchange et GameOrchestrator. Les agents sont indexés par siège.
    /// La seed ne reproduit QUE la distribution ; le GameLog trace les décisions.
    /// </summary>
    public static class GameRunner
    {
        public const int DefaultMoveCap = 250_000;

        public static GameRunOutcome Run(
            int seed,
            IReadOnlyList<IPlayerAgent> agents,
            IReadOnlyList<string> names,
            bool recordLog,
            int moveCap = DefaultMoveCap)
        {
            if (agents is null) throw new ArgumentNullException(nameof(agents));
            if (names is null) throw new ArgumentNullException(nameof(names));
            if (agents.Count != names.Count)
                throw new ArgumentException("Un nom par agent est requis.", nameof(names));

            int playerCount = agents.Count;
            var orchestrator = new GameOrchestrator();
            var tracker = new GameResultTracker(seed, playerCount);
            var recorder = recordLog ? new GameLogRecorder() : null;

            GameState? state = null;

            try
            {
                // 1. Deck mélangé via la seed, distribution
                var deck = Deck.CreateFull();
                deck.Shuffle(GameSeed.CreateRandom(seed));

                var seats = new List<(int Id, string Name)>(playerCount);
                for (int i = 0; i < playerCount; i++)
                    seats.Add((i, names[i]));

                var exchange = CardDealer.Deal(deck, seats);

                // 2. Échange pré-partie (non loggé)
                exchange = RunExchange(exchange, agents);

                // 3. GameState de départ
                state = PreGameExchange.CreateGameState(exchange);

                var firstSeatIndex = FirstPlayerResolver.Resolve(state.Players);
                recorder?.Record(GameLogEntry.GameStart(
                    state.Players[firstSeatIndex].Id,
                    "Smallest card in hand"));

                tracker.ObserveState(state);

                // 4. Boucle de tours
                while (!GameResultCalculator.IsGameOver(state))
                {
                    if (tracker.TotalMovesSoFar >= moveCap)
                        throw new InvalidOperationException(
                            $"Plafond anti-boucle atteint ({moveCap} coups).");

                    state = PlayOneTurn(state, agents, orchestrator, recorder, tracker);
                }

                var result = tracker.Build(state);
                return new GameRunOutcome(result, BuildLog(seed, recorder), null);
            }
            catch (Exception ex)
            {
                recorder?.Record(GameLogEntry.GameEnd(null));
                return new GameRunOutcome(
                    GameResult.Error(seed, playerCount),
                    BuildLog(seed, recorder),
                    ex);
            }
        }

        // ── Échange ──────────────────────────────────────────────────────

        private static PreGameExchangeState RunExchange(
            PreGameExchangeState exchange, IReadOnlyList<IPlayerAgent> agents)
        {
            for (int seat = 0; seat < agents.Count; seat++)
            {
                int playerId = exchange.Players[seat].Id;
                var view = PlayerVisibleState.FromExchange(exchange, playerId);
                var choices = agents[seat].ProposeExchanges(view);

                foreach (var choice in choices)
                {
                    var resolution = PreGameExchange.Swap(exchange, playerId, choice);
                    if (resolution.IsAccepted)
                        exchange = resolution.State;
                }

                var ready = PreGameExchange.MarkReady(exchange, playerId);
                if (ready.IsAccepted)
                    exchange = ready.State;
            }

            return PreGameExchange.Finish(exchange);
        }

        // ── Un tour ──────────────────────────────────────────────────────

        private static GameState PlayOneTurn(
            GameState state,
            IReadOnlyList<IPlayerAgent> agents,
            GameOrchestrator orchestrator,
            IGameLogRecorder? recorder,
            GameResultTracker tracker)
        {
            int seat = state.ActivePlayerIndex;
            int playerId = state.Players[seat].Id;

            while (true)
            {
                // Recalculer l'état du joueur À CHAQUE ITÉRATION
                var player = state.Players[seat];
                var agent = agents[seat];
                var view = PlayerVisibleState.From(state, player.Id);

                // ─ Phase Luck avec main vide : proposer une carte FaceDown
                if (player.CurrentPhase == DefPhase.Luck && player.Hand.Count == 0)
                {
                    // Le joueur doit avoir au moins 1 FaceDown disponible
                    if (player.FaceDown.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Phase Luck mais FaceDown vide (joueur {player.Id})");
                    }

                    var blind = agent.ProposeFaceDownPlay(view);
                    int faceDownIndex = 0;

                    if (blind is not null)
                    {
                        int found = player.FaceDown.ToList().IndexOf(blind.Cards[0]);
                        if (found >= 0) faceDownIndex = found;
                    }

                    // LOG
                    System.Console.WriteLine(
                        $"[LUCK] seat={seat}, player.Id={player.Id}, " +
                        $"proposed index={faceDownIndex}, FaceDown.Count={player.FaceDown.Count}, " +
                        $"blind card={blind?.Cards[0]?.Rank}/{blind?.Cards[0]?.Suit}");

                    var resolution = orchestrator.ApplyBlindPlay(
                        state, seat, faceDownIndex, recorder, tracker);

                    if (!resolution.Result.Success)
                    {
                        // Carte FaceDown ne convient pas → ramassage forcé
                        // La main s'est reconstituée, on continue la boucle
                        state = Require(resolution.Result, "ApplyBlindPlay");
                        continue;  // ← Retour à la boucle : main NON vide maintenant
                    }

                    // Succès : on termine ce tour
                    return Require(resolution.Result, "ApplyBlindPlay");
                }

                // ─ Phase Luck avec main NON vide (après ramassage) : proposer coup normal
                if (player.CurrentPhase == DefPhase.Luck && player.Hand.Count > 0)
                {
                    var proposed = agent.ProposeNormalPlay(view);

                    if (proposed is null)
                    {
                        var pickup = GameOrchestrator.RequestPickup(state, player.Id, recorder, tracker);
                        state = Require(pickup, "RequestPickup");
                        continue;  // ← Retour à la boucle : vérifier main/phase à nouveau
                    }

                    var played = orchestrator.ApplyPlay(state, proposed, seat, recorder, tracker);

                    if (!played.Success)
                    {
                        // Coup rejeté → ramassage volontaire
                        var fallback = GameOrchestrator.RequestPickup(state, player.Id, recorder, tracker);
                        state = Require(fallback, "RequestPickup (fallback)");
                        continue;  // ← Retour à la boucle
                    }

                    // Coup accepté → traiter le Don le cas échéant
                    if (played.PendingGift.HasValue)
                    {
                        var pending = played.PendingGift.Value;
                        var donor = pending.State.Players[pending.State.ActivePlayerIndex];
                        var giftView = PlayerVisibleState.From(pending.State, donor.Id);
                        var choice = agent.ResolveGift(giftView);

                        var resolved = orchestrator.ResolveGiftAndContinue(
                            state, proposed, pending, choice, recorder, tracker);
                        state = Require(resolved, "ResolveGiftAndContinue");
                        continue;  // ← Retour à la boucle : vérifier phase à nouveau
                    }

                    // Coup accepté, pas de Don → on termine ce tour
                    return played.NewState;
                }

                // ─ Phases Work / Talent : proposer coup ou ramassage
                var proposedNormal = agent.ProposeNormalPlay(view);

                if (proposedNormal is null)
                {
                    var pickup = GameOrchestrator.RequestPickup(state, player.Id, recorder, tracker);
                    return Require(pickup, "RequestPickup");
                }

                var playedNormal = orchestrator.ApplyPlay(state, proposedNormal, seat, recorder, tracker);

                if (!playedNormal.Success)
                {
                    // Coup rejeté → ramassage volontaire
                    var fallback = GameOrchestrator.RequestPickup(state, player.Id, recorder, tracker);
                    return Require(fallback, "RequestPickup (fallback)");
                }

                // Coup accepté → traiter le Don le cas échéant
                if (playedNormal.PendingGift.HasValue)
                {
                    var pending = playedNormal.PendingGift.Value;
                    var donor = pending.State.Players[pending.State.ActivePlayerIndex];
                    var giftView = PlayerVisibleState.From(pending.State, donor.Id);
                    var choice = agent.ResolveGift(giftView);

                    var resolved = orchestrator.ResolveGiftAndContinue(
                        state, proposedNormal, pending, choice, recorder, tracker);
                    return Require(resolved, "ResolveGiftAndContinue");
                }

                // Coup accepté, pas de Don → on termine ce tour
                return playedNormal.NewState;
            }
        }

        private static GameState Require(PlayResult result, string operation)
        {
            if (!result.Success)
                throw new InvalidOperationException(
                    $"{operation} rejeté : {result.RejectionReason}");
            return result.NewState;
        }

        private static GameLog? BuildLog(int seed, GameLogRecorder? recorder)
            => recorder is null ? null : new GameLog(seed, recorder.Entries);
    }
}