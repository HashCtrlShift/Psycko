using NUnit.Framework;
using System.Collections.Generic;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    [TestFixture]
    public class PickUpPileTests
    {
        private Player player;
        private GameState state;

        [SetUp]
        public void SetUp()
        {
            player = new Player("p1", "Alice");
            state = new GameState(new List<Player> { player });
        }

        [Test]
        public void PickUpPile_WithCards_MovesAllCardsToHand()
        {
            state.Pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            state.Pile.Add(new Card(CardRank.Seven, CardSuit.Clubs));
            state.Pile.Add(new Card(CardRank.King, CardSuit.Diamonds));

            bool result = state.PickUpPile(player);

            Assert.IsTrue(result);
            Assert.AreEqual(3, player.Hand.Count);
            Assert.IsTrue(state.Pile.IsEmpty());
        }

        [Test]
        public void PickUpPile_EmptyPile_ReturnsFalse()
        {
            bool result = state.PickUpPile(player);

            Assert.IsFalse(result);
            Assert.AreEqual(0, player.Hand.Count);
        }

        [Test]
        public void PickUpPile_NullPlayer_ReturnsFalse()
        {
            state.Pile.Add(new Card(CardRank.Five, CardSuit.Hearts));

            bool result = state.PickUpPile(null);

            Assert.IsFalse(result);
            Assert.AreEqual(1, state.Pile.Count, "La pile ne doit pas être vidée si le joueur est null.");
        }

        [Test]
        public void PickUpPile_VoluntaryPickup_EvenIfCardsPlayable_Succeeds()
        {
            // Cas stratégique : le joueur ramasse alors qu'il aurait pu jouer.
            state.Pile.Add(new Card(CardRank.Three, CardSuit.Hearts));
            player.AddCardToHand(new Card(CardRank.Five, CardSuit.Clubs)); // carte jouable en main

            bool result = state.PickUpPile(player);

            Assert.IsTrue(result);
            Assert.AreEqual(2, player.Hand.Count); // carte jouable + carte ramassée
        }

        [Test]
        public void PickUpPile_PreservesCardIdentity()
        {
            Card king = new Card(CardRank.King, CardSuit.Spades);
            state.Pile.Add(king);

            state.PickUpPile(player);

            Assert.Contains(king, player.Hand);
        }
    }
}