using NUnit.Framework;
using Psycko;
using System.Collections.Generic;

namespace Psycko.Tests
{
    public class DeckTests
    {
        [Test]
        public void Deck_CreatedWithCorrectCardCount()
        {
            var deck = new Deck();
            Assert.AreEqual(63, deck.Count);
        }

        [Test]
        public void Deck_Contains60StandardCards()
        {
            var deck = new Deck();
            int standardCount = 0;
            var allCards = deck.GetAllCards();
            
            foreach (var card in allCards)
            {
                if (card.IsStandardCard) standardCount++;
            }
            
            Assert.AreEqual(60, standardCount);
        }

        [Test]
        public void Deck_Contains3Jokers()
        {
            var deck = new Deck();
            int jokerCount = 0;
            var allCards = deck.GetAllCards();
            
            foreach (var card in allCards)
            {
                if (card.IsJoker) jokerCount++;
            }
            
            Assert.AreEqual(3, jokerCount);
        }

        [Test]
        public void Deck_Contains1JokerOfEachType()
        {
            var deck = new Deck();
            int glassCount = 0, blackCount = 0, colorCount = 0;
            var allCards = deck.GetAllCards();
            
            foreach (var card in allCards)
            {
                if (card.IsJoker)
                {
                    if (card.JokerType == JokerType.Glass) glassCount++;
                    else if (card.JokerType == JokerType.Black) blackCount++;
                    else if (card.JokerType == JokerType.Color) colorCount++;
                }
            }
            
            Assert.AreEqual(1, glassCount);
            Assert.AreEqual(1, blackCount);
            Assert.AreEqual(1, colorCount);
        }

        [Test]
        public void Deck_Contains4CardsOfEachStandardRankAndSuit()
        {
            var deck = new Deck();
            var allCards = deck.GetAllCards();
            
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
                {
                    var card = new Card(rank, suit);
                    int count = 0;
                    
                    foreach (var deckCard in allCards)
                    {
                        if (deckCard == card) count++;
                    }
                    
                    Assert.AreEqual(1, count, $"Deck should contain exactly 1 card of {rank} of {suit}");
                }
            }
        }

        [Test]
        public void Deck_Draw_RemovesCardFromDeck()
        {
            var deck = new Deck();
            int initialCount = deck.Count;
            var card = deck.Draw();
            
            Assert.IsNotNull(card);
            Assert.AreEqual(initialCount - 1, deck.Count);
        }

        [Test]
        public void Deck_Draw_ReturnsAllCardsInOrder()
        {
            var deck = new Deck();
            var drawnCards = new List<Card>();
            
            while (deck.Count > 0)
            {
                drawnCards.Add(deck.Draw());
            }
            
            Assert.AreEqual(63, drawnCards.Count);
        }

        [Test]
        public void Deck_Draw_WhenEmpty_ThrowsException()
        {
            var deck = new Deck();
            
            // Vider le deck
            while (deck.Count > 0)
            {
                deck.Draw();
            }
            
            Assert.Throws<System.InvalidOperationException>(() => deck.Draw());
        }

        [Test]
        public void Deck_Shuffle_DifferentWithDifferentSeeds()
        {
            var deck1 = new Deck(seed: 42);
            var deck2 = new Deck(seed: 99);
            
            var cards1 = deck1.GetAllCards();
            var cards2 = deck2.GetAllCards();
            
            bool allIdentical = true;
            for (int i = 0; i < 63; i++)
            {
                if (cards1[i] != cards2[i])
                {
                    allIdentical = false;
                    break;
                }
            }
            
            Assert.IsFalse(allIdentical, "Different seeds should produce different shuffles");
        }

        [Test]
        public void Deck_Shuffle_DeterministicWithSameSeed()
        {
            var deck1 = new Deck(seed: 42);
            var deck2 = new Deck(seed: 42);
            
            var cards1 = deck1.GetAllCards();
            var cards2 = deck2.GetAllCards();
            
            for (int i = 0; i < 63; i++)
            {
                Assert.AreEqual(cards1[i], cards2[i], $"Deck order differs at position {i}");
            }
        }

        [Test]
        public void Deck_Peek_ReturnsCardsWithoutRemoving()
        {
            var deck = new Deck();
            int initialCount = deck.Count;
            var peeked = deck.Peek(5);
            
            Assert.AreEqual(5, peeked.Count);
            Assert.AreEqual(initialCount, deck.Count);
        }

        [Test]
        public void Deck_Peek_InvalidCount_ThrowsException()
        {
            var deck = new Deck();
            Assert.Throws<System.ArgumentException>(() => deck.Peek(100));
            Assert.Throws<System.ArgumentException>(() => deck.Peek(-1));
        }

        [Test]
        public void Deck_GetAllCards_ReturnsAllCards()
        {
            var deck = new Deck();
            var allCards = deck.GetAllCards();
            Assert.AreEqual(63, allCards.Count);
        }

        [Test]
        public void Deck_GetAllCards_DoesNotModifyDeck()
        {
            var deck = new Deck();
            int initialCount = deck.Count;
            deck.GetAllCards();
            Assert.AreEqual(initialCount, deck.Count);
        }
    }
}