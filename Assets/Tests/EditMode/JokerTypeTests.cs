using NUnit.Framework;
using Psycko;

namespace Psycko.Tests
{
    public class JokerTypeTests
    {
        [Test]
        public void JokerType_HasExactly3Values()
        {
            var values = System.Enum.GetValues(typeof(JokerType));
            Assert.AreEqual(3, values.Length);
        }

        [Test]
        public void JokerType_ContainsAllRequired()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(JokerType), JokerType.Glass));
            Assert.IsTrue(System.Enum.IsDefined(typeof(JokerType), JokerType.Black));
            Assert.IsTrue(System.Enum.IsDefined(typeof(JokerType), JokerType.Color));
        }
    }
}