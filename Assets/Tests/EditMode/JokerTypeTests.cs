using System.Collections.Generic;
using NUnit.Framework;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    [TestFixture]
    public class GameStateTests
    {
        private GameState gameState;
        private List<Player> players;
        private Pile pile;
        private Deck deck;

        [SetUp]
        public void Setup()
        {
            players = new List<Player>
            {
                new Player("p0", "Player0"),
                new Player("p1", "Player1"),
                new Player("p2", "Player2"),
                new Player("p3", "Player3")
            };

            pile = new Pile();
            deck = new Deck(seed: 42);
            gameState = new GameState(players, pile, deck);
        }

        #region GetEffectiveTopCard & GetSignificantCardsFromTop Tests

        [Test]
        public void GetEffectiveTopCard_EmptyPile_ReturnsNull()
        {
            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.IsNull(effectiveTop);
        }

        [Test]
        public void GetEffectiveTopCard_SingleStandardCard_ReturnsThatCard()
        {
            Card card = new Card(CardRank.Five, CardSuit.Hearts);
            pile.Add(card);

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.AreEqual(card, effectiveTop.Value);
        }

        [Test]
        public void GetEffectiveTopCard_GlassJokerOnTop_SkipsItAndReturnsCardBelow()
        {
            Card below = new Card(CardRank.Seven, CardSuit.Spades);
            Card glassJoker = new Card(JokerType.Glass);

            pile.Add(below);
            pile.Add(glassJoker);

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.AreEqual(below, effectiveTop.Value);
        }

        [Test]
        public void GetEffectiveTopCard_BlackJokerOnTop_ReturnsBlackJoker()
        {
            Card below = new Card(CardRank.Nine, CardSuit.Diamonds);
            Card blackJoker = new Card(JokerType.Black);

            pile.Add(below);
            pile.Add(blackJoker);

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.AreEqual(blackJoker, effectiveTop.Value);
        }

        [Test]
        public void GetEffectiveTopCard_ColorJokerOnTop_ReturnsColorJoker()
        {
            Card below = new Card(CardRank.King, CardSuit.Clubs);
            Card colorJoker = new Card(JokerType.Color);

            pile.Add(below);
            pile.Add(colorJoker);

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.AreEqual(colorJoker, effectiveTop.Value);
        }

        [Test]
        public void GetEffectiveTopCard_MultipleGlassJokersOnTop_SkipsAllAndReturnsFinalCard()
        {
            Card card = new Card(CardRank.Six, CardSuit.Hearts);
            Card glass1 = new Card(JokerType.Glass);
            Card glass2 = new Card(JokerType.Glass);

            pile.Add(card);
            pile.Add(glass1);
            pile.Add(glass2);

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.AreEqual(card, effectiveTop.Value);
        }

        [Test]
        public void GetEffectiveTopCard_OnlyGlassJokersInPile_ReturnsNull()
        {
            pile.Add(new Card(JokerType.Glass));
            pile.Add(new Card(JokerType.Glass));

            var effectiveTop = gameState.GetEffectiveTopCard(pile);
            Assert.IsNull(effectiveTop);
        }

        #endregion

        #region IsPlayable Tests

        [Test]
        public void IsPlayable_EmptyPile_AnyCardPlayable()
        {
            Card card = new Card(CardRank.Three, CardSuit.Spades);
            Assert.IsTrue(gameState.IsPlayable(card));
        }

        [Test]
        public void IsPlayable_StandardCardGreaterThanTop_Playable()
        {
            pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            Card card = new Card(CardRank.Eight, CardSuit.Spades);

            Assert.IsTrue(gameState.IsPlayable(card));
        }

        [Test]
        public void IsPlayable_StandardCardEqualToTop_Playable()
        {
            pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            Card card = new Card(CardRank.Five, CardSuit.Spades);

            Assert.IsTrue(gameState.IsPlayable(card));
        }

        [Test]
        public void IsPlayable_StandardCardLessThanTop_NotPlayable()
        {
            pile.Add(new Card(CardRank.Eight, CardSuit.Hearts));
            Card card = new Card(CardRank.Five, CardSuit.Spades);

            Assert.IsFalse(gameState.IsPlayable(card));
        }

        [Test]
        public void IsPlayable_AnyJokerPlayableOverStandardCard()
        {
            pile.Add(new Card(CardRank.King, CardSuit.Hearts));

            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Glass)));
            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Black)));
            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Color)));
        }

        [Test]
        public void IsPlayable_AnyCardPlayableOverBlackJoker()
        {
            pile.Add(new Card(JokerType.Black));

            Assert.IsTrue(gameState.IsPlayable(new Card(CardRank.Three, CardSuit.Spades)));
            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Glass)));
            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Color)));
        }

        [Test]
        public void IsPlayable_AnyCardPlayableOverColorJoker()
        {
            pile.Add(new Card(JokerType.Color));

            Assert.IsTrue(gameState.IsPlayable(new Card(CardRank.Nine, CardSuit.Hearts)));
            Assert.IsTrue(gameState.IsPlayable(new Card(JokerType.Glass)));
        }

        [Test]
        public void IsPlayable_GlassJokerIsTransparent_CompareWithCardBelow()
        {
            // Pile : [6♥, Glass, 8♠]
            // Effective top : 8♠
            pile.Add(new Card(CardRank.Six, CardSuit.Hearts));
            pile.Add(new Card(JokerType.Glass));
            pile.Add(new Card(CardRank.Eight, CardSuit.Spades));

            Card playable = new Card(CardRank.Nine, CardSuit.Clubs);
            Card notPlayable = new Card(CardRank.Seven, CardSuit.Diamonds);

            Assert.IsTrue(gameState.IsPlayable(playable));
            Assert.IsFalse(gameState.IsPlayable(notPlayable));
        }

        #endregion

        #region DetectSquare Tests

        [Test]
        public void DetectSquare_PileWithLessThan4Cards_NotSquare()
        {
            pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Five, CardSuit.Spades));
            pile.Add(new Card(CardRank.Five, CardSuit.Clubs));

            Assert.IsFalse(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_4StandardCardsOfSameRank_IsSquare()
        {
            pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Seven, CardSuit.Spades));
            pile.Add(new Card(CardRank.Seven, CardSuit.Clubs));
            pile.Add(new Card(CardRank.Seven, CardSuit.Diamonds));

            Assert.IsTrue(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_4StandardCardsOfDifferentRanks_NotSquare()
        {
            pile.Add(new Card(CardRank.Five, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Six, CardSuit.Spades));
            pile.Add(new Card(CardRank.Seven, CardSuit.Clubs));
            pile.Add(new Card(CardRank.Eight, CardSuit.Diamonds));

            Assert.IsFalse(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_GlassJokersBetweenCards_TransparentDoesntBreakChain()
        {
            // Pile : [9♥, Glass, 9♠, Glass, 9♣, 9♦]
            // Significant : [9♦, 9♣, 9♠, 9♥] => Square
            pile.Add(new Card(CardRank.Nine, CardSuit.Hearts));
            pile.Add(new Card(JokerType.Glass));
            pile.Add(new Card(CardRank.Nine, CardSuit.Spades));
            pile.Add(new Card(JokerType.Glass));
            pile.Add(new Card(CardRank.Nine, CardSuit.Clubs));
            pile.Add(new Card(CardRank.Nine, CardSuit.Diamonds));

            Assert.IsTrue(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_BlackJokerBreaksChain_NoSquare()
        {
            // Pile : [9♥, 9♠, 9♣, Black, 9♦]
            // Significant : [Black, 9♦, 9♣] => stops at Black, only 2 cards => not square
            pile.Add(new Card(CardRank.Nine, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Nine, CardSuit.Spades));
            pile.Add(new Card(CardRank.Nine, CardSuit.Clubs));
            pile.Add(new Card(JokerType.Black));
            pile.Add(new Card(CardRank.Nine, CardSuit.Diamonds));

            Assert.IsFalse(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_ColorJokerBreaksChain_NoSquare()
        {
            pile.Add(new Card(CardRank.King, CardSuit.Hearts));
            pile.Add(new Card(CardRank.King, CardSuit.Spades));
            pile.Add(new Card(CardRank.King, CardSuit.Clubs));
            pile.Add(new Card(JokerType.Color));
            pile.Add(new Card(CardRank.King, CardSuit.Diamonds));

            Assert.IsFalse(gameState.DetectSquare(pile));
        }

        [Test]
        public void DetectSquare_OnlyGlassJokersAboveSquare_SquareStillDetected()
        {
            pile.Add(new Card(CardRank.Jack, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Jack, CardSuit.Spades));
            pile.Add(new Card(CardRank.Jack, CardSuit.Clubs));
            pile.Add(new Card(CardRank.Jack, CardSuit.Diamonds));
            pile.Add(new Card(JokerType.Glass));
            pile.Add(new Card(JokerType.Glass));

            Assert.IsTrue(gameState.DetectSquare(pile));
        }

        #endregion

        #region DetectDoublet Tests

        [Test]
        public void DetectDoublet_FewerThan2Cards_NotDoublet()
        {
            // Aucune carte précédente significative => pas de Doublon possible
            Card card = new Card(CardRank.Five, CardSuit.Hearts);
            
            Assert.IsFalse(gameState.DetectDoublet(card));
        }

        [Test]
        public void DetectDoublet_2CardsOfSameRank_IsDoublet()
        {
            // Première carte significative : 6♥
            Card card1 = new Card(CardRank.Six, CardSuit.Hearts);
            pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = Six
            
            // Deuxième carte : 6♠ (même rang => Doublon)
            Card card2 = new Card(CardRank.Six, CardSuit.Spades);
            
            Assert.IsTrue(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_2CardsOfDifferentRanks_NotDoublet()
        {
            Card card1 = new Card(CardRank.Five, CardSuit.Hearts);
            pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = Five
            
            Card card2 = new Card(CardRank.Seven, CardSuit.Spades);
            
            Assert.IsFalse(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_GlassJokerBetween2Cards_TransparentDoesntBreakChain()
        {
            // Première carte : 8♥
            Card card1 = new Card(CardRank.Eight, CardSuit.Hearts);
            pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = Eight
            
            // Verre empilé (transparent, ne change pas LastSignificantRank)
            pile.Add(new Card(JokerType.Glass));
            
            // Deuxième carte : 8♠ (même rang que la dernière significative => Doublet)
            Card card2 = new Card(CardRank.Eight, CardSuit.Spades);
            
            Assert.IsTrue(gameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_BlackJokerBetween2Cards_BreaksChain()
        {
            // Première carte : 5♥
            Card card1 = new Card(CardRank.Five, CardSuit.Hearts);
            pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);  // LastSignificantRank = Five
            
            // Deuxième carte : 5♠ (même rang, mais on va empiler un Joker Noir)
            Card card2 = new Card(CardRank.Five, CardSuit.Spades);
            pile.Add(card2);
            gameState.UpdateLastSignificantRank(card2);  // LastSignificantRank = Five
            
            // Joker Noir empilé => il casse la chaîne Doublon
            // UpdateLastSignificantRank(Joker Noir) doit RESET LastSignificantRank à null
            Card blackJoker = new Card(JokerType.Black);
            pile.Add(blackJoker);
            gameState.UpdateLastSignificantRank(blackJoker);  // LastSignificantRank = null (reset)
            
            // Troisième carte : 5♦ (5 != null => pas de Doublon)
            Card card3 = new Card(CardRank.Five, CardSuit.Diamonds);
            
            Assert.IsFalse(gameState.DetectDoublet(card3));
        }

        [Test]
        public void DetectDoublet_ColorJokerBetween2Cards_BreaksChain()
        {
            Card card1 = new Card(CardRank.Queen, CardSuit.Hearts);
            pile.Add(card1);
            gameState.UpdateLastSignificantRank(card1);
            
            Card card2 = new Card(CardRank.Queen, CardSuit.Spades);
            pile.Add(card2);
            gameState.UpdateLastSignificantRank(card2);
            
            // Joker Couleur empilé => reset LastSignificantRank
            Card colorJoker = new Card(JokerType.Color);
            pile.Add(colorJoker);
            gameState.UpdateLastSignificantRank(colorJoker);  // LastSignificantRank = null
            
            Card card3 = new Card(CardRank.Queen, CardSuit.Diamonds);
            
            Assert.IsFalse(gameState.DetectDoublet(card3));
        }

        [Test]
        public void DetectDoublet_OnlyTwoPlayersRemain_DisabledReturnsFalse()
        {
            var twoPlayerList = new List<Player>
            {
                new Player("p0", "Player0"),
                new Player("p1", "Player1")
            };
            var twoPlayerGameState = new GameState(twoPlayerList, new Pile(), new Deck());
            
            Card card1 = new Card(CardRank.Three, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Three, CardSuit.Spades);
            
            twoPlayerGameState.UpdateLastSignificantRank(card1);  // Initialise tracker
            
            // Même rang, mais seulement 2 joueurs => Doublon désactivé
            Assert.IsFalse(twoPlayerGameState.DetectDoublet(card2));
        }

        [Test]
        public void DetectDoublet_3PlayersRemain_Enabled()
        {
            var threePlayerList = new List<Player>
            {
                new Player("p0", "Player0"),
                new Player("p1", "Player1"),
                new Player("p2", "Player2")
            };
            var threePlayerGameState = new GameState(threePlayerList, new Pile(), new Deck());
            
            Card card1 = new Card(CardRank.Four, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Four, CardSuit.Spades);
            
            threePlayerGameState.UpdateLastSignificantRank(card1);
            
            // 3 joueurs => Doublon actif
            Assert.IsTrue(threePlayerGameState.DetectDoublet(card2));
        }

        #endregion

        #region PickUpPile Tests

        [Test]
        public void PickUpPile_EmptyPile_ReturnsFalse()
        {
            Player player = players[0];
            bool result = gameState.PickUpPile(player);

            Assert.IsFalse(result);
        }

        [Test]
        public void PickUpPile_PileWithCards_AllCardsAddedToHand()
        {
            Player player = players[0];
            Card card1 = new Card(CardRank.Five, CardSuit.Hearts);
            Card card2 = new Card(CardRank.Nine, CardSuit.Spades);

            pile.Add(card1);
            pile.Add(card2);

            bool result = gameState.PickUpPile(player);

            Assert.IsTrue(result);
            Assert.AreEqual(2, player.Hand.Count);
            Assert.Contains(card1, player.Hand);
            Assert.Contains(card2, player.Hand);
        }

        [Test]
        public void PickUpPile_PileCleared_IsEmpty()
        {
            Player player = players[0];
            pile.Add(new Card(CardRank.King, CardSuit.Clubs));
            pile.Add(new Card(CardRank.Ace, CardSuit.Diamonds));

            gameState.PickUpPile(player);

            Assert.IsTrue(pile.IsEmpty());
        }

        #endregion

        #region RefillHand Tests

        [Test]
        public void RefillHand_HandLessThan3_FillsUpTo3()
        {
            Player player = players[0];
            player.AddCardToHand(new Card(CardRank.Two, CardSuit.Hearts));

            gameState.RefillHand(player);

            Assert.AreEqual(3, player.Hand.Count);
        }

        [Test]
        public void RefillHand_HandAlreadyHas3_NoChange()
        {
            Player player = players[0];
            player.AddCardToHand(new Card(CardRank.Two, CardSuit.Hearts));
            player.AddCardToHand(new Card(CardRank.Three, CardSuit.Spades));
            player.AddCardToHand(new Card(CardRank.Four, CardSuit.Clubs));

            int initialDeckCount = deck.Count;
            gameState.RefillHand(player);

            Assert.AreEqual(3, player.Hand.Count);
            Assert.AreEqual(initialDeckCount, deck.Count);
        }

        [Test]
        public void RefillHand_DeckEmpty_HandStaysSmall()
        {
            Player player = players[0];
            player.AddCardToHand(new Card(CardRank.Five, CardSuit.Hearts));

            // Drain deck
            while (deck.Count > 0)
                deck.Draw();

            gameState.RefillHand(player);

            Assert.AreEqual(1, player.Hand.Count);
        }

        #endregion

        #region ResolvePileDestruction Tests

        [Test]
        public void ResolvePileDestruction_EmptyPile_NoOp()
        {
            gameState.ResolvePileDestruction(DestructionReason.Square);
            Assert.IsTrue(pile.IsEmpty());
        }

        [Test]
        public void ResolvePileDestruction_PileWithCards_AllCardsRemoved()
        {
            pile.Add(new Card(CardRank.Seven, CardSuit.Hearts));
            pile.Add(new Card(CardRank.Nine, CardSuit.Spades));
            pile.Add(new Card(CardRank.King, CardSuit.Clubs));

            gameState.ResolvePileDestruction(DestructionReason.Two);

            Assert.IsTrue(pile.IsEmpty());
        }

        [Test]
        public void ResolvePileDestruction_AnyReason_ClearsTheStack()
        {
            pile.Add(new Card(CardRank.Ace, CardSuit.Diamonds));
            pile.Add(new Card(JokerType.Black));

            gameState.ResolvePileDestruction(DestructionReason.Bomb);

            Assert.IsTrue(pile.IsEmpty());
        }

        #endregion

        #region PlayCard Integration Tests

        [Test]
        public void PlayCard_ValidCardToEmptyPile_Success()
        {
            Player player = players[0];
            Card card = new Card(CardRank.Six, CardSuit.Hearts);
            player.AddCardToHand(card);

            bool result = gameState.PlayCard(player, card);

            Assert.IsTrue(result);
            Assert.IsFalse(player.Hand.Contains(card));
            Assert.AreEqual(card, pile.Top());
        }

        [Test]
        public void PlayCard_InvalidCard_NotInHand()
        {
            Player player = players[0];
            Card card = new Card(CardRank.Eight, CardSuit.Hearts);

            bool result = gameState.PlayCard(player, card);

            Assert.IsFalse(result);
        }

        [Test]
        public void PlayCard_InvalidCard_NotPlayable()
        {
            Player player = players[0];
            Card lower = new Card(CardRank.Three, CardSuit.Hearts);
            Card higher = new Card(CardRank.King, CardSuit.Spades);

            pile.Add(higher);
            player.AddCardToHand(lower);

            bool result = gameState.PlayCard(player, lower);

            Assert.IsFalse(result);
        }

        [Test]
        public void PlayCard_Square_PileDestroyedSamePlayerRejoins()
        {
            Player p0 = players[0];
            Card card4 = new Card(CardRank.Jack, CardSuit.Hearts);
            Card card3 = new Card(CardRank.Jack, CardSuit.Spades);
            Card card2 = new Card(CardRank.Jack, CardSuit.Clubs);
            Card card1 = new Card(CardRank.Jack, CardSuit.Diamonds);

            p0.AddCardToHand(card1);
            p0.AddCardToHand(card2);
            p0.AddCardToHand(card3);
            p0.AddCardToHand(card4);

            pile.Add(card1);
            pile.Add(card2);
            pile.Add(card3);

            bool result = gameState.PlayCard(p0, card4);

            Assert.IsTrue(result);
            Assert.IsTrue(pile.IsEmpty());
            Assert.AreEqual(p0, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void PlayCard_Two_NormalCase_PileDestroyedNextPlayer()
        {
            Player p0 = players[0];
            Player p1 = players[1];
            Card two = new Card(CardRank.Two, CardSuit.Hearts);

            p0.AddCardToHand(two);
            p0.AddCardToHand(new Card(CardRank.Five, CardSuit.Spades));

            gameState.PlayCard(p0, two);

            Assert.IsTrue(pile.IsEmpty());
            Assert.AreEqual(p0, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void PlayCard_Bomb_PileDestroyedNextPlayer()
        {
            Player p0 = players[0];
            Player p1 = players[1];
            Card bomb = new Card(JokerType.Color);

            p0.AddCardToHand(bomb);

            gameState.PlayCard(p0, bomb);

            Assert.IsTrue(pile.IsEmpty());
            Assert.AreEqual(p1, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void PlayCard_Doublet_SkipsNextPlayerAdvancesToFollowing()
        {
            Player p0 = players[0];
            Player p1 = players[1];
            Player p2 = players[2];

            // Arrange : la pile contient déjà un 4 (posé "avant" le test),
            // et le tracker Doublon reflète cet état.
            pile.Add(new Card(CardRank.Four, CardSuit.Clubs));
            gameState.UpdateLastSignificantRank(new Card(CardRank.Four, CardSuit.Clubs));

            // p0 joue un second 4 consécutif => Doublon
            Card doubletCard = new Card(CardRank.Four, CardSuit.Hearts);
            p0.AddCardToHand(doubletCard);
            p0.AddCardToHand(new Card(CardRank.Five, CardSuit.Spades));

            // Act
            bool played = gameState.PlayCard(p0, doubletCard);

            // Assert
            Assert.IsTrue(played, "Le Doublon doit être une pose valide");
            Assert.AreEqual(p2, gameState.TurnManager.CurrentTurn.CurrentPlayer,
                "Le Doublon doit sauter p1 : p2 doit jouer ensuite");
        }
        [Test]
        public void PlayCard_SevenCard_PlayerWhoPlayedSevenRefilled()
        {
            Player p0 = players[0];
            Card seven = new Card(CardRank.Seven, CardSuit.Hearts);
            p0.AddCardToHand(seven);
            p0.AddCardToHand(new Card(CardRank.Eight, CardSuit.Spades));
            // p0.Hand = [7♥, 8♠] (2 cartes)

            int deckBefore = deck.Count;

            // Act
            bool result = gameState.PlayCard(p0, seven);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3, p0.Hand.Count); // 1 restante (8♠) + 2 piochées = 3
            Assert.AreEqual(deckBefore - 2, deck.Count); // 2 cartes piochées pour remplir à 3
        }
        #endregion
    }
}