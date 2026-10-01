using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Unique source de vérité pour le calcul de fin de partie.
    /// Règle : la partie est terminée lorsqu'il reste au plus un joueur
    /// dont la phase n'est pas Finished. Méthode pure, aucune mutation,
    /// zéro dépendance Unity.
    /// </summary>
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
    }
}