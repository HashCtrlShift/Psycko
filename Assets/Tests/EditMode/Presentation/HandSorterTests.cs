using System.Collections.Generic;
using NUnit.Framework;
using Psycko.Core.Domain;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class HandSorterTests
    {
        [Test] public void TriRangCouleurJokersSansModifierLaSource()
        {
            var source = new List<Card> { Card.CreateJoker(DefJokerType.Color), Card.CreateStandard(DefSuit.Spades,DefRank.Three), Card.CreateStandard(DefSuit.Hearts,DefRank.Three), Card.CreateStandard(DefSuit.Clubs,DefRank.Two), Card.CreateJoker(DefJokerType.Glass), Card.CreateJoker(DefJokerType.Black), Card.CreateStandard(DefSuit.Clubs,DefRank.Three) };
            var snapshot = new List<Card>(source); var sorted = HandSorter.Sort(source);
            Assert.AreEqual(DefRank.Three,sorted[0].Rank); Assert.AreEqual(DefSuit.Clubs,sorted[0].Suit); Assert.AreEqual(DefSuit.Hearts,sorted[1].Suit); Assert.AreEqual(DefSuit.Spades,sorted[2].Suit); Assert.AreEqual(DefRank.Two,sorted[3].Rank);
            Assert.AreEqual(DefJokerType.Glass,sorted[4].JokerType); Assert.AreEqual(DefJokerType.Black,sorted[5].JokerType); Assert.AreEqual(DefJokerType.Color,sorted[6].JokerType);
            CollectionAssert.AreEqual(snapshot,source); CollectionAssert.AreEquivalent(source,sorted); Assert.AreEqual(source.Count,sorted.Count); Assert.AreEqual(0,HandSorter.Sort(new List<Card>()).Count);
        }
        [Test] public void RangTrieSelonOrdreEnum()
        {
            var list = new List<Card> { Card.CreateStandard(DefSuit.Spades,DefRank.Two),Card.CreateStandard(DefSuit.Clubs,DefRank.Priest),Card.CreateStandard(DefSuit.Clubs,DefRank.Three) };
            var s=HandSorter.Sort(list); Assert.AreEqual(DefRank.Three,s[0].Rank); Assert.AreEqual(DefRank.Priest,s[1].Rank); Assert.AreEqual(DefRank.Two,s[2].Rank);
        }
    }
}
