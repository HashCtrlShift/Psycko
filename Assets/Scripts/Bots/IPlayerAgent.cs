using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Interface commune à tout agent capable de décider d'un coup à jouer.
    /// Implémentée par RandomBot pour l'instant ; futurs bots plus complexes
    /// (heuristiques, ML) implémenteront la même interface.
    /// </summary>
    public interface IPlayerAgent
    {
        /// <summary>
        /// Choisit une carte à jouer parmi les cartes légales fournies.
        /// Retourne null si l'agent décide de ramasser la pile (aucun coup jouable
        /// ou choix délibéré de ramasser).
        /// </summary>
        Card? ChooseCard(Player player, GameState gameState, IReadOnlyList<Card> legalCards);
    }
}