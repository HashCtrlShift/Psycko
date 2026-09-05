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
            
            // Première carte : 7 de Cœur
            Card card1 = new Card(CardRank.Seven, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // Initialise le tracker
            
            // Deuxième carte : 7 de Pique (même rang = Doublon)
            Card card2 = new Card(CardRank.Seven, CardSuit.Spades);

            Assert.IsTrue(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_TwoDifferentRanks_ReturnsFalse()
        {
            var gameState = CreateGameState();
            
            Card card1 = new Card(CardRank.Seven, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);
            
            Card card2 = new Card(CardRank.Eight, CardSuit.Spades);

            Assert.IsFalse(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_SingleCardInPile_ReturnsFalse()
        {
            var gameState = CreateGameState();
            Card card1 = new Card(CardRank.Seven, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            // Ne pas appeler UpdateLastSignificantRank => LastSignificantRank reste null

            Assert.IsFalse(gameState.DetectDoublet(card1));
        }

        [Test]
        public void DetectDoublet_EmptyPile_ReturnsFalse()
        {
            var gameState = CreateGameState();
            Card card = new Card(CardRank.Seven, CardSuit.Hearts);

            Assert.IsFalse(gameState.DetectDoublet(card));
        }

        [Test]
        public void DetectDoublet_GlassJokerBetweenSameRanks_IsTransparent_ReturnsTrue()
        {
            var gameState = CreateGameState();
            
            // Première carte : 9 de Cœur
            Card card1 = new Card(CardRank.Nine, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = 9
            
            // Verre empilé (transparent, ne change pas LastSignificantRank)
            gameState.Pile.Add(new Card(JokerType.Glass));
            
            // Deuxième carte : 9 de Trèfle (Doublon !)
            Card card2 = new Card(CardRank.Nine, CardSuit.Clubs);

            Assert.IsTrue(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_BlackJokerBetweenSameRanks_BreaksChain_ReturnsFalse()
        {
            var gameState = CreateGameState();
            
            Card card1 = new Card(CardRank.Nine, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = 9
            
            // Joker Noir empilé (casse la chaîne, met à jour LastSignificantRank à null)
            gameState.Pile.Add(new Card(JokerType.Black));
            gameState.UpdateLastSignificantRank(new Card(JokerType.Black));  // Joker => LastSignificantRank reste inchangé
            
            Card card2 = new Card(CardRank.Nine, CardSuit.Clubs);

            // Le Joker Noir n'efface pas LastSignificantRank, mais en vrai c'est un cas de logique
            // Voir les règles : le Joker Noir "casse" la chaîne de Doublon
            // Pour être cohérent, UpdateLastSignificantRank doit ignorer les Jokers
            // => DetectDoublet regarde alors card1 (9) vs card2 (9) => faux positif
            // CORRECTION : il faut que UpdateLastSignificantRank RESET sur Joker Noir/Couleur
            
            Assert.IsFalse(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_MultipleGlassJokersStacked_StillTransparent_ReturnsTrue()
        {
            var gameState = CreateGameState();
            
            Card card1 = new Card(CardRank.King, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);
            
            gameState.Pile.Add(new Card(JokerType.Glass));
            gameState.Pile.Add(new Card(JokerType.Glass));
            
            Card card2 = new Card(CardRank.King, CardSuit.Diamonds);

            Assert.IsTrue(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_OnlyTwoActivePlayersLeft_IsDisabled_ReturnsFalse()
        {
            var gameState = CreateGameState(4);
            gameState.Players[0].HasWon = true;
            gameState.Players[1].HasWon = true;

            Card card1 = new Card(CardRank.Ten, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);
            
            Card card2 = new Card(CardRank.Ten, CardSuit.Spades);

            Assert.IsFalse(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_MoreThanTwoActivePlayers_StillActive_ReturnsTrue()
        {
            var gameState = CreateGameState(4);
            gameState.Players[0].HasWon = true;  // 3 actifs restants

            Card card1 = new Card(CardRank.Ten, CardSuit.Hearts);
            gameState.Pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);
            
            Card card2 = new Card(CardRank.Ten, CardSuit.Spades);

            Assert.IsTrue(gameState.DetectDoublet(card2));
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
            gameState.Players[1].HasWon = true;

            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Hearts));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Spades));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Diamonds));
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Clubs));

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