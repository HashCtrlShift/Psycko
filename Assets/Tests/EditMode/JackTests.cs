using NUnit.Framework;
using System.Collections.Generic;
using Psycko;

namespace Psycko.Tests
{
    [TestFixture]
    public class JackTests
    {
        private GameState _gameState;
        private List<Player> _players;

        [SetUp]
        public void Setup()
        {
            _players = new List<Player>
            {
                new Player("p1", "Player1"),
                new Player("p2", "Player2"),
                new Player("p3", "Player3"),
                new Player("p4", "Player4")
            };
            _gameState = new GameState(_players);
        }

        #region Basic Jack Inversion

        /// <summary>
        /// Test basique : Valet inverse Clockwise → CounterClockwise
        /// </summary>
        [Test]
        public void Jack_InvertsDirection_ClockwiseToCounterClockwise()
        {
            // Arrange
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            Player player = _players[0];

            // Act
            _gameState.HandleJackPlayed(player);

            // Assert
            Assert.AreEqual(GameDirection.CounterClockwise, _gameState.TurnManager.CurrentTurn.Direction);
        }

        /// <summary>
        /// Test basique : Valet inverse CounterClockwise → Clockwise
        /// </summary>
        [Test]
        public void Jack_InvertsDirection_CounterClockwiseToClockwise()
        {
            // Arrange
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.CounterClockwise;
            Player player = _players[0];

            // Act
            _gameState.HandleJackPlayed(player);

            // Assert
            Assert.AreEqual(GameDirection.Clockwise, _gameState.TurnManager.CurrentTurn.Direction);
        }

