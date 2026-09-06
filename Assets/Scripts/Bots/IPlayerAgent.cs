using System.Collections.Generic;
using Psycko;

namespace Psycko.Bots
{
    public interface IPlayerAgent
    {
        /// <summary>
        /// Le joueur choisit les cartes à jouer (1-4 de même rang) ou null/empty pour ramasser.
        /// </summary>
        List<Card> ChooseCards(Player player, GameState gameState);

        /// <summary>
        /// Le joueur choisit quelle carte donner lors d'un 7 (Don).
        /// Retourne null si pas de carte à donner.
        /// </summary>
        Card? ChooseGiftCard(Player giver, Player receiver);  
    }
}