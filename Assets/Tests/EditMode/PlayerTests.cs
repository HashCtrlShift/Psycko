using NUnit.Framework;
using Psycko;
using System;

public class PlayerTests
{
    [Test]
    public void Constructor_InitializesCorrectly()
    {
        var player = new Player("player-1", "Alice");
        Assert.AreEqual("player-1", player.Id);
        Assert.AreEqual("Alice", player.Name);
        Assert.AreEqual(0, player.Hand.Count);
        Assert.AreEqual(0, player.FaceUp.Count);
        Assert.AreEqual(0, player.FaceDown.Count);
        Assert.IsNull(player.AssignedPowerCard);
    }

    [Test]
    public void Constructor_ThrowsOnNullId()
    {
        Assert.Throws<ArgumentException>(() => new Player(null, "Alice"));
    }

    [Test]
    public void Constructor_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new Player("player-1", ""));
    }

    [Test]
    public void AddCardToHand_IncreaseHandCount()
    {
        var player = new Player("player-1", "Alice");
        var card = new Card(CardRank.Seven, CardSuit.Hearts);
        
        player.AddCardToHand(card);
        Assert.AreEqual(1, player.Hand.Count);
        Assert.AreEqual(card, player.Hand[0]);
    }

    [Test]
    public void AddCardFaceUp_IncreasesFaceUpCount()
    {
        var player = new Player("player-1", "Alice");
        var card = new Card(CardRank.King, CardSuit.Diamonds);
        
        player.AddCardFaceUp(card);
        Assert.AreEqual(1, player.FaceUp.Count);
        Assert.AreEqual(card, player.FaceUp[0]);
    }

    [Test]
    public void AddCardFaceDown_IncreasesFaceDownCount()
    {
        var player = new Player("player-1", "Alice");
        var card = new Card(CardRank.Ace, CardSuit.Clubs);
        
        player.AddCardFaceDown(card);
        Assert.AreEqual(1, player.FaceDown.Count);
        Assert.AreEqual(card, player.FaceDown[0]);
    }

    [Test]
    public void TotalCards_ReturnsSumOfAllZones()
    {
        var player = new Player("player-1", "Alice");
        player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
        player.AddCardToHand(new Card(CardRank.Four, CardSuit.Hearts));
        player.AddCardFaceUp(new Card(CardRank.Five, CardSuit.Hearts));
        player.AddCardFaceDown(new Card(CardRank.Six, CardSuit.Hearts));
        
        Assert.AreEqual(4, player.TotalCards);
    }

    [Test]
    public void RemoveCardFromHand_RemovesCard()
    {
        var player = new Player("player-1", "Alice");
        var card = new Card(CardRank.Seven, CardSuit.Hearts);
        player.AddCardToHand(card);
        
        bool removed = player.RemoveCardFromHand(card);
        Assert.IsTrue(removed);
        Assert.AreEqual(0, player.Hand.Count);
    }

    [Test]
    public void RemoveCardFromHand_ReturnsFalseIfNotFound()
    {
        var player = new Player("player-1", "Alice");
        var card1 = new Card(CardRank.Seven, CardSuit.Hearts);
        var card2 = new Card(CardRank.Eight, CardSuit.Hearts);
        player.AddCardToHand(card1);
        
        bool removed = player.RemoveCardFromHand(card2);
        Assert.IsFalse(removed);
    }

    [Test]
    public void AssignedPowerCard_CanBeSet()
    {
        var player = new Player("player-1", "Alice");
        var powerCard = new PowerCard("pc-1", "player-1");
        
        player.AssignedPowerCard = powerCard;
        Assert.IsTrue(player.AssignedPowerCard.HasValue);
        Assert.AreEqual(powerCard, player.AssignedPowerCard.Value);
    }

    [Test]
    public void Equals_ReturnsTrueForSameId()
    {
        var player1 = new Player("player-1", "Alice");
        var player2 = new Player("player-1", "Bob");
        
        Assert.AreEqual(player1, player2); // Même id, noms différents
    }

    [Test]
    public void Equals_ReturnsFalseForDifferentId()
    {
        var player1 = new Player("player-1", "Alice");
        var player2 = new Player("player-2", "Alice");
        
        Assert.AreNotEqual(player1, player2);
    }

    [Test]
    public void ToString_FormatsCorrectly()
    {
        var player = new Player("player-1", "Alice");
        var powerCard = new PowerCard("pc-1", "player-1");
        player.AssignedPowerCard = powerCard;
        
        string result = player.ToString();
        Assert.That(result, Does.Contain("player-1"));
        Assert.That(result, Does.Contain("Alice"));
        Assert.That(result, Does.Contain("PowerCard="));
    }
}