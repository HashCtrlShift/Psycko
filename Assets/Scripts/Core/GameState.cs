using System;
using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    public class GameState
    {
        public GamePhase CurrentPhase { get; set; }
        public List<Player> Players { get; }
        public Pile Pile { get; }
        public Deck Deck { get; }
        public TurnManager TurnManager { get; private set; }

        private readonly EffectResolver _effects;

        public ComparisonMode ActiveComparisonMode =>
            _effects.IsPriestActive ? ComparisonMode.LessOrEqual : ComparisonMode.GreaterOrEqual;

        public bool IsPriestActive => _effects.IsPriestActive;
        public int PriestHeightBlock => _effects.PriestHeightBlock;

        public GameState(
            IEnumerable<Player> players = null,
            Pile pile = null,
            Deck deck = null)
        {
            CurrentPhase = GamePhase.Travail;
            Players = players == null ? new List<Player>() : new List<Player>(players);
            Pile = pile ?? new Pile();
            Deck = deck ?? new Deck();
            TurnManager = new TurnManager(Players);
            _effects = new EffectResolver();
        }

        // ------------------------------------------------------------------
        // VALIDATION / LECTURE (délégation pure)
        // ------------------------------------------------------------------

        public bool IsPlayable(Card card)
        {
            return PlayValidator.IsPlayable(card, Pile, _effects.IsPriestActive, _effects.PriestHeightBlock);
        }

        public Card? GetEffectiveTopCard(Pile pile) => PileInspector.GetEffectiveTopCard(pile);

        public List<Card> GetSignificantCardsFromTop(Pile pile, int count) =>
            PileInspector.GetSignificantCardsFromTop(pile, count);

        public bool DetectSquare(Pile pile) => PileInspector.DetectSquare(pile);

        public void UpdateLastSignificantRank(Card card) => _effects.UpdateLastSignificantRank(card);

        public PhaseTransitionResult CheckPlayerState(Player player) =>
            PhaseController.CheckPlayerState(player, Deck);

        // ------------------------------------------------------------------
        // PIOCHE / RAMASSAGE
        // ------------------------------------------------------------------

        public bool PickUpPile(Player player)
        {
            if (player == null || Pile.IsEmpty())
                return false;

            while (!Pile.IsEmpty())
            {
                player.AddCardToHand(Pile.Pop());
            }

            _effects.ResetPriestBlock();
            _effects.ResetDoubletTracker();
            return true;
        }

        public void RefillHand(Player player)
        {
            if (player == null)
                return;

            while (player.Hand.Count < 3 && Deck.Count > 0)
            {
                player.AddCardToHand(Deck.Draw());
            }
        }

        // ------------------------------------------------------------------
        // POSE DE CARTE — orchestration seule, effets délégués
        // ------------------------------------------------------------------

        public bool PlayCard(Player player, Card card)
        {
            if (player == null || !player.Hand.Contains(card) || !IsPlayable(card))
                return false;

            // Pose
            player.RemoveCardFromHand(card);
            Pile.Add(card);

            // Consommation de la contrainte Prêtre (Joker de Verre transparent)
            if (_effects.IsPriestActive && PlayValidator.IsSignificantForPriest(card))
            {
                _effects.ResetPriestBlock();
            }

            bool effectHandled = false;

            if (card.IsJoker && card.JokerType == JokerType.Color)
            {
                // Bombe : destruction + joueur suivant, jamais de rejeu
                _effects.ResolvePileDestruction(Pile, DestructionReason.Bomb);
                TurnManager.HandleBombPlayed();
                effectHandled = true;
            }
            else if (card.IsStandardCard && card.Rank == CardRank.Two)
            {
                PhaseTransitionResult state = CheckPlayerState(player);
                _effects.HandleTwoCardPlayed(player, Pile, TurnManager, state, PickUpPile);
                effectHandled = true;
            }
            else if (DetectSquare(Pile))
            {
                // Carré : pile détruite, même joueur rejoue (CurrentTurn inchangé)
                _effects.ResolvePileDestruction(Pile, DestructionReason.Square);
                effectHandled = true;
            }
            else if (_effects.DetectDoublet(card, TurnManager.GetActivePlayerCount()))
            {
                // Doublon : saute le joueur suivant
                TurnManager.AdvanceToNextPlayer();
                TurnManager.AdvanceToNextPlayer();
                effectHandled = true;
            }
            else if (card.IsStandardCard && card.Rank == CardRank.Jack)
            {
                _effects.HandleJackPlayed(TurnManager);
                TurnManager.AdvanceToNextPlayer();
                effectHandled = true;
            }

            if (!effectHandled)
            {
                TurnManager.AdvanceToNextPlayer();
            }

            RefillHand(player);
            _effects.UpdateLastSignificantRank(card);

            // Activation d'un nouveau Prêtre
            if (card.IsStandardCard && card.Rank == CardRank.Priest && !Pile.IsEmpty())
            {
                _effects.ActivatePriestBlock();
            }

            return true;
        }

        // ------------------------------------------------------------------
        // 7 (DON)
        // ------------------------------------------------------------------

        public void HandleSevenPlayed(Player playerWhoPlayed, Player nextPlayer, Card cardToGift)
        {
            _effects.HandleSevenPlayed(playerWhoPlayed, nextPlayer, cardToGift, CurrentPhase, RefillHand);
        }

        // ------------------------------------------------------------------
        // TRANSITIONS DE PHASE — délégation à PhaseController
        // ------------------------------------------------------------------

        /// <summary>
        /// Point d'entrée unique pour la console/présentation : applique toutes les
        /// transitions dues pour ce joueur (victoire, fusion, bascule, révélation Chance).
        /// </summary>
        /// <returns>true si le tour du joueur est déjà consommé (appelant doit "continue").</returns>
        public bool ApplyPhaseTransitions(Player player)
        {
            return PhaseController.ApplyAll(player, this);
        }

        /// <summary>
        /// Retourne le nombre de cartes en main du joueur.
        /// </summary>
        public int GetPlayerHandCount(Player player)
        {
            return player.Hand.Count;
        }

        // ------------------------------------------------------------------
        // FACTORY (tests)
        // ------------------------------------------------------------------

        public static GameState CreateWithTurnManager(
            IEnumerable<Player> players,
            Pile pile,
            Deck deck,
            TurnManager turnManager)
        {
            var gameState = new GameState(players, pile, deck);
            gameState.TurnManager = turnManager ?? new TurnManager(gameState.Players);
            return gameState;
        }
    }
}