        /// <summary>
        /// Test : Double inversion retrouve la direction initiale
        /// </summary>
        [Test]
        public void Jack_DoubleInversion_ReturnsToPreviousDirection()
        {
            // Arrange
            GameDirection originalDirection = GameDirection.Clockwise;
            _gameState.TurnManager.CurrentTurn.Direction = originalDirection;
            Player player = _players[0];

            // Act
            _gameState.HandleJackPlayed(player);
            GameDirection afterFirstInversion = _gameState.TurnManager.CurrentTurn.Direction;
            _gameState.HandleJackPlayed(player);

            // Assert
            Assert.AreEqual(originalDirection, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreNotEqual(originalDirection, afterFirstInversion);
        }

        #endregion

        #region Jack with AdvanceToNextPlayer

        /// <summary>
        /// Test : Valet → AdvanceToNextPlayer respecte la nouvelle direction (Clockwise)
        /// Direction AVANT le Valet : CounterClockwise
        /// Valet inverse → Clockwise
        /// AdvanceToNextPlayer de 0 en Clockwise → 1
        /// </summary>
        [Test]
        public void Jack_AdvanceToNextPlayer_Clockwise()
        {
            // Arrange : direction AVANT = CounterClockwise (sera inversée à Clockwise par le Valet)
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.CounterClockwise;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 0; // Player1

            // Act
            _gameState.HandleJackPlayed(_players[0]); // Inverse : CCW → CW
            _gameState.TurnManager.AdvanceToNextPlayer(); // Avance en CW : 0 → 1

            // Assert
            Assert.AreEqual(GameDirection.Clockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(1, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test : Valet → AdvanceToNextPlayer respecte la nouvelle direction (CounterClockwise)
        /// Direction AVANT le Valet : Clockwise
        /// Valet inverse → CounterClockwise
        /// AdvanceToNextPlayer de 2 en CounterClockwise → 1
        /// </summary>
        [Test]
        public void Jack_AdvanceToNextPlayer_CounterClockwise()
        {
            // Arrange : direction AVANT = Clockwise (sera inversée à CounterClockwise par le Valet)
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 2; // Player3

            // Act
            _gameState.HandleJackPlayed(_players[2]); // Inverse : CW → CCW
            _gameState.TurnManager.AdvanceToNextPlayer(); // Avance en CCW : 2 → 1

            // Assert
            Assert.AreEqual(GameDirection.CounterClockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(1, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Jack at 2 Players (Pass Effect)

        /// <summary>
        /// Test à 2 joueurs : Valet = inversion + avance = retour au même joueur = "passe"
        /// Direction AVANT : Clockwise → inversion → CounterClockwise
        /// De joueur 0, avance en CCW vers joueur 1
        /// Note : sémantiquement c'est un "passe" (inversion + avance au même joueur), 
        /// mais mécaniquement on a avancé à l'autre joueur en direction inversée
        /// </summary>
        [Test]
        public void Jack_TwoPlayers_Clockwise_ActsAsPass()
        {
            // Arrange
            var twoPlayers = new List<Player>
            {
                new Player("p1", "Player1"),
                new Player("p2", "Player2")
            };
            var gameStateTwoPlayers = new GameState(twoPlayers);
            gameStateTwoPlayers.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            gameStateTwoPlayers.TurnManager.CurrentTurn.CurrentPlayerIndex = 0;

            // Act
            gameStateTwoPlayers.HandleJackPlayed(twoPlayers[0]); // CW → CCW
            gameStateTwoPlayers.TurnManager.AdvanceToNextPlayer(); // 0 → 1 en CCW

            // Assert : direction inversée, joueur suivant (qui est le seul autre)
            Assert.AreEqual(GameDirection.CounterClockwise, gameStateTwoPlayers.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(1, gameStateTwoPlayers.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test à 2 joueurs : Valet depuis joueur 2, direction CounterClockwise
        /// Direction AVANT : CounterClockwise → inversion → Clockwise
        /// De joueur 1, avance en CW vers joueur 0
        /// </summary>
        [Test]
        public void Jack_TwoPlayers_CounterClockwise_ActsAsPass()
        {
            // Arrange
            var twoPlayers = new List<Player>
            {
                new Player("p1", "Player1"),
                new Player("p2", "Player2")
            };
            var gameStateTwoPlayers = new GameState(twoPlayers);
            gameStateTwoPlayers.TurnManager.CurrentTurn.Direction = GameDirection.CounterClockwise;
            gameStateTwoPlayers.TurnManager.CurrentTurn.CurrentPlayerIndex = 1;

            // Act
            gameStateTwoPlayers.HandleJackPlayed(twoPlayers[1]); // CCW → CW
            gameStateTwoPlayers.TurnManager.AdvanceToNextPlayer(); // 1 → 0 en CW

            // Assert : direction inversée, joueur suivant
            Assert.AreEqual(GameDirection.Clockwise, gameStateTwoPlayers.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(0, gameStateTwoPlayers.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Consecutive Jacks

        /// <summary>
        /// Test : Deux Valets consécutifs avec AdvanceToNextPlayer
        /// Direction initiale : Clockwise
        /// P1 (index 0) joue Valet → CW devient CCW → P4 (index 3)
        /// P4 (index 3) joue Valet → CCW devient CW → P0 (index 0)
        /// </summary>
        [Test]
        public void Jack_ConsecutiveJacks_FourPlayers()
        {
            // Arrange
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 0;

            // Act - P1 joue Valet
            _gameState.HandleJackPlayed(_players[0]); // CW → CCW
            _gameState.TurnManager.AdvanceToNextPlayer(); // 0 → 3 en CCW
            Assert.AreEqual(GameDirection.CounterClockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(3, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);

            // Act - P4 joue Valet
            _gameState.HandleJackPlayed(_players[3]); // CCW → CW
            _gameState.TurnManager.AdvanceToNextPlayer(); // 3 → 0 en CW

            // Assert (retour à Clockwise, retour à P1)
            Assert.AreEqual(GameDirection.Clockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(0, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test : Trois Valets consécutifs alternent les directions
        /// Valet 1 : CW → CCW
        /// Valet 2 : CCW → CW
        /// Valet 3 : CW → CCW
        /// Résultat final : CCW
        /// </summary>
        [Test]
        public void Jack_ThreeConsecutiveJacks_OddInversions()
        {
            // Arrange
            GameDirection initialDirection = GameDirection.Clockwise;
            _gameState.TurnManager.CurrentTurn.Direction = initialDirection;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 0;

            // Act - Valet 1
            _gameState.HandleJackPlayed(_players[0]);
            GameDirection afterFirst = _gameState.TurnManager.CurrentTurn.Direction;
            _gameState.TurnManager.AdvanceToNextPlayer();

            // Act - Valet 2
            _gameState.HandleJackPlayed(_players[3]);
            _gameState.TurnManager.AdvanceToNextPlayer();

            // Act - Valet 3
            _gameState.HandleJackPlayed(_players[0]);
            GameDirection afterThird = _gameState.TurnManager.CurrentTurn.Direction;

            // Assert : odd inversions = direction opposée à l'initiale
            Assert.AreEqual(GameDirection.CounterClockwise, afterFirst);
            Assert.AreEqual(GameDirection.CounterClockwise, afterThird);
            Assert.AreEqual(initialDirection, GameDirection.Clockwise);
        }

        #endregion

        #region Jack in Different Game Phases

        /// <summary>
        /// Test : Valet en Phase Travail (Le Travail)
        /// </summary>
        [Test]
        public void Jack_InPhase_Travail()
        {
            // Arrange
            _gameState.CurrentPhase = GamePhase.Travail;
            GameDirection directionBefore = _gameState.TurnManager.CurrentTurn.Direction;

            // Act
            _gameState.HandleJackPlayed(_players[0]);

            // Assert
            Assert.AreNotEqual(directionBefore, _gameState.TurnManager.CurrentTurn.Direction);
        }

        /// <summary>
        /// Test : Valet en Phase Talent (Le Talent)
        /// </summary>
        [Test]
        public void Jack_InPhase_Talent()
        {
            // Arrange
            _gameState.CurrentPhase = GamePhase.Talent;
            GameDirection directionBefore = _gameState.TurnManager.CurrentTurn.Direction;

            // Act
            _gameState.HandleJackPlayed(_players[0]);

            // Assert
            Assert.AreNotEqual(directionBefore, _gameState.TurnManager.CurrentTurn.Direction);
        }

        /// <summary>
        /// Test : Valet en Phase Chance (La Chance)
        /// </summary>
        [Test]
        public void Jack_InPhase_Chance()
        {
            // Arrange
            _gameState.CurrentPhase = GamePhase.Chance;
            GameDirection directionBefore = _gameState.TurnManager.CurrentTurn.Direction;

            // Act
            _gameState.HandleJackPlayed(_players[0]);

            // Assert
            Assert.AreNotEqual(directionBefore, _gameState.TurnManager.CurrentTurn.Direction);
        }

        #endregion

        #region Jack with Edge Cases

        /// <summary>
        /// Test : Valet depuis dernier joueur, direction Clockwise
        /// Direction AVANT : Clockwise → inversion → CounterClockwise
        /// De joueur 3, avance en CCW vers joueur 2
        /// </summary>
        [Test]
        public void Jack_LastPlayer_ClockwiseWrapAround()
        {
            // Arrange
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 3; // Player4

            // Act
            _gameState.HandleJackPlayed(_players[3]); // CW → CCW
            _gameState.TurnManager.AdvanceToNextPlayer(); // 3 → 2 en CCW

            // Assert
            Assert.AreEqual(GameDirection.CounterClockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(2, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test : Valet depuis premier joueur, direction CounterClockwise
        /// Direction AVANT : CounterClockwise → inversion → Clockwise
        /// De joueur 0, avance en CW vers joueur 1
        /// </summary>
        [Test]
        public void Jack_FirstPlayer_CounterClockwiseWrapAround()
        {
            // Arrange
            _gameState.TurnManager.CurrentTurn.Direction = GameDirection.CounterClockwise;
            _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex = 0; // Player1

            // Act
            _gameState.HandleJackPlayed(_players[0]); // CCW → CW
            _gameState.TurnManager.AdvanceToNextPlayer(); // 0 → 1 en CW

            // Assert
            Assert.AreEqual(GameDirection.Clockwise, _gameState.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(1, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test : Valet n'affecte pas le joueur actuel (reste le même jusqu'à AdvanceToNextPlayer)
        /// </summary>
        [Test]
        public void Jack_DoesNotChangeCurrentPlayer()
        {
            // Arrange
            int originalPlayerIndex = _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex;

            // Act
            _gameState.HandleJackPlayed(_players[originalPlayerIndex]);

            // Assert (aucun changement de joueur immédiatement)
            Assert.AreEqual(originalPlayerIndex, _gameState.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Jack with Card Height Validation

        /// <summary>
        /// Test : Valet a une hauteur valide (CardRank.Jack = 9)
        /// </summary>
        [Test]
        public void Jack_HasValidCardRank()
        {
            // Arrange
            Card jackCard = new Card(CardRank.Jack, CardSuit.Hearts);

            // Assert
            Assert.AreEqual(CardRank.Jack, jackCard.Rank);
            Assert.AreEqual(9, (int)jackCard.Rank);
            Assert.IsFalse(jackCard.IsJoker);
        }

        /// <summary>
        /// Test : Valet peut être joué après 10 (Valet > 10)
        /// </summary>
        [Test]
        public void Jack_CanBePlayedAfterTen()
        {
            // Arrange
            Card tenCard = new Card(CardRank.Ten, CardSuit.Hearts);
            Card jackCard = new Card(CardRank.Jack, CardSuit.Clubs);

            // Assert (Jack = 9, Ten = 7, donc 9 > 7)
            Assert.Greater((int)jackCard.Rank, (int)tenCard.Rank);
        }

        /// <summary>
        /// Test : Valet peut être joué avant Dame (Valet < Dame)
        /// </summary>
        [Test]
        public void Jack_CanBePlayedBeforeQueen()
        {
            // Arrange
            Card jackCard = new Card(CardRank.Jack, CardSuit.Hearts);
            Card queenCard = new Card(CardRank.Queen, CardSuit.Clubs);

            // Assert (Jack = 9, Queen = 11, donc 9 < 11)
            Assert.Less((int)jackCard.Rank, (int)queenCard.Rank);
        }

        #endregion

        #region Jack Direction Persistence

        /// <summary>
        /// Test : Direction persiste après plusieurs tours sans Valet
        /// </summary>
        [Test]
        public void Jack_DirectionPersists_AfterNormalCards()
        {
            // Arrange
            _gameState.HandleJackPlayed(_players[0]);
            GameDirection directionAfterJack = _gameState.TurnManager.CurrentTurn.Direction;

            // Act (simuler avance sans Valet)
            _gameState.TurnManager.AdvanceToNextPlayer();
            _gameState.TurnManager.AdvanceToNextPlayer();

            // Assert
            Assert.AreEqual(directionAfterJack, _gameState.TurnManager.CurrentTurn.Direction);
        }

        /// <summary>
        /// Test : Valet suivi d'une carte standard, puis Valet à nouveau
        /// </summary>
        [Test]
        public void Jack_WithNormalCardInBetween()
        {
            // Arrange
            GameDirection initial = _gameState.TurnManager.CurrentTurn.Direction;

            // Act - Valet 1
            _gameState.HandleJackPlayed(_players[0]);
            GameDirection afterFirst = _gameState.TurnManager.CurrentTurn.Direction;
            _gameState.TurnManager.AdvanceToNextPlayer();

            _gameState.TurnManager.AdvanceToNextPlayer(); // Normal card played by another player

            // Act - Valet 2 (depuis P2)
            _gameState.HandleJackPlayed(_players[2]);
            GameDirection afterSecond = _gameState.TurnManager.CurrentTurn.Direction;

            // Assert
            Assert.AreNotEqual(initial, afterFirst);
            Assert.AreEqual(initial, afterSecond);
        }

        #endregion

        #region Jack with 3 Players

        /// <summary>
        /// Test : Valet à 3 joueurs, direction Clockwise
        /// Direction AVANT : Clockwise → inversion → CounterClockwise
        /// De joueur 0, avance en CCW vers joueur 2 (wrap-around)
        /// </summary>
        [Test]
        public void Jack_ThreePlayers_Clockwise()
        {
            // Arrange
            var threePlayers = new List<Player>
            {
                new Player("p1", "Player1"),
                new Player("p2", "Player2"),
                new Player("p3", "Player3")
            };
            var gameStateThree = new GameState(threePlayers);
            gameStateThree.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;
            gameStateThree.TurnManager.CurrentTurn.CurrentPlayerIndex = 0;

            // Act
            gameStateThree.HandleJackPlayed(threePlayers[0]); // CW → CCW
            gameStateThree.TurnManager.AdvanceToNextPlayer(); // 0 → 2 en CCW

            // Assert
            Assert.AreEqual(GameDirection.CounterClockwise, gameStateThree.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(2, gameStateThree.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        /// <summary>
        /// Test : Valet à 3 joueurs, direction CounterClockwise
        /// Direction AVANT : CounterClockwise → inversion → Clockwise
        /// De joueur 2, avance en CW vers joueur 0 (wrap-around)
        /// </summary>
        [Test]
        public void Jack_ThreePlayers_CounterClockwise()
        {
            // Arrange
            var threePlayers = new List<Player>
            {
                new Player("p1", "Player1"),
                new Player("p2", "Player2"),
                new Player("p3", "Player3")
            };
            var gameStateThree = new GameState(threePlayers);
            gameStateThree.TurnManager.CurrentTurn.Direction = GameDirection.CounterClockwise;
            gameStateThree.TurnManager.CurrentTurn.CurrentPlayerIndex = 2;

            // Act
            gameStateThree.HandleJackPlayed(threePlayers[2]); // CCW → CW
            gameStateThree.TurnManager.AdvanceToNextPlayer(); // 2 → 0 en CW

            // Assert
            Assert.AreEqual(GameDirection.Clockwise, gameStateThree.TurnManager.CurrentTurn.Direction);
            Assert.AreEqual(0, gameStateThree.TurnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Jack Strength

        /// <summary>
        /// Test : Valet a le rang CardRank.Jack = 9
        /// </summary>
        [Test]
        public void Jack_RankIsCorrect()
        {
            // Arrange
            Card jack = new Card(CardRank.Jack, CardSuit.Hearts);

            // Assert
            Assert.AreEqual(CardRank.Jack, jack.Rank);
            Assert.AreEqual(9, (int)jack.Rank);
        }

        /// <summary>
        /// Test : Valet n'est pas un Joker
        /// </summary>
        [Test]
        public void Jack_IsNotAJoker()
        {
            // Arrange
            Card jack = new Card(CardRank.Jack, CardSuit.Hearts);

            // Assert
            Assert.IsFalse(jack.IsJoker);
        }

        /// <summary>
        /// Test : Tous les Valets (4 couleurs) inversent la direction
        /// </summary>
        [Test]
        public void Jack_AllSuitsInvertDirection()
        {
            // Arrange
            var suits = new[] { CardSuit.Spades, CardSuit.Hearts, CardSuit.Clubs, CardSuit.Diamonds };
            foreach (var suit in suits)
            {
                // Reset : direction Clockwise AVANT inversion
                _gameState.TurnManager.CurrentTurn.Direction = GameDirection.Clockwise;

                Card jack = new Card(CardRank.Jack, suit);

                // Act
                _gameState.HandleJackPlayed(_players[0]);

                // Assert : doit devenir CounterClockwise après inversion
                Assert.AreEqual(GameDirection.CounterClockwise, _gameState.TurnManager.CurrentTurn.Direction);
            }
        }

        #endregion
    }
}