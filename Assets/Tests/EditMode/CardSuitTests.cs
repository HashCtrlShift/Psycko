using NUnit.Framework;
using Psycko;

namespace Psycko.Tests
{
    public class CardSuitTests
    {
        [Test]
        public void CardSuit_HasExactly4Values()
        {
            var values = System.Enum.GetValues(typeof(CardSuit));
            Assert.AreEqual(4, values.Length);
        }

        [Test]
        public void CardSuit_ContainsAllRequired()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardSuit), CardSuit.Spades));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardSuit), CardSuit.Hearts));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardSuit), CardSuit.Clubs));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardSuit), CardSuit.Diamonds));
        }
    }
}