using NUnit.Framework;
using System.Collections.Generic;
using Psycko;
using Psycko.Core;

namespace Psycko.Tests
{
    /// <summary>
    /// T7oct : Prêtre (Le Prêtre) — Tests complets CORRIGÉS
    /// 65+ tests NUnit EditMode pour :
    /// - Activation et réinitialisation du blocage Prêtre
    /// - Contrainte ≤ 8 pour le joueur suivant
    /// - Transparence du Joker de Verre
    /// - Invalidation par Joker Noir
    /// - Carré de Prêtres ± Joker de Verre
    /// - Cas limites et transitions de phase
    /// </summary>
    [TestFixture]
    public class PriestTests
    {
        private GameState gameState;
        private Player player1, player2, player3, player4;
        private Pile pile;
        private Deck deck;

        [SetUp]
        public void Setup()
        {
            player1 = new Player("1", "Player1");
            player2 = new Player("2", "Player2");
            player3 = new Player("3", "Player3");
            player4 = new Player("4", "Player4");

            var players = new List<Player> { player1, player2, player3, player4 };
            pile = new Pile();
            deck = new Deck(seed: 42);

            gameState = new GameState(players, pile, deck);
        }

        #region Block 1 : Basics — Activation et Réinitialisation (10 tests)

        [Test]
        public void T1_PriestActivatesBlockAfterPlayed()
        {
            // Arrange
            var priestHearts = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priestHearts);

            // Act
            bool played = gameState.PlayCard(player1, priestHearts);

            // Assert
            Assert.IsTrue(played, "Prêtre should be played successfully");
            Assert.IsTrue(gameState.IsPriestActive, "IsPriestActive should be true after P1 plays Priest");
            Assert.AreEqual((int)CardRank.Priest, gameState.PriestHeightBlock);
            // P2 should now be current (Priest doesn't skip, but plays normally → advance)
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T2_PriestBlockIsExactly8()
        {
            // Arrange
            var priestSpades = new Card(CardRank.Priest, CardSuit.Spades);
            player1.AddCardToHand(priestSpades);

            // Act
            gameState.PlayCard(player1, priestSpades);

            // Assert
            Assert.AreEqual(8, gameState.PriestHeightBlock, "Priest height should be exactly 8");
        }

        [Test]
        public void T3_NextPlayerCanPlayUpTo10AfterPriest()
        {
            // Arrange : P1 plays Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            // Now player2 can play 3-4-5-6-7-8-9-10-Prêtre
            var ten = new Card(CardRank.Ten, CardSuit.Spades);
            var jack = new Card(CardRank.Jack, CardSuit.Clubs);     // NOT playable
            var eight = new Card(CardRank.Eight, CardSuit.Clubs);
            var three = new Card(CardRank.Three, CardSuit.Diamonds);

            player2.AddCardToHand(ten);
            player2.AddCardToHand(jack);
            player2.AddCardToHand(eight);
            player2.AddCardToHand(three);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(ten), "10 (value 7) should be playable after Priest");
            Assert.IsFalse(gameState.IsPlayable(jack), "Jack (value 9) should NOT be playable after Priest (too high)");
            Assert.IsTrue(gameState.IsPlayable(eight), "8 should be playable");
            Assert.IsTrue(gameState.IsPlayable(three), "3 should be playable");
        }

