#nullable enable

using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Écoute passive d'une partie en direct (Console, futur Présentation).
    /// Side-effect pur : aucune logique de jeu ne dépend de l'observateur.
    /// </summary>
    public interface IGameObserver
    {
        /// <summary>Appelé à chaque entrée de log, dans l'ordre.</summary>
        void OnLogEntry(GameLogEntry entry);

        /// <summary>Appelé après chaque tour avec l'état réel de la partie.</summary>
        void OnStateChanged(GameState state);
    }
}