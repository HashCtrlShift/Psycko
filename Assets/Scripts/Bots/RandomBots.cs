using System;
using System.Collections.Generic;
using System.Linq;
using Psycko;

namespace Psycko.Bots
{
    public class RandomBot : IPlayerAgent
    {
        private Random _random;

        public RandomBot(Random random = null)
        {
            _random = random ?? new Random();
        }

        public List<Card> ChooseCards(Player player, GameState gameState)
        {
            // Stratégie basique : 70% de chance de jouer, 30% de ramasser
            if (_random.Next(100) < 30)
            {
                return null; // Ramasser
            }

            // Essayer de jouer une carte valide
            List<Card> playableCards = player.Hand
                .Where(c => gameState.IsPlayable(c))
                .ToList();

            if (playableCards.Count == 0)
            {
                return null; // Rien à jouer, ramasser
            }

            // Choisir une carte aléatoire
            Card selectedCard = playableCards[_random.Next(playableCards.Count)];

            // Grouper les cartes du même rang dans la main
            List<Card> sameRankInHand = player.Hand
                .Where(c => !c.IsJoker && c.Rank == selectedCard.Rank)
                .ToList();

            // Décider aléatoirement combien de cartes jouer (1-4)
            int maxCards = Math.Min(4, sameRankInHand.Count);
            int cardCount = _random.Next(1, maxCards + 1);

            // Sélectionner les N premières cartes du même rang
            List<Card> cardsToPlay = sameRankInHand.Take(cardCount).ToList();

            return cardsToPlay.Count > 0 ? cardsToPlay : null;
        }

        public Card? ChooseGiftCard(Player giver, Player receiver)
        {
            // Choisir une carte aléatoire dans la main du bot
            if (giver.Hand.Count == 0)
                return null;

            return giver.Hand[_random.Next(giver.Hand.Count)];
        }
    }
}