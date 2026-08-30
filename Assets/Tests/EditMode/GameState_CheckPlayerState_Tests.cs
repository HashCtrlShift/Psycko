using NUnit.Framework;

namespace Psycko.Tests
{
    public class GameState_CheckPlayerState_Tests
    {
        private static readonly Card TestCard = default(Card);

        private static Player CreatePlayer(int hand = 0, int faceUp = 0, int faceDown = 0)
        {
            var player = new Player("player-1", "Player 1");
            for (int i = 0; i < hand; i++) player.Hand.Add(TestCard);
            for (int i = 0; i < faceUp; i++) player.FaceUp.Add(TestCard);
            for (int i = 0; i < faceDown; i++) player.FaceDown.Add(TestCard);
            return player;
        }

        private static GameState CreateState(GamePhase phase, int deckCount, Player player)
        {
            var deck = new Deck();
            while (deck.Count > deckCount) deck.Draw();
            return new GameState(new[] { player }, new Psycko.Core.Pile(), deck)
            {
                CurrentPhase = phase
            };
        }

        [Test]
        public void Travail_Hand2_Deck5_ReturnsNoChange()
        {
            var player = CreatePlayer(hand: 2);
            Assert.AreEqual(PhaseTransitionResult.NoChange, CreateState(GamePhase.Travail, 5, player).CheckPlayerState(player));
        }
        [Test]
        public void Travail_Hand0_Deck3_ReturnsNoChange()
        {
            var player = CreatePlayer();
            Assert.AreEqual(PhaseTransitionResult.NoChange, CreateState(GamePhase.Travail, 3, player).CheckPlayerState(player));
        }
        [Test]
        public void Travail_Hand0_Deck0_FaceUp3_TransitionsToTalent()
        {
            var player = CreatePlayer(faceUp: 3);
            Assert.AreEqual(PhaseTransitionResult.TransitionedToTalent, CreateState(GamePhase.Travail, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Travail_Hand0_Deck0_FaceDown3_TransitionsToChance()
        {
            var player = CreatePlayer(faceDown: 3);
            Assert.AreEqual(PhaseTransitionResult.TransitionedToChance, CreateState(GamePhase.Travail, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Travail_AllEmpty_ReturnsWon()
        {
            var player = CreatePlayer();
            Assert.AreEqual(PhaseTransitionResult.Won, CreateState(GamePhase.Travail, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Talent_Hand1_FaceUp0_Deck0_ReturnsNoChange()
        {
            var player = CreatePlayer(1);
            Assert.AreEqual(PhaseTransitionResult.NoChange, CreateState(GamePhase.Talent, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Talent_Hand0_FaceUp0_Deck0_FaceDown3_TransitionsToChance()
        {
            var player = CreatePlayer(faceDown: 3);
            Assert.AreEqual(PhaseTransitionResult.TransitionedToChance, CreateState(GamePhase.Talent, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Talent_AllEmpty_ReturnsWon()
        {
            var player = CreatePlayer();
            Assert.AreEqual(PhaseTransitionResult.Won, CreateState(GamePhase.Talent, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Chance_FaceDown1_ReturnsNoChange()
        {
            var player = CreatePlayer(faceDown: 1);
            Assert.AreEqual(PhaseTransitionResult.NoChange, CreateState(GamePhase.Chance, 0, player).CheckPlayerState(player));
        }
        [Test]
        public void Chance_AllEmpty_ReturnsWon()
        {
            var player = CreatePlayer();
            Assert.AreEqual(PhaseTransitionResult.Won, CreateState(GamePhase.Chance, 0, player).CheckPlayerState(player));
        }

        [Test]
        public void CheckPlayerState_DoesNotMutateCollections()
        {
            var player = CreatePlayer(hand: 2, faceUp: 3, faceDown: 4);
            var state = CreateState(GamePhase.Travail, 5, player);
            int hand = player.Hand.Count, faceUp = player.FaceUp.Count, faceDown = player.FaceDown.Count, deck = state.Deck.Count;

            state.CheckPlayerState(player);

            Assert.AreEqual(hand, player.Hand.Count);
            Assert.AreEqual(faceUp, player.FaceUp.Count);
            Assert.AreEqual(faceDown, player.FaceDown.Count);
            Assert.AreEqual(deck, state.Deck.Count);
        }
    }
}