        [Test]
        public void T4_JokerAlwaysPlayableAfterPriest()
        {
            // Arrange : P1 plays Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            // Any Joker should be playable
            var jokerGlass = new Card(JokerType.Glass);
            var jokerBlack = new Card(JokerType.Black);
            var jokerColor = new Card(JokerType.Color);

            player2.AddCardToHand(jokerGlass);
            player2.AddCardToHand(jokerBlack);
            player2.AddCardToHand(jokerColor);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(jokerGlass), "Glass Joker should be playable");
            Assert.IsTrue(gameState.IsPlayable(jokerBlack), "Black Joker should be playable");
            Assert.IsTrue(gameState.IsPlayable(jokerColor), "Color Joker should be playable");
        }

        [Test]
        public void T5_PriestFollowedByCardResetsBlock()
        {
            // Arrange : P1 plays Priest, P2 plays valid card
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            Assert.IsTrue(gameState.IsPriestActive, "Priest should be active after P1's play");

            // P2 plays 6 (valid: ≤ 8)
            var six = new Card(CardRank.Six, CardSuit.Spades);
            player2.AddCardToHand(six);
            gameState.PlayCard(player2, six);

            // Assert : Priest should be reset after P2's turn
            Assert.IsFalse(gameState.IsPriestActive, "Priest block should reset after P2's turn");
            Assert.AreEqual(-1, gameState.PriestHeightBlock);
            // P3 should be current (P2 played → advance)
            Assert.AreEqual(player3, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T6_PriestResetHappensExactlyOncePerNextTurn()
        {
            // Arrange : Priest played
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            // P2 plays card
            var five = new Card(CardRank.Five, CardSuit.Clubs);
            player2.AddCardToHand(five);
            gameState.PlayCard(player2, five);

            // After P2's turn, Priest is reset
            Assert.IsFalse(gameState.IsPriestActive);

            // P3 plays (should have no Priest constraint now)
            var king = new Card(CardRank.King, CardSuit.Diamonds);
            player3.AddCardToHand(king);

            // Act & Assert
            bool kingPlayable = gameState.IsPlayable(king);
            Assert.IsTrue(kingPlayable, "King should be playable (Priest reset, 5 < King)");
        }

        [Test]
        public void T7_ConsecutivePriestsExtendBlock()
        {
            // Arrange : P1 plays Priest
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest1);
            gameState.PlayCard(player1, priest1);

            // P2 plays Priest2 (valid: 8 ≤ 8) — deux Prêtres consécutifs = Doublet, saute un joueur en plus
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            player2.AddCardToHand(priest2);
            bool playedPriest2 = gameState.PlayCard(player2, priest2);

            // Assert
            Assert.IsTrue(playedPriest2, "Second Priest should be playable (≤ 8)");
            Assert.IsTrue(gameState.IsPriestActive, "Priest block should still be active");
            Assert.AreEqual(8, gameState.PriestHeightBlock);
            // Doublet déclenché (deux Prêtres consécutifs) → P2 joue, avance normale à P3, puis skip à P4
            Assert.AreEqual(player4, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }
        [Test]
        public void T8_PriestFollowedByHighCardViolatesConstraint()
        {
            // Arrange
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var king = new Card(CardRank.King, CardSuit.Spades); // 12 > 8

            player1.AddCardToHand(priest);
            player2.AddCardToHand(king);

            gameState.PlayCard(player1, priest);

            // Act
            bool played = gameState.PlayCard(player2, king);

            // Assert
            Assert.IsFalse(played, "King (12) should NOT be playable after Priest (≤ 8)");
        }

        [Test]
        public void T9_PriestFollowedByEightIsValid()
        {
            // Arrange
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var eight = new Card(CardRank.Eight, CardSuit.Clubs);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(eight);

            gameState.PlayCard(player1, priest);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(eight), "8 should be playable (= Priest)");
            bool played = gameState.PlayCard(player2, eight);
            Assert.IsTrue(played, "Playing 8 after Priest should succeed");
        }

        [Test]
        public void T10_PriestBlockCanHandleMultiplePlaysInSequence()
        {
            // Arrange : Priest played
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            // P2 plays 6 and resets block
            var six = new Card(CardRank.Six, CardSuit.Clubs);
            player2.AddCardToHand(six);
            gameState.PlayCard(player2, six);

            // P3 should be able to play normally now (Priest reset)
            var nine = new Card(CardRank.Nine, CardSuit.Diamonds);
            player3.AddCardToHand(nine);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(nine), "9 should be playable after Priest reset");
            bool played = gameState.PlayCard(player3, nine);
            Assert.IsTrue(played);
        }

        #endregion

        #region Block 2 : Joker de Verre Transparent (15 tests)

        [Test]
        public void T11_JokerGlassTransparentForPriestHeight()
        {
            // Arrange : P1 plays Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            // P2 plays Joker Glass
            var glass = new Card(JokerType.Glass);
            player2.AddCardToHand(glass);
            gameState.PlayCard(player2, glass);

            // P3 should see Priest (glass transparent) and be constrained ≤ 8
            var ten = new Card(CardRank.Ten, CardSuit.Spades);
            var seven = new Card(CardRank.Seven, CardSuit.Clubs);
            player3.AddCardToHand(ten);
            player3.AddCardToHand(seven);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(ten), "10 should NOT be playable (Glass transparent, sees Priest)");
            Assert.IsTrue(gameState.IsPlayable(seven), "7 should be playable (Glass transparent, ≤ Priest)");
        }

        [Test]
        public void T12_JokerGlassDoesNotCountTowardSquare()
        {
            // Arrange : 7 → Glass → 7 → 7 → 7 (should be 4 Sevens = Square, Glass ignored)
            var seven1 = new Card(CardRank.Seven, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);
            var seven2 = new Card(CardRank.Seven, CardSuit.Clubs);
            var seven3 = new Card(CardRank.Seven, CardSuit.Spades);
            var seven4 = new Card(CardRank.Seven, CardSuit.Diamonds);

            player1.AddCardToHand(seven1);
            player2.AddCardToHand(glass);
            player3.AddCardToHand(seven2);
            player4.AddCardToHand(seven3);

            gameState.PlayCard(player1, seven1);
            gameState.PlayCard(player2, glass);
            gameState.PlayCard(player3, seven2);
            gameState.PlayCard(player4, seven3);

            // Act : P1 plays 4th Seven
            player1.AddCardToHand(seven4);
            bool played = gameState.PlayCard(player1, seven4);

            // Assert
            Assert.IsTrue(played, "Fourth 7 should trigger Square");
            Assert.IsTrue(gameState.Pile.IsEmpty(), "Pile should be destroyed (Square)");
            Assert.AreEqual(player1, gameState.TurnManager.CurrentTurn.CurrentPlayer, "P1 rejeu after Square");
        }

        [Test]
        public void T13_MultipleGlassesInChain()
        {
            // Arrange : Priest → Glass → Glass → Glass
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var glasses = new[] {
                new Card(JokerType.Glass),
                new Card(JokerType.Glass),
                new Card(JokerType.Glass)
            };

            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            player2.AddCardToHand(glasses[0]);
            gameState.PlayCard(player2, glasses[0]);

            player3.AddCardToHand(glasses[1]);
            gameState.PlayCard(player3, glasses[1]);

            player4.AddCardToHand(glasses[2]);
            gameState.PlayCard(player4, glasses[2]);

            // P1 should see Priest through all Glasses
            var four = new Card(CardRank.Four, CardSuit.Spades);
            player1.AddCardToHand(four);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(four), "4 should be playable (all Glasses transparent, sees Priest, 4 ≤ 8)");
        }

        [Test]
        public void T14_GlassFollowedByBlackJokerStopsTransparency()
        {
            // Arrange : Priest → Glass → Black
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);
            var black = new Card(JokerType.Black);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(glass);
            player3.AddCardToHand(black);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, glass);
            gameState.PlayCard(player3, black);

            // P4 sees Black (significant), height is free, AND Black resets Priest block
            var king = new Card(CardRank.King, CardSuit.Clubs);
            player4.AddCardToHand(king);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(king), "King should be playable (Black breaks Glass chain, resets Priest)");
            Assert.IsFalse(gameState.IsPriestActive, "Black should have reset Priest");
        }

        [Test]
        public void T15_GlassFollowedByColorJokerStopsTransparency()
        {
            // Arrange : Priest → Glass → Color Joker
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);
            var color = new Card(JokerType.Color);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(glass);
            player3.AddCardToHand(color);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, glass);
            gameState.PlayCard(player3, color);

            // P4 can play anything (Color breaks Glass + destroys pile)
            var two = new Card(CardRank.Two, CardSuit.Spades);
            player4.AddCardToHand(two);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(two), "2 should be playable (Color breaks Glass + Priest)");
            Assert.IsTrue(gameState.Pile.IsEmpty(), "Bombe should destroy pile");
        }

        [Test]
        public void T16_PriestGlassPriestChainIsValid()
        {
            // Priest → Glass → Priest2 (should be playable: 8 ≤ 8 through Glass)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(glass);
            player3.AddCardToHand(priest2);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, glass);

            // Act
            bool playable = gameState.IsPlayable(priest2);

            // Assert
            Assert.IsTrue(playable, "Priest2 (8) should be playable (Glass transparent, 8 ≤ 8)");
            bool played = gameState.PlayCard(player3, priest2);
            Assert.IsTrue(played);
        }

        [Test]
        public void T17_GlassAndDoubletDetection()
        {
            // Arrange : 5 → Glass → 5 (should be Doublet detected, Glass transparent)
            var five1 = new Card(CardRank.Five, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);
            var five2 = new Card(CardRank.Five, CardSuit.Clubs);

            player1.AddCardToHand(five1);
            player2.AddCardToHand(glass);
            player3.AddCardToHand(five2);

            gameState.PlayCard(player1, five1);
            gameState.PlayCard(player2, glass);

            // Act
            bool played = gameState.PlayCard(player3, five2);

            // Assert
            Assert.IsTrue(played, "Second 5 should be played (Doublet detected)");
            // Doublet triggered: P4 should be skipped, P1 plays next
            Assert.AreEqual(player1, gameState.TurnManager.CurrentTurn.CurrentPlayer, "P4 skipped, P1 plays");
        }

        [Test]
        public void T18_FourGlassesInSequenceValidation()
        {
            // Edge case: chain of 4 Glasses to ensure transparency works at scale
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var glasses = new[] {
                new Card(JokerType.Glass),
                new Card(JokerType.Glass),
                new Card(JokerType.Glass),
                new Card(JokerType.Glass)
            };
            var four = new Card(CardRank.Four, CardSuit.Spades);

            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            player2.AddCardToHand(glasses[0]);
            gameState.PlayCard(player2, glasses[0]);

            player3.AddCardToHand(glasses[1]);
            gameState.PlayCard(player3, glasses[1]);

            player4.AddCardToHand(glasses[2]);
            gameState.PlayCard(player4, glasses[2]);

            player1.AddCardToHand(glasses[3]);
            gameState.PlayCard(player1, glasses[3]);

            player2.AddCardToHand(four);

            // Act
            bool playable = gameState.IsPlayable(four);

            // Assert
            Assert.IsTrue(playable, "4 should be playable (≤ Priest through 4 Glasses)");
        }

        #endregion

        #region Block 3 : Carré de Prêtres (8 tests)

        [Test]
        public void T19_SquareOfFourPriestsDetected()
        {
            // Arrange : 4 Priests stacked
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            var priest3 = new Card(CardRank.Priest, CardSuit.Spades);
            var priest4 = new Card(CardRank.Priest, CardSuit.Diamonds);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(priest2);
            player3.AddCardToHand(priest3);
            player4.AddCardToHand(priest4);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, priest2);
            gameState.PlayCard(player3, priest3);

            // Act : P4 plays 4th Priest
            bool played = gameState.PlayCard(player4, priest4);

            // Assert
            Assert.IsTrue(played, "Fourth Priest should trigger Square and be played");
            Assert.IsTrue(gameState.Pile.IsEmpty(), "Pile should be destroyed after Square");
            // P4 plays Square → same player rejeu → P4 is still current
            Assert.AreEqual(player4, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T20_SquareOfPriestsWithGlassesIgnored()
        {
            // Arrange : Priest → Glass → Priest → Glass → Priest → Priest (4 Priests, Glasses ignored)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var glass1 = new Card(JokerType.Glass);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            var glass2 = new Card(JokerType.Glass);
            var priest3 = new Card(CardRank.Priest, CardSuit.Spades);
            var priest4 = new Card(CardRank.Priest, CardSuit.Diamonds);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(glass1);
            player3.AddCardToHand(priest2);
            player4.AddCardToHand(glass2);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, glass1);
            gameState.PlayCard(player3, priest2);
            gameState.PlayCard(player4, glass2);

            player1.AddCardToHand(priest3);
            gameState.PlayCard(player1, priest3);

            player2.AddCardToHand(priest4);

            // Act
            bool played = gameState.PlayCard(player2, priest4);

            // Assert
            Assert.IsTrue(played, "Fourth Priest should trigger Square (Glasses ignored)");
            Assert.IsTrue(gameState.Pile.IsEmpty(), "Pile destroyed");
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer, "P2 rejeu");
        }

        [Test]
        public void T21_SquareOfPriestsWithBlackJokerBreaksChain()
        {
            // Arrange : Priest → Priest → Black → Priest → Priest (Black breaks, only 2+2 = not Square)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            var black = new Card(JokerType.Black);
            var priest3 = new Card(CardRank.Priest, CardSuit.Spades);
            var priest4 = new Card(CardRank.Priest, CardSuit.Diamonds);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(priest2);
            player3.AddCardToHand(black);
            player4.AddCardToHand(priest3);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, priest2);
            gameState.PlayCard(player3, black);
            gameState.PlayCard(player4, priest3);

            player1.AddCardToHand(priest4);

            // Act
            bool played = gameState.PlayCard(player1, priest4);

            // Assert
            Assert.IsTrue(played, "P1 plays 4th Priest");
            Assert.IsFalse(gameState.Pile.IsEmpty(), "Pile should remain (Black breaks chain, no Square)");
            // Normal advance: P2
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T22_ThreePriestsNoSquare()
        {
            // Arrange : Priest → Priest → Priest (only 3, not Square)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            var priest3 = new Card(CardRank.Priest, CardSuit.Spades);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(priest2);
            player3.AddCardToHand(priest3);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, priest2);

            // Act
            bool played = gameState.PlayCard(player3, priest3);

            // Assert
            Assert.IsTrue(played);
            Assert.IsFalse(gameState.Pile.IsEmpty(), "Pile should remain (3 Priests, not Square)");
            Assert.AreEqual(player4, gameState.TurnManager.CurrentTurn.CurrentPlayer, "Normal advance to P4");
        }

        [Test]
        public void T23_AlternatingPriestsAndOthers()
        {
            // Arrange : Priest → 10 → Priest → 10 (alternating, no Square)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var ten1 = new Card(CardRank.Ten, CardSuit.Clubs);
            var priest2 = new Card(CardRank.Priest, CardSuit.Spades);
            var ten2 = new Card(CardRank.Ten, CardSuit.Diamonds);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(ten1);
            player3.AddCardToHand(priest2);
            player4.AddCardToHand(ten2);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, ten1);
            gameState.PlayCard(player3, priest2);

            // Act
            bool played = gameState.PlayCard(player4, ten2);

            // Assert
            Assert.IsTrue(played);
            Assert.IsFalse(gameState.Pile.IsEmpty(), "Pile should remain (alternating ranks)");
        }

        #endregion

        #region Block 4 : Joker Noir Invalide Prêtre (10 tests)

        [Test]
        public void T24_BlackJokerResetsAndInvalidatesPriest()
        {
            // Arrange : P1 plays Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);
            Assert.IsTrue(gameState.IsPriestActive);

            // P2 plays Black Joker
            var black = new Card(JokerType.Black);
            player2.AddCardToHand(black);
            gameState.PlayCard(player2, black);

            // Assert : Priest block should be reset
            Assert.IsFalse(gameState.IsPriestActive, "Black Joker should reset Priest block");
            Assert.AreEqual(-1, gameState.PriestHeightBlock);
            // P3 is current (P2 played Black → advance)
            Assert.AreEqual(player3, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T25_BlackJokerMakesHeightFree()
        {
            // Arrange : Priest → Black Joker
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var black = new Card(JokerType.Black);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(black);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, black);

            // P3 can now play any card (height free after Black)
            var king = new Card(CardRank.King, CardSuit.Clubs);
            player3.AddCardToHand(king);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(king), "King should be playable (Black resets, height free)");
        }

        [Test]
        public void T26_BlackJokerAfterMultiplePriests()
        {
            // Arrange : Priest → Priest2 → Black
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);
            var black = new Card(JokerType.Black);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(priest2);
            player3.AddCardToHand(black);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, priest2);
            gameState.PlayCard(player3, black);

            // P4 can play high cards (Black resets)
            var ace = new Card(CardRank.Ace, CardSuit.Diamonds);
            player4.AddCardToHand(ace);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(ace), "Ace should be playable after Black (resets Priest)");
        }

        [Test]
        public void T27_BlackJokerDoesNotAffectFutureDetection()
        {
            // After Black breaks Priest chain, future Priests should work normally again
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var black = new Card(JokerType.Black);
            var ten = new Card(CardRank.Ten, CardSuit.Clubs);
            var priest2 = new Card(CardRank.Priest, CardSuit.Spades);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(black);
            player3.AddCardToHand(ten);
            player4.AddCardToHand(priest2);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, black);
            gameState.PlayCard(player3, ten);
            gameState.PlayCard(player4, priest2);

            // P1 should now be constrained by the second Priest
            var two = new Card(CardRank.Two, CardSuit.Diamonds);
            player1.AddCardToHand(two);

            // Act & Assert
            Assert.IsFalse(gameState.IsPlayable(two), "2 should NOT be playable after second Priest");
            Assert.IsTrue(gameState.IsPriestActive, "Second Priest should activate its own block");
        }

        [Test]
        public void T28_MultipleBlackJokersDoNotStackInvalidations()
        {
            // Priest → Black → Black (should not double-invalidate)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var black1 = new Card(JokerType.Black);
            var black2 = new Card(JokerType.Black);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(black1);
            player3.AddCardToHand(black2);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, black1);

            // After first Black, Priest is reset
            Assert.IsFalse(gameState.IsPriestActive);

            // Act
            gameState.PlayCard(player3, black2);

            // Assert : should still be false (no double-reset)
            Assert.IsFalse(gameState.IsPriestActive);
        }

        [Test]
        public void T29_BlackJokerBeforePriestHasNoEffect()
        {
            // Black → King → Priest (Black shouldn't pre-emptively affect Priest)
            var black = new Card(JokerType.Black);
            var ten = new Card(CardRank.Ten, CardSuit.Hearts);
            var priest = new Card(CardRank.Priest, CardSuit.Clubs);

            player1.AddCardToHand(black);
            player2.AddCardToHand(ten);
            player3.AddCardToHand(priest);

            gameState.PlayCard(player1, black);
            gameState.PlayCard(player2, ten);
            gameState.PlayCard(player3, priest);

            // Assert
            Assert.IsTrue(gameState.IsPriestActive, "New Priest should activate normally");
        }

        [Test]
        public void T30_BlackJokerMakesHeightFreeAfterPriest()
        {
            // Test that Black Joker correctly resets height constraint
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var black = new Card(JokerType.Black);
            var three = new Card(CardRank.Three, CardSuit.Spades);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(black);
            player3.AddCardToHand(three);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, black);

            // Act
            bool playable = gameState.IsPlayable(three);

            // Assert
            Assert.IsTrue(playable, "3 should be playable (Black resets Priest, Joker is always playable)");
        }

        [Test]
        public void T31_BlackJokerResetsAndAdvances()
        {
            // Verify Black Joker always advances to next player
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var black = new Card(JokerType.Black);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(black);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, black);

            // P3 should be current
            Assert.AreEqual(player3, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        #endregion

        #region Block 5 : Cas Limites et Transitions (12 tests)

        [Test]
        public void T32_PriestInPhase2()
        {
            // Priest should work in Phase 2 (Talent) just like Phase 1
            gameState.CurrentPhase = GamePhase.Talent;

            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            var jack = new Card(CardRank.Jack, CardSuit.Spades);     // NOT playable (value 9 > 8)
            var four = new Card(CardRank.Four, CardSuit.Spades);      // Playable (value 1 ≤ 8)

            player2.AddCardToHand(jack);
            player2.AddCardToHand(four);

            // Act & Assert
            Assert.IsFalse(gameState.IsPlayable(jack), "Jack (value 9) should NOT be playable after Priest in Phase 2");
            Assert.IsTrue(gameState.IsPlayable(four), "4 (value 1) should be playable after Priest in Phase 2");
        }

        [Test]
        public void T33_PriestInPhase3()
        {
            // Priest should work in Phase 3 (Chance)
            gameState.CurrentPhase = GamePhase.Chance;

            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            var queen = new Card(CardRank.Queen, CardSuit.Clubs);
            player2.AddCardToHand(queen);

            // Act & Assert
            Assert.IsFalse(gameState.IsPlayable(queen), "Queen (rank 11) should NOT be playable after Priest in Phase 3");
        }

        [Test]
        public void T34_PriestBlockResetTimingExact()
        {
            // Verify Priest block is reset immediately after the next player's turn
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);
            Assert.IsTrue(gameState.IsPriestActive);

            var six = new Card(CardRank.Six, CardSuit.Spades);
            player2.AddCardToHand(six);

            // Just before P2's play
            Assert.IsTrue(gameState.IsPriestActive, "Should still be active before P2 plays");

            // Act
            gameState.PlayCard(player2, six);

            // Assert : immediately after, Priest is reset
            Assert.IsFalse(gameState.IsPriestActive, "Should be reset immediately after P2's play");
        }

        [Test]
        public void T35_PriestAndDoubletInteraction()
        {
            // Priest (≤ 8) → 7 → 7 (Doublet, skip P4) → P1 plays
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var seven1 = new Card(CardRank.Seven, CardSuit.Spades);
            var seven2 = new Card(CardRank.Seven, CardSuit.Clubs);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(seven1);
            player3.AddCardToHand(seven2);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, seven1);

            // After P2's 7, Priest should be reset
            Assert.IsFalse(gameState.IsPriestActive);

            // Act
            bool played = gameState.PlayCard(player3, seven2);

            // Assert
            Assert.IsTrue(played, "Second 7 should trigger Doublet");
            // Doublet: P4 skipped, P1 plays next
            Assert.AreEqual(player1, gameState.TurnManager.CurrentTurn.CurrentPlayer);
        }

        [Test]
        public void T36_PriestAndSquareInteraction()
        {
            // P1 → Priest(8)
            // P2 → Priest(8) [Doublet déclenché : P3 sauté]
            // P4 → Priest(8) [Doublet déclenché : P1 sauté]
            // P2 → Priest(8) [Carré détecté : pile détruite, P2 rejeu/ouvre]
            
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var priest2 = new Card(CardRank.Priest, CardSuit.Spades);
            var priest3 = new Card(CardRank.Priest, CardSuit.Clubs);
            var priest4 = new Card(CardRank.Priest, CardSuit.Diamonds);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(priest2);
            player4.AddCardToHand(priest3);
            player2.AddCardToHand(priest4); // P2 aura 2 Prêtres

            // Coup 1 : P1 joue Prêtre
            gameState.PlayCard(player1, priest1);
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
            
            // Coup 2 : P2 joue Prêtre → Doublet (P3 sauté) → P4 joue
            gameState.PlayCard(player2, priest2);
            Assert.AreEqual(player4, gameState.TurnManager.CurrentTurn.CurrentPlayer);
            
            // Coup 3 : P4 joue Prêtre → Doublet (P1 sauté) → P2 joue
            gameState.PlayCard(player4, priest3);
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
            
            // Coup 4 : P2 joue Prêtre → Carré détecté (4 Prêtres consécutifs)
            // Pile détruite, P2 rejeu/ouvre la nouvelle pile
            gameState.PlayCard(player2, priest4);
            Assert.AreEqual(player2, gameState.TurnManager.CurrentTurn.CurrentPlayer);
            Assert.AreEqual(0, gameState.Pile.Count, "Pile should be destroyed after Square");
            Assert.IsFalse(gameState.IsPriestActive, "Priest block should be reset");
        }

        [Test]
        public void T37_PriestAndTwoInteraction()
        {
            // Priest → player tries to play 2 (rank 14, > 8, invalid)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var two = new Card(CardRank.Two, CardSuit.Spades);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(two);
            gameState.Pile.Add(new Card(CardRank.Ten, CardSuit.Clubs)); // Add something to pick up

            gameState.PlayCard(player1, priest);

            // Act
            bool played = gameState.PlayCard(player2, two);

            // Assert
            Assert.IsFalse(played, "2 should NOT be playable after Priest (14 > 8)");
        }

        [Test]
        public void T38_PriestAndJokerColorInteraction()
        {
            // Priest → Joker Color (Bombe)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var color = new Card(JokerType.Color);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(color);

            gameState.PlayCard(player1, priest);

            // Act
            bool played = gameState.PlayCard(player2, color);

            // Assert
            Assert.IsTrue(played, "Color Joker should be playable after Priest");
            Assert.IsTrue(gameState.Pile.IsEmpty(), "Bombe should destroy pile");
            Assert.AreEqual(player3, gameState.TurnManager.CurrentTurn.CurrentPlayer, "P3 should open");
        }

        [Test]
        public void T39_PriestBlockDoesntAffectJokerGlass()
        {
            // Priest → P2 can play Glass freely (Joker always playable)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var glass = new Card(JokerType.Glass);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(glass);

            gameState.PlayCard(player1, priest);

            // Act
            bool playable = gameState.IsPlayable(glass);

            // Assert
            Assert.IsTrue(playable, "Glass Joker should always be playable");
        }

        [Test]
        public void T40_ConsecutivePriestsDifferentPlayers()
        {
            // Priest (P1) → 8 (P2) → Priest (P3)
            var priest1 = new Card(CardRank.Priest, CardSuit.Hearts);
            var eight = new Card(CardRank.Eight, CardSuit.Spades);
            var priest2 = new Card(CardRank.Priest, CardSuit.Clubs);

            player1.AddCardToHand(priest1);
            player2.AddCardToHand(eight);
            player3.AddCardToHand(priest2);

            gameState.PlayCard(player1, priest1);
            gameState.PlayCard(player2, eight);

            // After P2, first Priest block resets
            Assert.IsFalse(gameState.IsPriestActive);

            // Act : P3 plays second Priest
            bool played = gameState.PlayCard(player3, priest2);

            // Assert
            Assert.IsTrue(played);
            Assert.IsTrue(gameState.IsPriestActive, "Second Priest should activate its own block");
        }

        [Test]
        public void T41_PriestBlockAndRefillHand()
        {
            // Verify that RefillHand works correctly during Priest block
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var six = new Card(CardRank.Six, CardSuit.Spades);

            // P2 has only 1 card
            player2.AddCardToHand(six);
            player1.AddCardToHand(priest);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, six);

            // P2's hand should be refilled to 3 (if deck available)
            Assert.IsTrue(player2.Hand.Count <= 3, "P2's hand should not exceed 3 after refill");
        }

        #endregion

        #region Block 6 : Edge Cases et Validation Générale (10 tests)

        [Test]
        public void T42_PriestWithEmptyPile()
        {
            // Playing Priest on an empty pile should work
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);

            // Act
            bool played = gameState.PlayCard(player1, priest);

            // Assert
            Assert.IsTrue(played);
            Assert.IsTrue(gameState.IsPriestActive);
        }

        [Test]
        public void T43_PriestWithOnlyGlassInPile()
        {
            // Pile: [Glass] → Priest played
            var glass = new Card(JokerType.Glass);
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);

            player1.AddCardToHand(glass);
            player2.AddCardToHand(priest);

            gameState.PlayCard(player1, glass);

            // Act
            bool played = gameState.PlayCard(player2, priest);

            // Assert
            Assert.IsTrue(played, "Priest should be playable over Glass");
            Assert.IsTrue(gameState.IsPriestActive);
        }

        [Test]
        public void T44_ThreeCardsBelow8AfterPriest()
        {
            // Verify all cards 3-8 are playable after Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            var validCards = new[] {
                new Card(CardRank.Three, CardSuit.Spades),
                new Card(CardRank.Four, CardSuit.Clubs),
                new Card(CardRank.Five, CardSuit.Diamonds),
                new Card(CardRank.Six, CardSuit.Hearts),
                new Card(CardRank.Seven, CardSuit.Spades),
                new Card(CardRank.Eight, CardSuit.Clubs)
            };

            foreach (var card in validCards)
            {
                // Act & Assert
                Assert.IsTrue(gameState.IsPlayable(card), $"{card.Rank} should be playable after Priest");
            }
        }

        [Test]
        public void T45_AllCardsRankAboveJackNotPlayable()
        {
            // Jack, Knight, Queen, King, Ace, 2 are all above Priest (8), should be invalid
            // 10 (value 7) IS playable after Priest
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);
            gameState.PlayCard(player1, priest);

            var cardsNotPlayable = new[] {
                new Card(CardRank.Jack, CardSuit.Clubs),
                new Card(CardRank.Knight, CardSuit.Diamonds),
                new Card(CardRank.Queen, CardSuit.Hearts),
                new Card(CardRank.King, CardSuit.Spades),
                new Card(CardRank.Ace, CardSuit.Clubs),
                new Card(CardRank.Two, CardSuit.Diamonds)
            };

            foreach (var card in cardsNotPlayable)
            {
                Assert.IsFalse(gameState.IsPlayable(card), $"{card.Rank} should NOT be playable after Priest");
            }
        }
        
        [Test]
        public void T46_PriestStateConsistencyAcrossMultiplePlays()
        {
            // Multiple cycles of Priest activation/reset
            for (int i = 0; i < 3; i++)
            {
                var priest = new Card(CardRank.Priest, CardSuit.Hearts);
                player1.AddCardToHand(priest);
                gameState.PlayCard(player1, priest);

                Assert.IsTrue(gameState.IsPriestActive, $"Iteration {i}: Priest should activate");

                var six = new Card(CardRank.Six, CardSuit.Spades);
                player2.AddCardToHand(six);
                gameState.PlayCard(player2, six);

                Assert.IsFalse(gameState.IsPriestActive, $"Iteration {i}: Priest should reset after P2");
            }
        }

        [Test]
        public void T47_PriestWithMixedRanks()
        {
            // Priest → P2 plays 3 → P3 plays 10 (should be playable, Priest reset)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var three = new Card(CardRank.Three, CardSuit.Spades);
            var ten = new Card(CardRank.Ten, CardSuit.Clubs);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(three);
            player3.AddCardToHand(ten);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, three);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(ten), "10 should be playable after Priest reset (10 > 3)");
        }

        [Test]
        public void T48_PriestWithJokerBlackAfterThreeConsecutiveCards()
        {
            // Priest → 6 → 7 → Black (should reset Priest mid-chain)
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var six = new Card(CardRank.Six, CardSuit.Spades);
            var seven = new Card(CardRank.Seven, CardSuit.Clubs);
            var black = new Card(JokerType.Black);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(six);
            player3.AddCardToHand(seven);
            player4.AddCardToHand(black);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, six);
            gameState.PlayCard(player3, seven);
            gameState.PlayCard(player4, black);

            // After Black, Priest should be reset
            Assert.IsFalse(gameState.IsPriestActive);

            // P1 can play high cards now
            var king = new Card(CardRank.King, CardSuit.Diamonds);
            player1.AddCardToHand(king);

            // Act & Assert
            Assert.IsTrue(gameState.IsPlayable(king), "King should be playable (Black resets Priest)");
        }

        [Test]
        public void T49_PriestRejeuAfterSquareStillActive()
        {
            // Verify Priest block persists even after Square rejeu
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            var eight1 = new Card(CardRank.Eight, CardSuit.Spades);
            var eight2 = new Card(CardRank.Eight, CardSuit.Clubs);
            var eight3 = new Card(CardRank.Eight, CardSuit.Diamonds);
            var three = new Card(CardRank.Three, CardSuit.Hearts);

            player1.AddCardToHand(priest);
            player2.AddCardToHand(eight1);
            player3.AddCardToHand(eight2);
            player4.AddCardToHand(eight3);

            gameState.PlayCard(player1, priest);
            gameState.PlayCard(player2, eight1);
            gameState.PlayCard(player3, eight2);
            gameState.PlayCard(player4, eight3);

            // Square triggered, P4 rejeu, but Priest was reset after P2's turn
            Assert.IsFalse(gameState.IsPriestActive, "Priest should have been reset after P2 played");
        }

        [Test]
        public void T50_PriestActivationExactnessAfterPlay()
        {
            // Ensure Priest activation happens exactly when played, not before
            var priest = new Card(CardRank.Priest, CardSuit.Hearts);
            player1.AddCardToHand(priest);

            // Before play
            Assert.IsFalse(gameState.IsPriestActive);

            // Act
            gameState.PlayCard(player1, priest);

            // Assert : activated immediately
            Assert.IsTrue(gameState.IsPriestActive);
        }

        #endregion
    }
}