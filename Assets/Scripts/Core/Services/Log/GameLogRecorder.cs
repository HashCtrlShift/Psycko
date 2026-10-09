using System.Collections.Generic;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Implémentation simple du recorder : accumule les entrées de log dans une liste.
    /// Une instance par partie ; son cycle de vie est lié à GameOrchestrator et à la
    /// boucle de simulation (Console).
    /// </summary>
    public sealed class GameLogRecorder : IGameLogRecorder
    {
        private readonly List<GameLogEntry> _entries = new List<GameLogEntry>();

        public IReadOnlyList<GameLogEntry> Entries => _entries.AsReadOnly();

        public void Record(GameLogEntry entry)
        {
            if (entry == null)
                throw new System.ArgumentNullException(nameof(entry));
            _entries.Add(entry);
        }
    }
}