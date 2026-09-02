using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    /// <summary>
    /// Tests pour la règle du 2 (CardRank.Two = 14) : destruction de pile, rejeu, ramassage forcé.
    /// Couvre TurnManager.HandleTwoPlayed() et GameState.PlayCard() → HandleTwoCardPlayed().
    /// </summary>
    [TestFixture]
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

        // ---------------------------------------------------------
        // TurnManager.HandleTwoPlayed(bool) — tests directs
        // ---------------------------------------------------------

        [Test]
        public void HandleTwoPlayed_NotLastCard_SamePlayerKeepsTurn()
        {
            var turnManager = new TurnManager(_players, _player1);

            turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: false);

            Assert.AreEqual(_player1, turnManager.CurrentTurn.CurrentPlayer,
                "Cas normal (NoChange) : le même joueur doit conserver son tour.");
        }

        [Test]
        public void HandleTwoPlayed_IsLastCard_AdvancesToNextPlayer()
        {
            var turnManager = new TurnManager(_players, _player1);

            turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: true);

            Assert.AreEqual(_player2, turnManager.CurrentTurn.CurrentPlayer,
                "Cas dernier 2 : joueur suivant commence après ramassage forcé.");
        }

        // ---------------------------------------------------------
        // GameState.PlayCard → HandleTwoCardPlayed — cas normal (NoChange)
        // ---------------------------------------------------------

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
            _player1.AddCardToHand(filler); // il reste une carte après le 2 → NoChange

            bool result = gameState.PlayCard(_player1, two);

            Assert.IsTrue(result);
            Assert.AreEqual(0, gameState.Pile.Count,
                "Pile détruite après un 2 (cas normal NoChange).");
            Assert.AreEqual(_player1, gameState.TurnManager.CurrentTurn.CurrentPlayer,
                "Même joueur rejoue après un 2 (cas normal NoChange).");
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

            Assert.IsFalse(_player1.Hand.Contains(two),
                "En cas NoChange : le 2 doit avoir disparu (pile détruite), pas revenir en main.");
            Assert.AreEqual(1, _player1.Hand.Count,
                "Seul le filler doit rester en main.");
        }

        // ---------------------------------------------------------
        // GameState.PlayCard → HandleTwoCardPlayed — cas dernier 2 (transition)
        // ---------------------------------------------------------

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
            _player1.AddCardToHand(two); // seule carte en main → transition (pas NoChange)

            bool result = gameState.PlayCard(_player1, two);

            Assert.IsTrue(result);
            Assert.AreEqual(0, gameState.Pile.Count,
                "Pile entièrement ramassée par le joueur.");
            Assert.IsTrue(_player1.Hand.Contains(existingPileCard),
                "Joueur récupère les cartes de la pile en main.");
            Assert.IsTrue(_player1.Hand.Contains(two),
                "Le 2 lui-même revient en main du joueur (ramassé avec la pile).");
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
                "Joueur suivant ouvre après ramassage forcé sur dernier 2.");
        }

        // ---------------------------------------------------------
        // Cas limite : 2 seul en pile vide, pas de carte existante à ramasser
        // ---------------------------------------------------------

        [Test]
        public void PlayCard_TwoPlayed_LastCard_EmptyPileBeforeTwo_StillAdvancesAndReturnsTwoToHand()
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
                "Le 2 revient en main du joueur (pile vide avant, rien d'autre à détruire).");
            Assert.AreEqual(_player2, gameState.TurnManager.CurrentTurn.CurrentPlayer,
                "Joueur suivant commence malgré pile vide.");
        }

        // ---------------------------------------------------------
        // CardRank.Two validation
        // ---------------------------------------------------------

        [Test]
        public void CardRank_TwoAlwaysEqualsFourteen()
        {
            Assert.AreEqual(14, (int)CardRank.Two,
                "CardRank.Two doit valoir 14 pour la hauteur de comparaison (même que K, A, mais spécial pour destruction).");
        }
    }
}