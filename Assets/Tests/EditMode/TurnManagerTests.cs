using NUnit.Framework;
using System.Collections.Generic;
using Psycko;

namespace Psycko.Tests.EditMode
{
    public class TurnManagerTests
    {
        private List<Player> players;
        private TurnManager turnManager;

        [SetUp]
        public void Setup()
        {
            players = new List<Player>
            {
                new Player("P1", "Alice"),
                new Player("P2", "Bob"),
                new Player("P3", "Charlie"),
                new Player("P4", "Dave")
            };
            turnManager = new TurnManager(players, players[0]);
        }

        #region Initialization Tests

        [Test]
        public void Constructor_WithValidPlayers_InitializesProperly()
        {
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(0, turnManager.CurrentTurn.CurrentPlayerIndex);
            Assert.AreEqual(GameDirection.Clockwise, turnManager.CurrentTurn.Direction);
        }

        [Test]
        public void Constructor_WithStartingPlayer_InitializesCorrectly()
        {
            var tm = new TurnManager(players, players[2]);
            Assert.AreEqual("Charlie", tm.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(2, tm.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void Constructor_WithNullPlayersList_ThrowsException()
        {
            Assert.Throws<System.ArgumentException>(() => new TurnManager(null, null));
        }

        [Test]
        public void Constructor_WithEmptyPlayersList_ThrowsException()
        {
            Assert.Throws<System.ArgumentException>(() => new TurnManager(new List<Player>(), null));
        }

        [Test]
        public void Constructor_WithStartingPlayerNotInList_ThrowsException()
        {
            var otherPlayer = new Player("P99", "Unknown");
            Assert.Throws<System.ArgumentException>(() => new TurnManager(players, otherPlayer));
        }

        #endregion

        #region Direction Tests

        [Test]
        public void ReverseDirection_FromClockwiseToCounterClockwise_Reverses()
        {
            Assert.AreEqual(GameDirection.Clockwise, turnManager.CurrentTurn.Direction);
            turnManager.ReverseDirection();
            Assert.AreEqual(GameDirection.CounterClockwise, turnManager.CurrentTurn.Direction);
        }

        [Test]
        public void ReverseDirection_FromCounterClockwiseToClockwise_Reverses()
        {
            turnManager.ReverseDirection();
            turnManager.ReverseDirection();
            Assert.AreEqual(GameDirection.Clockwise, turnManager.CurrentTurn.Direction);
        }

        [Test]
        public void ReverseDirection_DoesNotChangeCurrentPlayer()
        {
            var originalPlayer = turnManager.CurrentTurn.CurrentPlayer;
            turnManager.ReverseDirection();
            Assert.AreEqual(originalPlayer, turnManager.CurrentTurn.CurrentPlayer);
        }

        #endregion

        #region Turn Advancement Tests

        [Test]
        public void AdvanceToNextPlayer_Clockwise_MovesForward()
        {
            // Alice (0) -> Bob (1)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(1, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void AdvanceToNextPlayer_ClockwiseWrapsAround_ReturnsToFirst()
        {
            // Dave (3) -> Alice (0)
            turnManager = new TurnManager(players, players[3]);
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(0, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void AdvanceToNextPlayer_CounterClockwise_MovesBackward()
        {
            turnManager = new TurnManager(players, players[2]);
            turnManager.ReverseDirection();
            // Charlie (2) -> Bob (1)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(1, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void AdvanceToNextPlayer_CounterClockwiseWrapsAround_ReturnsToLast()
        {
            turnManager = new TurnManager(players, players[0]);
            turnManager.ReverseDirection();
            // Alice (0) -> Dave (3)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(3, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Skip Won Players Tests

        [Test]
        public void AdvanceToNextPlayer_SkipsPlayersThatHaveWon_Clockwise()
        {
            players[1].HasWon = true; // Bob a gagné
            // Alice (0) -> Bob (1, won) -> Charlie (2)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Charlie", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(2, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void AdvanceToNextPlayer_SkipsMultipleWonPlayers_Clockwise()
        {
            players[1].HasWon = true; // Bob
            players[2].HasWon = true; // Charlie
            // Alice (0) -> Bob (won) -> Charlie (won) -> Dave (3)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(3, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void AdvanceToNextPlayer_SkipsPlayersThatHaveWon_CounterClockwise()
        {
            turnManager = new TurnManager(players, players[3]);
            turnManager.ReverseDirection();
            players[2].HasWon = true; // Charlie a gagné
            // Dave (3) -> Charlie (2, won) -> Bob (1)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(1, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        #endregion

        #region Card Effect Handler Tests

        [Test]
        public void HandleTwoPlayed_SamePlayerStaysActive()
        {
            var originalPlayer = turnManager.CurrentTurn.CurrentPlayer;
            turnManager.HandleTwoPlayed();
            Assert.AreEqual(originalPlayer, turnManager.CurrentTurn.CurrentPlayer);
            Assert.AreEqual(0, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void HandleBombPlayed_AdvancesToNextPlayer()
        {
            turnManager.HandleBombPlayed();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(1, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void HandlePlayerPickedUp_AdvancesToNextPlayer()
        {
            turnManager.HandlePlayerPickedUp();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void HandlePlayerPassed_AdvancesToNextPlayer()
        {
            turnManager.HandlePlayerPassed();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void HandlePlayerTransitionedOrWon_AdvancesToNextPlayer()
        {
            turnManager.HandlePlayerTransitionedOrWon(players[0]);
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        #endregion

        #region Game Over Tests

        [Test]
        public void GetActivePlayerCount_AllPlayersActive_Returns4()
        {
            Assert.AreEqual(4, turnManager.GetActivePlayerCount());
        }

        [Test]
        public void GetActivePlayerCount_OnePlayerWon_Returns3()
        {
            players[0].HasWon = true;
            Assert.AreEqual(3, turnManager.GetActivePlayerCount());
        }

        [Test]
        public void GetActivePlayerCount_ThreePlayersWon_Returns1()
        {
            players[0].HasWon = true;
            players[1].HasWon = true;
            players[2].HasWon = true;
            Assert.AreEqual(1, turnManager.GetActivePlayerCount());
        }

        [Test]
        public void IsGameOver_MultiplePlayersActive_ReturnsFalse()
        {
            Assert.IsFalse(turnManager.IsGameOver());
        }

        [Test]
        public void IsGameOver_OnlyOnePlayerActive_ReturnsTrue()
        {
            players[0].HasWon = true;
            players[1].HasWon = true;
            players[2].HasWon = true;
            Assert.IsTrue(turnManager.IsGameOver());
        }

        [Test]
        public void GetLoser_GameNotOver_ReturnsNull()
        {
            Assert.IsNull(turnManager.GetLoser());
        }

        [Test]
        public void GetLoser_GameOver_ReturnLastActivePlayer()
        {
            players[0].HasWon = true;
            players[1].HasWon = true;
            players[2].HasWon = true;
            var loser = turnManager.GetLoser();
            Assert.AreEqual("Dave", loser.Name);
            Assert.IsFalse(loser.HasWon);
        }

        #endregion

        #region Scenario Tests

        [Test]
        public void Scenario_StandardTurnFlow_AdvancesCorrectly()
        {
            // Alice joue -> Bob ouvre
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Bob joue -> Charlie ouvre
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Charlie", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Charlie joue -> Dave ouvre
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Dave joue -> Alice ouvre (wrap)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void Scenario_TwoPlayedThenBomb_CorrectFlow()
        {
            // Alice joue 2 (rejeu Alice)
            turnManager.HandleTwoPlayed();
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Alice joue Bombe -> Bob ouvre
            turnManager.HandleBombPlayed();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void Scenario_JackPlayedThenBomb_DirectionReversedThenAdvanced()
        {
            // Alice joue Valet (inverse sens)
            turnManager.ReverseDirection();
            Assert.AreEqual(GameDirection.CounterClockwise, turnManager.CurrentTurn.Direction);
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Alice joue Bombe -> Dave ouvre (en arrière)
            turnManager.HandleBombPlayed();
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);
            Assert.AreEqual(3, turnManager.CurrentTurn.CurrentPlayerIndex);
        }

        [Test]
        public void Scenario_PlayerPicksUpThenAdvances()
        {
            // Alice ramasse -> Bob ouvre
            turnManager.HandlePlayerPickedUp();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Bob ramasse -> Charlie ouvre
            turnManager.HandlePlayerPickedUp();
            Assert.AreEqual("Charlie", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void Scenario_PlayerWinsAndLeavesGame()
        {
            // Alice gagne (phase transition ou partie)
            players[0].HasWon = true;
            turnManager.HandlePlayerTransitionedOrWon(players[0]);

            // Bob ouvre (Alice est skippée)
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Bob -> Charlie -> Dave -> Bob (Alice skipped)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Charlie", turnManager.CurrentTurn.CurrentPlayer.Name);
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);
        }

        [Test]
        public void Scenario_ComplexGameFlow()
        {
            // Alice -> Bob (standard)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Bob joue 2 (rejeu)
            turnManager.HandleTwoPlayed();
            Assert.AreEqual("Bob", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Bob joue Valet (inverse sens)
            turnManager.ReverseDirection();
            Assert.AreEqual(GameDirection.CounterClockwise, turnManager.CurrentTurn.Direction);

            // Bob -> Alice (arrière, Bob a joué)
            turnManager.AdvanceToNextPlayer();
            Assert.AreEqual("Alice", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Alice gagne
            players[0].HasWon = true;
            turnManager.HandlePlayerTransitionedOrWon(players[0]);

            // Dave ouvre (direction inverse, Alice skipped)
            Assert.AreEqual("Dave", turnManager.CurrentTurn.CurrentPlayer.Name);

            // Game continue...
            Assert.IsFalse(turnManager.IsGameOver());
            Assert.AreEqual(3, turnManager.GetActivePlayerCount());
        }

        #endregion
    }
}