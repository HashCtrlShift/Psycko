using NUnit.Framework;
using Psycko;

public class PowerCardTests
{
    [Test]
    public void Constructor_InitializesCorrectly()
    {
        var pc = new PowerCard("pc-1", "player-a");
        Assert.AreEqual("pc-1", pc.Id);
        Assert.AreEqual("player-a", pc.PlayerId);
        Assert.AreEqual(PowerCardEffectType.Unknown, pc.EffectType);
        Assert.IsFalse(pc.IsUsed);
    }

    [Test]
    public void IsUsed_CanBeModified()
    {
        var pc = new PowerCard("pc-1", "player-a");
        Assert.IsFalse(pc.IsUsed);
        pc.IsUsed = true;
        Assert.IsTrue(pc.IsUsed);
    }

    [Test]
    public void Equals_ReturnsTrueForSameId()
    {
        var pc1 = new PowerCard("pc-1", "player-a");
        var pc2 = new PowerCard("pc-1", "player-b"); // Même id, joueur différent
        Assert.AreEqual(pc1, pc2); // Equals compare l'id uniquement
    }

    [Test]
    public void Equals_ReturnsFalseForDifferentId()
    {
        var pc1 = new PowerCard("pc-1", "player-a");
        var pc2 = new PowerCard("pc-2", "player-a");
        Assert.AreNotEqual(pc1, pc2);
    }

    [Test]
    public void GetHashCode_IsSameForSameId()
    {
        var pc1 = new PowerCard("pc-1", "player-a");
        var pc2 = new PowerCard("pc-1", "player-b");
        Assert.AreEqual(pc1.GetHashCode(), pc2.GetHashCode());
    }

    [Test]
    public void ToString_FormatsCorrectly()
    {
        var pc = new PowerCard("pc-1", "player-a");
        string expected = "PowerCard(pc-1, player-a, Unknown, Used=False)";
        Assert.AreEqual(expected, pc.ToString());
    }
}