using System.Collections.Generic;
using Psycko.Core.Domain.Log;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Contrat d'enregistrement des actions d'une partie.
    /// Logging = side-effect pur : aucune logique de jeu ne dépend du recorder.
    /// </summary>
    public interface IGameLogRecorder
    {
        IReadOnlyList<GameLogEntry> Entries { get; }

        void Record(GameLogEntry entry);
    }
}