using System;
using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Bot de référence "simple" : choisit uniformément au hasard parmi les coups légaux.
    /// Sert de base de validation structurelle (simulations de masse) avant bots plus complexes.
    /// </summary>
    public class RandomBot : IPlayerAgent
    {
        private readonly Random _random;

        public RandomBot(int seed = -1)
        {
            _random = seed >= 0 ? new Random(seed) : new Random();
        }

        /// <summary>
        /// Choisit une carte aléatoire parmi les cartes légales.
        /// Retourne null (ramasse la pile) si aucune carte n'est jouable.
        /// </summary>
        public Card? ChooseCard(Player player, GameState gameState, IReadOnlyList<Card> legalCards)
        {
            if (legalCards == null || legalCards.Count == 0)
                return null; // aucun coup légal => ramasse

            int index = _random.Next(legalCards.Count);
            return legalCards[index];
        }

        /// <summary>
        /// Calcule les cartes actuellement jouables par le joueur (main uniquement),
        /// selon la règle de hauteur active de GameState. Utilitaire pour piloter ChooseCard.
        /// </summary>
        public static List<Card> GetLegalCards(Player player, GameState gameState)
        {
            var legal = new List<Card>();

            if (player == null || gameState == null)
                return legal;

            foreach (Card card in player.Hand)
            {
                if (gameState.IsPlayable(card))
                    legal.Add(card);
            }

            return legal;
        }
    }
}