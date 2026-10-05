using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services
{
    public static class GameResultCalculator
    {
        public static bool IsGameOver(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            int stillPlaying = 0;

            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.Players[i].CurrentPhase != DefPhase.Finished)
                    stillPlaying++;
            }

            return stillPlaying <= 1;
        }

        /// <summary>
        /// Retourne l'Id du Psycko (dernier joueur non Finished) si la partie est
        /// terminée ; null sinon. Méthode pure.
        /// </summary>
        public static int? GetPsyckoPlayerId(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!IsGameOver(state))
                return null;

            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.Players[i].CurrentPhase != DefPhase.Finished)
                    return state.Players[i].Id;
            }

            return null;
        }
    }
}