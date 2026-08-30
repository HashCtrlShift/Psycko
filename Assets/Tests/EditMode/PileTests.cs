using NUnit.Framework;
using Psycko.Core;
using System;
using System.Collections.Generic;

namespace Psycko.Tests.EditMode
{
    [TestFixture]
    public class PileTests
    {
        private Pile pile;

        [SetUp]
        public void Setup()
        {
            pile = new Pile();
        }

        // ===== Add() Tests =====

        [Test]
        public void Add_WithValidCard_AddsCardToPile()
        {
            // Arrange
            Card card = new Card(CardRank.Three, CardSuit.Hearts);

            // Act
            pile.Add(card);

            // Assert
            Assert.AreEqual(1, pile.Count);
            Assert.AreEqual(card, pile.Top());
        }

        [Test]
        public void Add_MultipleCards_MaintainsLIFOOrder()
        {
            // Arrange
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Four, CardSuit.Diamonds);
            Card card3 = new Card(CardRank.Five, CardSuit.Clubs);

            // Act
            pile.Add(card1);
            pile.Add(card2);
            pile.Add(card3);

            // Assert
            Assert.AreEqual(3, pile.Count);
            Assert.AreEqual(card3, pile.Top());
        }

        // ===== Top() Tests =====

        [Test]
        public void Top_WithEmptyPile_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => pile.Top());
        }

        [Test]
        public void Top_WithCards_ReturnsMostRecentCard()
        {
            // Arrange
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Four, CardSuit.Diamonds);
            pile.Add(card1);
            pile.Add(card2);

            // Act
            Card top = pile.Top();

            // Assert
            Assert.AreEqual(card2, top);
            Assert.AreEqual(2, pile.Count); // Top() ne retire pas la carte
        }

        [Test]
        public void Top_CalledMultipleTimes_ReturnsSameCard()
        {
            // Arrange
            Card card = new Card(CardRank.Seven, CardSuit.Spades);
            pile.Add(card);

            // Act
            Card top1 = pile.Top();
            Card top2 = pile.Top();

            // Assert
            Assert.AreEqual(top1, top2);
            Assert.AreEqual(card, top1);
        }

        // ===== Pop() Tests =====

        [Test]
        public void Pop_WithEmptyPile_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => pile.Pop());
        }

        [Test]
        public void Pop_WithCards_RemovesAndReturnsMostRecentCard()
        {
            // Arrange
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Four, CardSuit.Diamonds);
            pile.Add(card1);
            pile.Add(card2);

            // Act
            Card popped = pile.Pop();

            // Assert
            Assert.AreEqual(card2, popped);
            Assert.AreEqual(1, pile.Count);
            Assert.AreEqual(card1, pile.Top());
        }

        [Test]
        public void Pop_MultipleCards_MaintainsLIFOOrder()
        {
            // Arrange
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Four, CardSuit.Diamonds);
            Card card3 = new Card(CardRank.Five, CardSuit.Clubs);
            pile.Add(card1);
            pile.Add(card2);
            pile.Add(card3);

            // Act
            Card pop1 = pile.Pop();
            Card pop2 = pile.Pop();

            // Assert
            Assert.AreEqual(card3, pop1);
            Assert.AreEqual(card2, pop2);
            Assert.AreEqual(1, pile.Count);
            Assert.AreEqual(card1, pile.Top());
        }

        // ===== Clear() Tests =====

        [Test]
        public void Clear_WithCards_EmptiesPile()
        {
            // Arrange
            pile.Add(new Card(CardRank.King, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Queen, CardSuit.Diamonds));

            // Act
            pile.Clear();

            // Assert
            Assert.IsTrue(pile.IsEmpty());
            Assert.AreEqual(0, pile.Count);
        }

        // ===== IsEmpty() Tests =====

        [Test]
        public void IsEmpty_WithEmptyPile_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(pile.IsEmpty());
        }

        [Test]
        public void IsEmpty_WithCards_ReturnsFalse()
        {
            // Arrange
            pile.Add(new Card(CardRank.Three, CardSuit.Hearts));

            // Act & Assert
            Assert.IsFalse(pile.IsEmpty());
        }

        [Test]
        public void IsEmpty_AfterClear_ReturnsTrue()
        {
            // Arrange
            pile.Add(new Card(CardRank.Three, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Four, CardSuit.Diamonds));

            // Act
            pile.Clear();

            // Assert
            Assert.IsTrue(pile.IsEmpty());
        }

        // ===== Cards Property Tests =====

        [Test]
        public void Cards_ReturnsReadOnlyCollection()
        {
            // Arrange
            Card card = new Card(CardRank.Three, CardSuit.Hearts);
            pile.Add(card);

            // Act
            IReadOnlyList<Card> cards = pile.Cards;

            // Assert
            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(card, cards[0]);
        }

       [Test]
        public void Cards_IsImmutable()
        {
            // Arrange
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            pile.Add(card1);
            var cards = pile.Cards;

            // Act & Assert
            // IReadOnlyList n'expose pas Add() - donc pas besoin de cast dangereux
            Assert.That(cards, Is.InstanceOf<IReadOnlyList<Card>>());
            Assert.That(cards.Count, Is.EqualTo(1));
    
            // Tentative de cast et modification = NotSupportedException
            Assert.Throws<NotSupportedException>(() => 
            {
                var list = (System.Collections.IList)cards;
                list.Add(new Card(CardRank.Four, CardSuit.Diamonds));
            });
        }

        // ===== ToString() Tests =====

        [Test]
        public void ToString_WithEmptyPile_ReturnsEmptyMessage()
        {
            // Act
            string result = pile.ToString();

            // Assert
            Assert.AreEqual("Pile(empty)", result);
        }

        [Test]
        public void ToString_WithCards_ReturnsCountAndTop()
        {
            // Arrange
            Card card = new Card(CardRank.King, CardSuit.Spades);
            pile.Add(card);

            // Act
            string result = pile.ToString();

            // Assert
            Assert.That(result, Does.Contain("Pile(1 cards"));
            Assert.That(result, Does.Contain("Top="));
        }
    }
}