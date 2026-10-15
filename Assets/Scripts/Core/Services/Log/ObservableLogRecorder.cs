#nullable enable

using System;
using System.Collections.Generic;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Décorateur : transmet chaque entrée au recorder interne (si présent)
    /// et à l'observateur. Permet l'affichage en direct même en mode Off.
    /// </summary>
    public sealed class ObservableLogRecorder : IGameLogRecorder
    {
        private readonly IGameLogRecorder? _inner;
        private readonly IGameObserver _observer;
        private readonly List<GameLogEntry> _fallback = new List<GameLogEntry>();

        public ObservableLogRecorder(IGameLogRecorder? inner, IGameObserver observer)
        {
            _inner = inner;
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        public IReadOnlyList<GameLogEntry> Entries =>
            _inner != null ? _inner.Entries : _fallback.AsReadOnly();

        public void Record(GameLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (_inner != null) _inner.Record(entry);
            else _fallback.Add(entry);
            _observer.OnLogEntry(entry);
        }
    }
}