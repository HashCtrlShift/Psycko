using NUnit.Framework;
using Psycko;

namespace Psycko.Tests
{
    public class CardTests
    {
        [Test]
        public void Card_StandardCard_CreatedCorrectly()
        {
            var card = new Card(CardRank.King, CardSuit.Hearts);
            Assert.AreEqual(CardRank.King, card.Rank);
            Assert.AreEqual(CardSuit.Hearts, card.Suit);
            Assert.IsFalse(card.IsJoker);
            Assert.IsTrue(card.IsStandardCard);
        }

        [Test]
        public void Card_Joker_CreatedCorrectly()
        {
            var joker = new Card(JokerType.Glass);
            Assert.IsTrue(joker.IsJoker);
            Assert.IsFalse(joker.IsStandardCard);
            Assert.AreEqual(JokerType.Glass, joker.JokerType);
        }

        [Test]
        public void Card_StandardCard_NotEqual_ToJoker()
        {
            var card = new Card(CardRank.King, CardSuit.Hearts);
            var joker = new Card(JokerType.Glass);
            Assert.AreNotEqual(card, joker);
        }

        [Test]
        public void Card_IdenticalStandardCards_AreEqual()
        {
            var card1 = new Card(CardRank.King, CardSuit.Hearts);
            var card2 = new Card(CardRank.King, CardSuit.Hearts);
            Assert.AreEqual(card1, card2);
        }

        [Test]
        public void Card_IdenticalJokers_AreEqual()
        {
            var joker1 = new Card(JokerType.Glass);
            var joker2 = new Card(JokerType.Glass);
            Assert.AreEqual(joker1, joker2);
        }

        [Test]
        public void Card_DifferentJokers_AreNotEqual()
        {
            var joker1 = new Card(JokerType.Glass);
            var joker2 = new Card(JokerType.Black);
            Assert.AreNotEqual(joker1, joker2);
        }

        [Test]
        public void Card_GetHashCode_Consistent()
        {
            var card = new Card(CardRank.Ace, CardSuit.Spades);
            int hash1 = card.GetHashCode();
            int hash2 = card.GetHashCode();
            Assert.AreEqual(hash1, hash2);
        }
    }
}