using NUnit.Framework;
using Psycko.Core;

namespace Psycko.Tests
{
    public class GameSeedTests
    {
        [Test]
        public void DeriveDeckSeed_IsDeterministic()
        {
            int seed1 = GameSeed.DeriveDeckSeed(12345);
            int seed2 = GameSeed.DeriveDeckSeed(12345);
            Assert.AreEqual(seed1, seed2);
        }

        [Test]
        public void DerivePlayerSeed_IsDeterministic()
        {
            int seed1 = GameSeed.DerivePlayerSeed(12345, 2);
            int seed2 = GameSeed.DerivePlayerSeed(12345, 2);
            Assert.AreEqual(seed1, seed2);
        }

        [Test]
        public void DeriveDeckSeed_And_DerivePlayerSeed_AreDifferent()
        {
            int deckSeed = GameSeed.DeriveDeckSeed(12345);
            int playerSeed = GameSeed.DerivePlayerSeed(12345, 0);
            Assert.AreNotEqual(deckSeed, playerSeed);
        }

        [Test]
        public void DifferentSeatIndices_ProduceDifferentSeeds()
        {
            int seedA = GameSeed.DerivePlayerSeed(12345, 0);
            int seedB = GameSeed.DerivePlayerSeed(12345, 1);
            Assert.AreNotEqual(seedA, seedB);
        }

        [Test]
        public void DifferentRootSeeds_ProduceDifferentDerivedSeeds()
        {
            int seedA = GameSeed.DerivePlayerSeed(11111, 0);
            int seedB = GameSeed.DerivePlayerSeed(22222, 0);
            Assert.AreNotEqual(seedA, seedB);
        }

        [Test]
        public void AllDerivedSeeds_AreNonNegative()
        {
            Assert.GreaterOrEqual(GameSeed.DeriveDeckSeed(-999), 0);
            Assert.GreaterOrEqual(GameSeed.DerivePlayerSeed(-999, 3), 0);
        }
    }
}