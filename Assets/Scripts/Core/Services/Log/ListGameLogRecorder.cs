using System.Collections.Generic;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services.Log
{
    /// <summary>Recorder en mémoire : une instance par partie.</summary>
    public sealed class ListGameLogRecorder : IGameLogRecorder
    {
        private readonly List<GameLogEntry> _entries = new List<GameLogEntry>();
        public IReadOnlyList<GameLogEntry> Entries => _entries;
        public void Record(GameLogEntry entry) => _entries.Add(entry);
    }
}