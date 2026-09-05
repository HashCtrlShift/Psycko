using NUnit.Framework;
using System.Collections.Generic;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    [TestFixture]
    public class PlayCardTests
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
        public void PlayCard_OnEmptyPile_AlwaysSucceeds()
        {
            var emptyDeck = new Deck(seed: 1);
            while (emptyDeck.Count > 0) emptyDeck.Draw();  // Vide le deck
    
            var state = new GameState(new List<Player> { player }, new Pile(), emptyDeck);
            
            Card card = new Card(CardRank.Five, CardSuit.Hearts);
            player.AddCardToHand(card);

            bool result = state.PlayCard(player, card);

            Assert.IsTrue(result);
            Assert.AreEqual(0, player.Hand.Count);
            Assert.AreEqual(1, state.Pile.Count);
            Assert.AreEqual(card, state.Pile.Top());
        }

        [Test]
        public void PlayCard_HigherRank_Succeeds()
        {
            state.Pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            Card card = new Card(CardRank.Seven, CardSuit.Clubs);
            player.AddCardToHand(card);

            bool result = state.PlayCard(player, card);

            Assert.IsTrue(result);
            Assert.AreEqual(card, state.Pile.Top());
        }

        [Test]
        public void PlayCard_EqualRank_Succeeds()
        {
            state.Pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            Card card = new Card(CardRank.Five, CardSuit.Clubs);
            player.AddCardToHand(card);

            bool result = state.PlayCard(player, card);

            Assert.IsTrue(result);
        }

        [Test]
        public void PlayCard_LowerRank_Fails()
        {
            state.Pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));
            Card card = new Card(CardRank.Five, CardSuit.Clubs);
            player.AddCardToHand(card);

            bool result = state.PlayCard(player, card);

            Assert.IsFalse(result);
            Assert.AreEqual(1, player.Hand.Count, "La carte refusée doit rester en main.");
            Assert.AreEqual(1, state.Pile.Count, "La pile ne doit pas avoir changé.");
        }

        [Test]
        public void PlayCard_CardNotInHand_Fails()
        {
            Card card = new Card(CardRank.Five, CardSuit.Hearts);
            // Ne pas ajouter la carte à la main du joueur

            bool result = state.PlayCard(player, card);

            Assert.IsFalse(result);
            Assert.AreEqual(0, state.Pile.Count);
        }

        [Test]
        public void PlayCard_NullPlayer_ReturnsFalse()
        {
            Card card = new Card(CardRank.Five, CardSuit.Hearts);

            bool result = state.PlayCard(null, card);

            Assert.IsFalse(result);
        }
        
        [Test]
        public void PlayCard_JokerFromHand_AlwaysSucceeds()
        {
            state.Pile.Add(new Card(CardRank.King, CardSuit.Hearts));
            Card joker = new Card(JokerType.Black);
            player.AddCardToHand(joker);

            bool result = state.PlayCard(player, joker);

            Assert.IsTrue(result);
            Assert.AreEqual(joker, state.Pile.Top());
        }

        [Test]
        public void PlayCard_OnTopOfJoker_AnyRankSucceeds()
        {
            state.Pile.Add(new Card(JokerType.Glass));
            Card card = new Card(CardRank.Three, CardSuit.Clubs);
            player.AddCardToHand(card);

            bool result = state.PlayCard(player, card);

            Assert.IsTrue(result);
        }
    }
}