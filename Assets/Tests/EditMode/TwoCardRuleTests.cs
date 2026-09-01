using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    public class TwoCardRuleTests
    {
        private Player _player1;
        private Player _player2;
        private Player _player3;
        private List<Player> _players;

        [SetUp]
        public void SetUp()
        {
            _player1 = new Player("p1", "Alice");
            _player2 = new Player("p2", "Bob");
            _player3 = new Player("p3", "Carla");
            _players = new List<Player> { _player1, _player2, _player3 };
        }

        // ---------------------------------------------------------------
        // TurnManager.HandleTwoPlayed(bool) — tests directs
        // ---------------------------------------------------------------

        [Test]
        public void HandleTwoPlayed_NotLastCard_SamePlayerKeepsTurn()
        {
            var turnManager = new TurnManager(_players, _player1);

            turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: false);

            Assert.AreEqual(_player1, turnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void HandleTwoPlayed_IsLastCard_AdvancesToNextPlayer()
        {
            var turnManager = new TurnManager(_players, _player1);

            turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: true);

            Assert.AreEqual(_player2, turnManager.CurrentTurn.CurrentPlayer);
        }

        // ---------------------------------------------------------------
        // GameState.PlayCard -> HandleTwoCardPlayed — cas normal (NoChange)
        // ---------------------------------------------------------------

        [Test]
        public void PlayCard_TwoPlayed_NoChange_DestroysPileAndSamePlayerReplays()
        {
            var deck = new Deck(seed: 1);
            var pile = new Pile();
            var gameState = GameState.CreateWithTurnManager(
                _players, pile, deck, new TurnManager(_players, _player1));

            var two = new Card(CardRank.Two, CardSuit.Hearts);
            var filler = new Card(CardRank.Three, CardSuit.Hearts);

            _player1.AddCardToHand(two);
            _player1.AddCardToHand(filler); // il reste une carte après le 2 -> NoChange

            bool result = gameState.PlayCard(_player1, two);

            Assert.IsTrue(result);
            Assert.AreEqual(0, gameState.Pile.Count, "La pile doit être détruite après un 2 (cas normal).");
            Assert.AreEqual(_player1, gameState.TurnManager.CurrentTurn.CurrentPlayer,
                "Le même joueur doit rejouer après un 2 (cas normal).");
        }

        [Test]
        public void PlayCard_TwoPlayed_NoChange_TwoIsNotReturnedToPlayerHand()
        {
            var deck = new Deck(seed: 1);
            while (deck.Count > 0) deck.Draw();  // Vide le deck
            
            var pile = new Pile();
            var gameState = GameState.CreateWithTurnManager(
                _players, pile, deck, new TurnManager(_players, _player1));

            var two = new Card(CardRank.Two, CardSuit.Hearts);
            var filler = new Card(CardRank.Three, CardSuit.Hearts);

            _player1.AddCardToHand(two);
            _player1.AddCardToHand(filler);

            gameState.PlayCard(_player1, two);

            Assert.IsFalse(_player1.Hand.Contains(two), "Le 2 doit avoir disparu (pile détruite), pas revenir en main.");
            Assert.AreEqual(1, _player1.Hand.Count, "Seul le filler doit rester en main.");
        }

        // ---------------------------------------------------------------
        // GameState.PlayCard -> HandleTwoCardPlayed — cas dernière carte (transition)
        // ---------------------------------------------------------------

        [Test]
        public void PlayCard_TwoPlayed_LastCard_PlayerPicksUpPileInstead()
        {
            // Deck vide pour garantir qu'aucune pioche automatique ne vienne remplir la main
            var deck = new Deck(seed: 1);
            while (deck.Count > 0) deck.Draw();

            var pile = new Pile();
            var existingPileCard = new Card(CardRank.Four, CardSuit.Spades);
            pile.Add(existingPileCard);

            var gameState = GameState.CreateWithTurnManager(
                _players, pile, deck, new TurnManager(_players, _player1));

            var two = new Card(CardRank.Two, CardSuit.Hearts);
            _player1.AddCardToHand(two); // seule carte en main, pas de FaceUp/FaceDown -> transition (pas NoChange)

            bool result = gameState.PlayCard(_player1, two);

            Assert.IsTrue(result);
            Assert.AreEqual(0, gameState.Pile.Count, "La pile doit être entièrement ramassée par le joueur.");
            Assert.IsTrue(_player1.Hand.Contains(existingPileCard),
                "Le joueur doit récupérer les cartes de la pile en main.");
            Assert.IsTrue(_player1.Hand.Contains(two),
                "Le 2 lui-même doit revenir en main du joueur (ramassé avec la pile).");
        }

        [Test]
        public void PlayCard_TwoPlayed_LastCard_AdvancesToNextPlayer()
        {
            var deck = new Deck(seed: 1);
            while (deck.Count > 0) deck.Draw();

            var pile = new Pile();
            pile.Add(new Card(CardRank.Four, CardSuit.Spades));

            var gameState = GameState.CreateWithTurnManager(
                _players, pile, deck, new TurnManager(_players, _player1));

            var two = new Card(CardRank.Two, CardSuit.Hearts);
            _player1.AddCardToHand(two);

            gameState.PlayCard(_player1, two);

            Assert.AreEqual(_player2, gameState.TurnManager.CurrentTurn.CurrentPlayer,
                "Le joueur suivant doit ouvrir après ramassage forcé sur dernier 2.");
        }

        // ---------------------------------------------------------------
        // Cas limite : le 2 seul en pile vide, pas de carte existante à ramasser
        // ---------------------------------------------------------------

        [Test]
        public void PlayCard_TwoPlayed_LastCard_EmptyPileBeforeTwo_StillAdvancesAndClearsHand()
        {
            var deck = new Deck(seed: 1);
            while (deck.Count > 0) deck.Draw();

            var pile = new Pile(); // vide avant le coup
            var gameState = GameState.CreateWithTurnManager(
                _players, pile, deck, new TurnManager(_players, _player1));

            var two = new Card(CardRank.Two, CardSuit.Hearts);
            _player1.AddCardToHand(two);

            bool result = gameState.PlayCard(_player1, two);

            Assert.IsTrue(result);
            Assert.IsTrue(_player1.Hand.Contains(two),
                "Le 2 doit revenir en main du joueur puisqu'il n'y a rien d'autre à détruire.");
            Assert.AreEqual(_player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }
    }
}