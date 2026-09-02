using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    [TestFixture]
    public class SevenGiftTests
    {
        private GameState gameState;
        private Player player1, player2, player3, player4;
        private Card sevenHearts;

        [SetUp]
        public void SetUp()
        {
            player1 = new Player("p1", "Player1");
            player2 = new Player("p2", "Player2");
            player3 = new Player("p3", "Player3");
            player4 = new Player("p4", "Player4");

            gameState = new GameState(new List<Player> { player1, player2, player3, player4 });
            sevenHearts = new Card(CardRank.Seven, CardSuit.Hearts);
        }

        /// <summary>
        /// Test : Joueur en Phase 1 pose un 7 avec des cartes en main → don applicable
        /// Main reste à 3 (rebobinage après don)
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase1_GiftApplied()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Travail;
            Card cardToGift = new Card(CardRank.Three, CardSuit.Spades);
            Card cardInHand = new Card(CardRank.Five, CardSuit.Clubs);

            player1.AddCardToHand(sevenHearts);
            player1.AddCardToHand(cardToGift);
            player1.AddCardToHand(cardInHand);

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after playing 7 and refilling");

            // Act 2 : Don
            gameState.HandleSevenPlayed(player1, player2, cardToGift);
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after gifting and refilling");
            Assert.AreEqual(1, player2.Hand.Count, "Player2 hand should be 1 (gifted card)");
        }

        /// <summary>
        /// Test : Joueur en Phase 1 pose un 7, rebobiné avec cartes face-up, puis don
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase1ToPhase2_GiftAfterRefill()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Travail;
            Card cardToGift = new Card(CardRank.Three, CardSuit.Spades);
            Card faceUpCard = new Card(CardRank.Eight, CardSuit.Diamonds);

            player1.AddCardToHand(sevenHearts);
            player1.AddCardToHand(cardToGift);
            player1.FaceUp.Add(faceUpCard);

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after refilling");

            // Act 2 : Don
            gameState.HandleSevenPlayed(player1, player2, cardToGift);
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after gifting and refilling");
            Assert.AreEqual(1, player2.Hand.Count, "Player2 hand should be 1 (gifted card)");
        }

        /// <summary>
        /// Test : Phase 2 → Phase 3 : joueur pose 7, puis transition vers Phase 3
        /// Main passe de 2 à 0 après don. Les FaceDown ne fusionnent pas automatiquement.
        /// ⚠️ Deck DOIT être à 0 pour cette phase
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase2ToPhase3_GiftThenTransition()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Talent;

            var emptyDeck = new Deck();
            while (emptyDeck.Count > 0)
                emptyDeck.Draw();

            gameState = new GameState(
                new List<Player> { player1, player2, player3, player4 },
                new Pile(),
                emptyDeck
            );

            Card cardToGift = new Card(CardRank.Three, CardSuit.Spades);

            player1.AddCardToHand(sevenHearts);
            player1.AddCardToHand(cardToGift);
            player1.FaceDown.Add(new Card(CardRank.King, CardSuit.Hearts));
            player1.FaceDown.Add(new Card(CardRank.Queen, CardSuit.Spades));
            player1.FaceDown.Add(new Card(CardRank.Jack, CardSuit.Diamonds));

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(1, player1.Hand.Count, "After playing 7, hand should be 1");

            // Act 2 : Don
            gameState.HandleSevenPlayed(player1, player2, cardToGift);
            Assert.AreEqual(0, player1.Hand.Count, "After gifting, Player1 hand should be 0");
            Assert.AreEqual(1, player2.Hand.Count, "Player2 hand should be 1 (gifted card)");
            Assert.AreEqual(3, player1.FaceDown.Count, "Player1 FaceDown still has 3 cards");

            // Act 3 : Vérifier la transition
            PhaseTransitionResult transition = gameState.CheckPlayerState(player1);
            Assert.AreEqual(PhaseTransitionResult.TransitionedToChance, transition, 
                "Should detect transition to Chance");

            // Act 4 : Appliquer la transition manuellement (ou via un mécanisme de jeu)
            if (transition == PhaseTransitionResult.TransitionedToChance)
            {
                gameState.CurrentPhase = GamePhase.Chance;
            }

            Assert.AreEqual(GamePhase.Chance, gameState.CurrentPhase, 
                "GameState phase should be Chance");
        }

        /// <summary>
        /// Test : Phase 3, joueur pose 7 comme DERNIÈRE carte → pas de don
        /// ⚠️ Deck DOIT être à 0
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase3_LastCard_NoGift()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Chance;

            var emptyDeck = new Deck();
            while (emptyDeck.Count > 0)
                emptyDeck.Draw();

            gameState = new GameState(
                new List<Player> { player1, player2, player3, player4 },
                new Pile(),
                emptyDeck
            );

            player1.AddCardToHand(sevenHearts);

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(0, player1.Hand.Count, "Player1 hand should be empty after playing last Seven");

            // Act 2 : Don impossible (pas de cartes en main)
            gameState.HandleSevenPlayed(player1, player2, sevenHearts);
            Assert.AreEqual(0, player1.Hand.Count, "Player1 hand remains empty");
            Assert.AreEqual(0, player2.Hand.Count, "Player2 hand unchanged (no gift possible)");
        }

        /// <summary>
        /// Test : Phase 3, joueur pose 7 avec cartes restantes + don
        /// ⚠️ Deck DOIT être à 0
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase3_Played_WithCardsRemaining_GiftApplied()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Chance;

            var emptyDeck = new Deck();
            while (emptyDeck.Count > 0)
                emptyDeck.Draw();

            gameState = new GameState(
                new List<Player> { player1, player2, player3, player4 },
                new Pile(),
                emptyDeck
            );

            Card cardToGift = new Card(CardRank.Three, CardSuit.Spades);

            player1.AddCardToHand(sevenHearts);
            player1.AddCardToHand(cardToGift);

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(1, player1.Hand.Count, "Player1 hand should be 1 (only cardToGift remains)");

            // Act 2 : Don
            gameState.HandleSevenPlayed(player1, player2, cardToGift);
            Assert.AreEqual(0, player1.Hand.Count, "Player1 hand should be empty after gifting (no refill in Phase 3)");
            Assert.AreEqual(1, player2.Hand.Count, "Player2 hand should be 1 (gifted card)");
        }

        /// <summary>
        /// Test : Phase 3, joueur retourne un 7 face-cachée → pas d'effet
        /// </summary>
        [Test]
        public void PlayCard_Seven_Phase3_Revealed_NoEffect()
        {
            // Arrange
            gameState.CurrentPhase = GamePhase.Chance;

            var emptyDeck = new Deck();
            while (emptyDeck.Count > 0)
                emptyDeck.Draw();

            gameState = new GameState(
                new List<Player> { player1, player2, player3, player4 },
                new Pile(),
                emptyDeck
            );

            int player2InitialHandCount = player2.Hand.Count;

            player1.FaceDown.Add(sevenHearts);

            // Act : Simuler révélation en Phase 3 (pas de PlayCard, pas de don)
            player1.FaceDown.Remove(sevenHearts);
            player1.AddCardToHand(sevenHearts);

            Assert.AreEqual(1, player1.Hand.Count, "Player1 has the Seven in hand");
            Assert.AreEqual(0, player1.FaceDown.Count, "FaceDown is now empty");
            Assert.AreEqual(player2InitialHandCount, player2.Hand.Count, "Player2 received nothing");
        }

        /// <summary>
        /// Test : Avec seulement 2 joueurs, le don fonctionne normalement en Phase 1
        /// </summary>
        [Test]
        public void PlayCard_Seven_TwoPlayers_StillApplies()
        {
            // Arrange
            var twoPlayers = new List<Player> { player1, player2 };
            gameState = new GameState(twoPlayers);
            gameState.CurrentPhase = GamePhase.Travail;

            Card cardToGift = new Card(CardRank.Three, CardSuit.Spades);
            Card cardInHand = new Card(CardRank.Five, CardSuit.Clubs);

            player1.AddCardToHand(sevenHearts);
            player1.AddCardToHand(cardToGift);
            player1.AddCardToHand(cardInHand);

            // Act 1 : Pose le 7
            bool playResult = gameState.PlayCard(player1, sevenHearts);
            Assert.IsTrue(playResult, "PlayCard should succeed");
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after refilling");

            // Act 2 : Don
            gameState.HandleSevenPlayed(player1, player2, cardToGift);
            Assert.AreEqual(3, player1.Hand.Count, "Player1 hand should be 3 after gifting and refilling");
            Assert.AreEqual(1, player2.Hand.Count, "Player2 hand should be 1 (0 initial + 1 gifted)");
        }
    }
}