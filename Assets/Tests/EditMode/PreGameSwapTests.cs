using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    public class PreGameSwapTests
    {
        private Player CreatePlayerWithHandAndFaceUp(string id, string name, Card[] hand, Card[] faceUp)
        {
            var player = new Player(id, name);
            foreach (Card c in hand) player.AddCardToHand(c);
            foreach (Card c in faceUp) player.AddCardFaceUp(c);
            return player;
        }

        // ------------------------------------------------------------------
        // SWAP
        // ------------------------------------------------------------------

        [Test]
        public void SwapHandWithFaceUp_ValidCards_SwapsCorrectly()
        {
            var handCard = new Card(CardRank.Five, CardSuit.Hearts);
            var faceUpCard = new Card(CardRank.King, CardSuit.Spades);

            var player = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { handCard, new Card(CardRank.Three, CardSuit.Clubs), new Card(CardRank.Ace, CardSuit.Diamonds) },
                new[] { faceUpCard, new Card(CardRank.Four, CardSuit.Clubs), new Card(CardRank.Seven, CardSuit.Diamonds) });

            bool result = PreGameSwap.SwapHandWithFaceUp(player, handCard, faceUpCard);

            Assert.IsTrue(result);
            Assert.IsFalse(player.Hand.Contains(handCard));
            Assert.IsTrue(player.Hand.Contains(faceUpCard));
            Assert.IsFalse(player.FaceUp.Contains(faceUpCard));
            Assert.IsTrue(player.FaceUp.Contains(handCard));
            Assert.AreEqual(3, player.Hand.Count);
            Assert.AreEqual(3, player.FaceUp.Count);
        }

        [Test]
        public void SwapHandWithFaceUp_CardNotInHand_ReturnsFalse()
        {
            var faceUpCard = new Card(CardRank.King, CardSuit.Spades);
            var notInHand = new Card(CardRank.Two, CardSuit.Hearts);

            var player = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(CardRank.Five, CardSuit.Hearts) },
                new[] { faceUpCard });

            bool result = PreGameSwap.SwapHandWithFaceUp(player, notInHand, faceUpCard);

            Assert.IsFalse(result);
        }

        [Test]
        public void SwapHandWithFaceUp_CardNotInFaceUp_ReturnsFalse()
        {
            var handCard = new Card(CardRank.Five, CardSuit.Hearts);
            var notInFaceUp = new Card(CardRank.Two, CardSuit.Hearts);

            var player = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { handCard },
                new[] { new Card(CardRank.King, CardSuit.Spades) });

            bool result = PreGameSwap.SwapHandWithFaceUp(player, handCard, notInFaceUp);

            Assert.IsFalse(result);
        }

        [Test]
        public void SwapHandWithFaceUp_RepeatedMultipleTimes_AllowsFullSwap()
        {
            var hand = new[]
            {
                new Card(CardRank.Three, CardSuit.Clubs),
                new Card(CardRank.Four, CardSuit.Clubs),
                new Card(CardRank.Five, CardSuit.Clubs)
            };
            var faceUp = new[]
            {
                new Card(CardRank.King, CardSuit.Spades),
                new Card(CardRank.Queen, CardSuit.Spades),
                new Card(CardRank.Jack, CardSuit.Spades)
            };

            var player = CreatePlayerWithHandAndFaceUp("p1", "Alice", hand, faceUp);

            // Swap les 3 cartes une par une
            Assert.IsTrue(PreGameSwap.SwapHandWithFaceUp(player, hand[0], faceUp[0]));
            Assert.IsTrue(PreGameSwap.SwapHandWithFaceUp(player, hand[1], faceUp[1]));
            Assert.IsTrue(PreGameSwap.SwapHandWithFaceUp(player, hand[2], faceUp[2]));

            Assert.AreEqual(3, player.Hand.Count);
            Assert.AreEqual(3, player.FaceUp.Count);
            CollectionAssert.AreEquivalent(faceUp, player.Hand);
            CollectionAssert.AreEquivalent(hand, player.FaceUp);
        }

        // ------------------------------------------------------------------
        // READY STATE
        // ------------------------------------------------------------------

        [Test]
        public void MarkReady_SetsPlayerReadyTrue()
        {
            var player = new Player("p1", "Alice");
            Assert.IsFalse(player.IsReady);

            PreGameSwap.MarkReady(player);

            Assert.IsTrue(player.IsReady);
        }

        [Test]
        public void AreAllPlayersReady_AllReady_ReturnsTrue()
        {
            var p1 = new Player("p1", "Alice");
            var p2 = new Player("p2", "Bob");
            PreGameSwap.MarkReady(p1);
            PreGameSwap.MarkReady(p2);

            bool result = PreGameSwap.AreAllPlayersReady(new List<Player> { p1, p2 });

            Assert.IsTrue(result);
        }

        [Test]
        public void AreAllPlayersReady_OneNotReady_ReturnsFalse()
        {
            var p1 = new Player("p1", "Alice");
            var p2 = new Player("p2", "Bob");
            PreGameSwap.MarkReady(p1);
            // p2 pas prêt

            bool result = PreGameSwap.AreAllPlayersReady(new List<Player> { p1, p2 });

            Assert.IsFalse(result);
        }

        // ------------------------------------------------------------------
        // DÉTERMINATION DU PREMIER JOUEUR
        // ------------------------------------------------------------------

        [Test]
        public void DetermineFirstPlayer_LowestRankInHand_WinsRegardlessOfFaceUp()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(CardRank.King, CardSuit.Spades), new Card(CardRank.Queen, CardSuit.Hearts), new Card(CardRank.Jack, CardSuit.Clubs) },
                new[] { new Card(CardRank.Three, CardSuit.Clubs), new Card(CardRank.Four, CardSuit.Clubs), new Card(CardRank.Five, CardSuit.Clubs) });

            var p2 = CreatePlayerWithHandAndFaceUp(
                "p2", "Bob",
                new[] { new Card(CardRank.Three, CardSuit.Hearts), new Card(CardRank.Ace, CardSuit.Diamonds), new Card(CardRank.Ten, CardSuit.Spades) },
                new[] { new Card(CardRank.King, CardSuit.Clubs), new Card(CardRank.Queen, CardSuit.Diamonds), new Card(CardRank.Jack, CardSuit.Hearts) });

            // p1 a un 3♣ en FACE-UP (ne compte pas), p2 a un 3♥ en MAIN (compte)
            var firstPlayer = PreGameSwap.DetermineFirstPlayer(new List<Player> { p1, p2 });

            Assert.AreEqual(p2, firstPlayer);
        }

        [Test]
        public void DetermineFirstPlayer_SameRank_ClubsBeatsAllOtherSuits()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(CardRank.Three, CardSuit.Spades), new Card(CardRank.Four, CardSuit.Clubs), new Card(CardRank.Five, CardSuit.Clubs) },
                new Card[0]);

            var p2 = CreatePlayerWithHandAndFaceUp(
                "p2", "Bob",
                new[] { new Card(CardRank.Three, CardSuit.Clubs), new Card(CardRank.Ace, CardSuit.Diamonds), new Card(CardRank.Ten, CardSuit.Spades) },
                new Card[0]);

            var firstPlayer = PreGameSwap.DetermineFirstPlayer(new List<Player> { p1, p2 });

            // 3♣ (p2) bat 3♠ (p1) selon l'ordre ♣→♦→♥→♠
            Assert.AreEqual(p2, firstPlayer);
        }

        [Test]
        public void DetermineFirstPlayer_SuitOrder_ClubsDiamondsHeartsSpades()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(CardRank.Three, CardSuit.Hearts), new Card(CardRank.Four, CardSuit.Clubs), new Card(CardRank.Five, CardSuit.Clubs) },
                new Card[0]);

            var p2 = CreatePlayerWithHandAndFaceUp(
                "p2", "Bob",
                new[] { new Card(CardRank.Three, CardSuit.Diamonds), new Card(CardRank.Ace, CardSuit.Diamonds), new Card(CardRank.Ten, CardSuit.Spades) },
                new Card[0]);

            var firstPlayer = PreGameSwap.DetermineFirstPlayer(new List<Player> { p1, p2 });

            // 3♦ (p2) bat 3♥ (p1)
            Assert.AreEqual(p2, firstPlayer);
        }

        [Test]
        public void DetermineFirstPlayer_NoThreesInHand_FallsBackToNextLowestRank()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(CardRank.Five, CardSuit.Clubs), new Card(CardRank.Six, CardSuit.Clubs), new Card(CardRank.Seven, CardSuit.Clubs) },
                new[] { new Card(CardRank.Three, CardSuit.Clubs), new Card(CardRank.Three, CardSuit.Diamonds), new Card(CardRank.Three, CardSuit.Hearts) });

            var p2 = CreatePlayerWithHandAndFaceUp(
                "p2", "Bob",
                new[] { new Card(CardRank.Four, CardSuit.Diamonds), new Card(CardRank.Ace, CardSuit.Diamonds), new Card(CardRank.Ten, CardSuit.Spades) },
                new[] { new Card(CardRank.Three, CardSuit.Spades), new Card(CardRank.Two, CardSuit.Clubs), new Card(CardRank.Two, CardSuit.Diamonds) });

            // Aucun 3 en main chez personne (tous en face-up) => on cherche le rang suivant : 4
            var firstPlayer = PreGameSwap.DetermineFirstPlayer(new List<Player> { p1, p2 });

            Assert.AreEqual(p2, firstPlayer); // 4♦ (p2) bat 5♣ (p1)
        }

        [Test]
        public void DetermineFirstPlayer_JokersInHandIgnored_DoesNotCrash()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(JokerType.Black), new Card(CardRank.Five, CardSuit.Clubs), new Card(JokerType.Glass) },
                new Card[0]);

            var firstPlayer = PreGameSwap.DetermineFirstPlayer(new List<Player> { p1 });

            Assert.AreEqual(p1, firstPlayer);
        }

        [Test]
        public void DetermineFirstPlayer_NoStandardCardsAnywhere_ThrowsInvalidOperationException()
        {
            var p1 = CreatePlayerWithHandAndFaceUp(
                "p1", "Alice",
                new[] { new Card(JokerType.Black), new Card(JokerType.Glass), new Card(JokerType.Color) },
                new Card[0]);

            Assert.Throws<System.InvalidOperationException>(() =>
                PreGameSwap.DetermineFirstPlayer(new List<Player> { p1 }));
        }
    }
}