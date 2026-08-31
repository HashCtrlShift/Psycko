using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests.EditMode
{
    [TestFixture]
    public class RefillHandTests
    {
        [Test]
        public void RefillHand_DeckHasEnoughCards_RefillsToThree()
        {
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            // main = 1 carte

            var deck = new Deck(seed: 42);
            // Pioche jusqu'à avoir exactement 2 cartes restantes
            // (1 en main + 2 pioches = 3 cartes après RefillHand)
            while (deck.Count > 2)
            {
                deck.Draw();
            }

            var state = new GameState(new[] { player }, new Pile(), deck);

            state.RefillHand(player);

            Assert.AreEqual(3, player.Hand.Count);
            Assert.AreEqual(0, deck.Count); // Les 3 cartes de la pioche ont été consommées
        }

        [Test]
        public void RefillHand_DeckHasPartialCards_RefillsPartially()
        {
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            // main = 1 carte

            var deck = new Deck(seed: 42);
            // Pioche jusqu'à avoir exactement 1 seule carte restante
            while (deck.Count > 1)
            {
                deck.Draw();
            }

            var state = new GameState(new[] { player }, new Pile(), deck);

            state.RefillHand(player);

            Assert.AreEqual(2, player.Hand.Count); // 1 initiale + 1 piochée
            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void RefillHand_HandAlreadyHasThreeCards_DoesNothing()
        {
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            player.AddCardToHand(new Card(CardRank.Four, CardSuit.Spades));
            player.AddCardToHand(new Card(CardRank.Five, CardSuit.Diamonds));
            // main = 3 cartes

            var deck = new Deck(seed: 42);
            int deckCountBefore = deck.Count;

            var state = new GameState(new[] { player }, new Pile(), deck);

            state.RefillHand(player);

            Assert.AreEqual(3, player.Hand.Count);
            Assert.AreEqual(deckCountBefore, deck.Count); // Aucune carte piochée
        }

        [Test]
        public void RefillHand_DeckIsEmpty_DoesNothing()
        {
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            // main = 1 carte

            var deck = new Deck(seed: 42);
            // Vide complètement la pioche
            while (deck.Count > 0)
            {
                deck.Draw();
            }

            var state = new GameState(new[] { player }, new Pile(), deck);

            state.RefillHand(player);

            Assert.AreEqual(1, player.Hand.Count); // Aucune carte piochée
            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void RefillHand_NullPlayer_DoesNotThrow()
        {
            var dummyPlayer = new Player("dummy", "Dummy");
            var gameState = new GameState(new List<Player> { dummyPlayer }, null, null);
            gameState.RefillHand(null);
            // Test passes if no exception is thrown
        }
        [Test]
        public void Integration_PlayCard_ThenRefillHand_ThenCheckPlayerState_TransitionsToTalent()
        {
            // Scénario : J1 joue sa carte de main, pioche vide,
            // mais FaceUp non vide => transition attendue vers Talent après refill (no-op car pioche vide).
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            player.AddCardFaceUp(new Card(CardRank.Four, CardSuit.Spades));

            var pile = new Pile();
            var deck = new Deck(seed: 42);
            // Vide la pioche
            while (deck.Count > 0)
            {
                deck.Draw();
            }

            var state = new GameState(new[] { player }, pile, deck);

            bool playResult = state.PlayCard(player, new Card(CardRank.Three, CardSuit.Hearts));
            Assert.IsTrue(playResult, "PlayCard should succeed");
            // main désormais vide

            state.RefillHand(player); // no-op, pioche vide
            var result = state.CheckPlayerState(player);

            Assert.AreEqual(PhaseTransitionResult.TransitionedToTalent, result);
        }

        [Test]
        public void Integration_PlayThreeCards_RefillHand_CheckWin()
        {
            // Scénario : J1 joue ses 3 cartes de main, pioche vide, FaceUp vide, FaceDown vide => gagne
            var player = new Player("J1", "Alice");
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Hearts));
            player.AddCardToHand(new Card(CardRank.Four, CardSuit.Spades));
            player.AddCardToHand(new Card(CardRank.Five, CardSuit.Diamonds));

            var pile = new Pile();
            var deck = new Deck(seed: 42);
            // Vide la pioche
            while (deck.Count > 0)
            {
                deck.Draw();
            }

            var state = new GameState(new[] { player }, pile, deck);

            // Joue les 3 cartes
            state.PlayCard(player, new Card(CardRank.Three, CardSuit.Hearts));
            state.PlayCard(player, new Card(CardRank.Four, CardSuit.Spades));
            state.PlayCard(player, new Card(CardRank.Five, CardSuit.Diamonds));

            state.RefillHand(player); // no-op, pioche vide et main vide
            var result = state.CheckPlayerState(player);

            Assert.AreEqual(PhaseTransitionResult.Won, result);
        }
    }
}