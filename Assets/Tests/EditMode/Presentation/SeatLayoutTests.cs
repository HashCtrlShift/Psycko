using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Psycko.Bots;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Validation;
using Psycko.Presentation.Controllers;

namespace Psycko.Presentation.Tests
{
    [TestFixture]
    public sealed class SeatLayoutTests
    {
        [Test] public void RepartitionValideDeterministeEtCouvertureDesSieges()
        {
            var checker = new CardPlayabilityChecker(); var possible = new HashSet<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                var one = new SeatLayout(new Random(seed)).Create(new UnityHumanAgent(), checker);
                var two = new SeatLayout(new Random(seed)).Create(new UnityHumanAgent(), checker);
                Assert.AreEqual(one.DiscardSeat,two.DiscardSeat); possible.Add(one.DiscardSeat);
                Assert.AreEqual(4,one.Agents.Count); Assert.IsInstanceOf<UnityHumanAgent>(one.Agents[0]);
                Assert.AreEqual(1,one.Agents.Count(a => a is DiscardBot)); Assert.AreEqual(2,one.Agents.Count(a => a is RandomBot));
                Assert.That(one.DiscardSeat,Is.InRange(1,3)); Assert.AreEqual(4,one.Names.Distinct().Count()); Assert.IsTrue(one.Names.All(n => !string.IsNullOrWhiteSpace(n)));
                Assert.AreEqual("Vous",one.Names[0]); Assert.AreEqual("Discard",one.Names[one.DiscardSeat]);
            }
            CollectionAssert.AreEquivalent(new[] { 1,2,3 },possible);
        }
    }
}
