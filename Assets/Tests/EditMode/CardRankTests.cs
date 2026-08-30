using NUnit.Framework;
using Psycko;

namespace Psycko.Tests
{
    public class CardRankTests
    {
        [Test]
        public void CardRank_HasExactly15Values()
        {
            var values = System.Enum.GetValues(typeof(CardRank));
            Assert.AreEqual(15, values.Length);
        }

        [Test]
        public void CardRank_ContainsAllRequired()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRank), CardRank.Three));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRank), CardRank.Four));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRank), CardRank.Priest));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRank), CardRank.Knight));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CardRank), CardRank.Two));
        }
    }
}