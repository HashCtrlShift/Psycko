using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    [TestFixture]
    public class DoubletAndSquareTests
    {
        private GameState CreateGameState(int playerCount = 4)
        {
            var players = new List<Player>();
            for (int i = 0; i < playerCount; i++)
                players.Add(new Player($"p{i}", $"Player{i}"));

            return new GameState(players, new Pile(), new Deck());
        }

        // ---------------------------------------------------------
        // DOUBLON
        // ---------------------------------------------------------

        [Test]
        public void DetectDoublet_TwoConsecutiveSameRank_ReturnsTrue()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Seven, CardSuit.Spades));

            Assert.IsTrue(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_TwoDifferentRanks_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Eight, CardSuit.Spades));

            Assert.IsFalse(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_SingleCardInPile_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));

            Assert.IsFalse(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_EmptyPile_ReturnsFalse()
        {
            var gameState = CreateGameState();

            Assert.IsFalse(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_GlassJokerBetweenSameRanks_IsTransparent_ReturnsTrue()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Nine, CardSuit.Hearts));
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(CardRank.Nine, CardSuit.Clubs));

            Assert.IsTrue(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_BlackJokerBetweenSameRanks_BreaksChain_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Nine, CardSuit.Hearts));
            gameState.Pile.Add(new Card(JokerType.Black));
            gameState.Pile.Add(new Card(CardRank.Nine, CardSuit.Clubs));

            Assert.IsFalse(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_MultipleGlassJokersStacked_StillTransparent_ReturnsTrue()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.King, CardSuit.Hearts));
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(CardRank.King, CardSuit.Diamonds));

            Assert.IsTrue(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_OnlyTwoActivePlayersLeft_IsDisabled_ReturnsFalse()
        {
            var gameState = CreateGameState(4);
            // 2 joueurs ont déjà gagné => 2 actifs restants
            gameState.Players[0].HasWon = true;
            gameState.Players[1].HasWon = true;

            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Spades));

            Assert.IsFalse(gameState.DetectDoublet(gameState.Pile));
        }

        [Test]
        public void DetectDoublet_MoreThanTwoActivePlayers_StillActive_ReturnsTrue()
        {
            var gameState = CreateGameState(4);
            gameState.Players[0].HasWon = true; // 3 actifs restants

            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Spades));

            Assert.IsTrue(gameState.DetectDoublet(gameState.Pile));
        }

        // ---------------------------------------------------------
        // CARRÉ
        // ---------------------------------------------------------

        [Test]
        public void DetectSquare_FourConsecutiveSameRank_ReturnsTrue()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Spades));
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Diamonds));
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Clubs));

            Assert.IsTrue(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_ThreeSameRankOnly_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Spades));
            gameState.Pile.Add(new Card(CardRank.Jack, CardSuit.Diamonds));

            Assert.IsFalse(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_GlassJokersInterleaved_StillCompletesSquare_ReturnsTrue()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Hearts));
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Spades));
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Diamonds));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Clubs));

            Assert.IsTrue(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_BlackJokerInterleaved_BreaksSquare_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Spades));
            gameState.Pile.Add(new Card(JokerType.Black));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Diamonds));
            gameState.Pile.Add(new Card(CardRank.Queen, CardSuit.Clubs));

            Assert.IsFalse(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_RemainsActiveWithOnlyTwoActivePlayers_ReturnsTrue()
        {
            var gameState = CreateGameState(4);
            gameState.Players[0].HasWon = true;
            gameState.Players[1].HasWon = true; // 2 actifs restants

            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Spades));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Diamonds));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Clubs));

            // Contrairement au Doublon, le Carré reste actif même à 2 joueurs
            Assert.IsTrue(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_EmptyPile_ReturnsFalse()
        {
            var gameState = CreateGameState();

            Assert.IsFalse(gameState.DetectSquare(gameState.Pile));
        }

        [Test]
        public void DetectSquare_FewerThanFourCardsTotal_ReturnsFalse()
        {
            var gameState = CreateGameState();
            gameState.Pile.Add(new Card(CardRank.Ace, CardSuit.Hearts));
            gameState.Pile.Add(new Card(JokerType.Glass));

            Assert.IsFalse(gameState.DetectSquare(gameState.Pile));
        }
    }
}