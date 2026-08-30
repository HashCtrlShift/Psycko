using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    public class GameState
    {
        public GamePhase CurrentPhase { get; set; }
        public List<Player> Players { get; }
        public Pile Pile { get; }
        public Deck Deck { get; }

        public GameState(
            IEnumerable<Player> players = null,
            Pile pile = null,
            Deck deck = null)
        {
            CurrentPhase = GamePhase.Travail;
            Players = players == null ? new List<Player>() : new List<Player>(players);
            Pile = pile ?? new Pile();
            Deck = deck ?? new Deck();
        }

        /// <summary>
        /// Détecte l'état du joueur sans modifier le joueur ni l'état de la partie.
        /// </summary>
        public PhaseTransitionResult CheckPlayerState(Player player)
        {
            int handCount = player.Hand.Count;
            int faceUpCount = player.FaceUp.Count;
            int faceDownCount = player.FaceDown.Count;
            bool deckIsEmpty = Deck.Count == 0;

            if (handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount == 0)
                return PhaseTransitionResult.Won;

            if (CurrentPhase != GamePhase.Chance &&
                handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount > 0)
                return PhaseTransitionResult.TransitionedToChance;

            if (CurrentPhase == GamePhase.Travail &&
                handCount == 0 && deckIsEmpty && faceUpCount > 0)
                return PhaseTransitionResult.TransitionedToTalent;

            return PhaseTransitionResult.NoChange;
        }
    }
}
