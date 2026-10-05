using System;
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Désigne le premier joueur : celui qui détient la plus petite carte en main.
    /// Ordre : rang croissant (3 … As, 2), puis couleur Trèfle < Carreau < Cœur < Pique.
    /// Jokers exclus. Seule la couche Hand compte. Chaque carte étant unique,
    /// il n'y a jamais d'égalité. Ne force pas la carte à être jouée.
    /// </summary>
    public static class FirstPlayerResolver
    {
        /// <returns>Index de siège (dans players) du premier joueur.</returns>
        public static int Resolve(IReadOnlyList<Player> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));

            var bestSeat = -1;
            var bestKey = int.MaxValue;

            for (var seat = 0; seat < players.Count; seat++)
            {
                foreach (var card in players[seat].Hand)
                {
                    if (card.IsJoker) continue;

                    // Clé = rang * 4 + couleur (enums déjà dans le bon ordre).
                    var key = (int)card.Rank.Value * 4 + (int)card.Suit.Value;
                    if (key < bestKey)
                    {
                        bestKey = key;
                        bestSeat = seat;
                    }
                }
            }

            if (bestSeat < 0)
                throw new InvalidOperationException(
                    "Aucune carte standard en main : impossible de désigner le premier joueur.");

            return bestSeat;
        }
    }
}