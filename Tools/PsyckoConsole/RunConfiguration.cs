#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain.Log;

namespace Psycko.Console
{
    public enum RunMode
    {
        Sim,
        Play
    }

    public enum SeatKind
    {
        Human,
        Random,
        Discard
    }

    /// <summary>
    /// Configuration d'exécution unique, partagée par la ligne de commande et le menu interactif.
    /// </summary>
    public sealed class RunConfiguration
    {
        /// <summary>Nombre de parties par fichier CSV (valeur par défaut).</summary>
        public const int DefaultBatchSize = 50;

        /// <summary>Nombre maximal de parties exceptionnelles écrites en CSV (valeur par défaut).</summary>
        public const int DefaultLogCap = 200;

        public RunMode Mode { get; }
        public int GameCount { get; }
        public int StartSeed { get; }
        public LogMode LogMode { get; }
        public bool Reveal { get; }
        public bool DryRun { get; }
        public ExceptionalGameCriteria Criteria { get; }
        public int BatchSize { get; }
        public int LogCap { get; }
        public IReadOnlyList<SeatKind> Seats { get; }
        public IReadOnlyList<string> Names { get; }

        public RunConfiguration(
            RunMode mode,
            int gameCount,
            int startSeed,
            LogMode logMode,
            bool reveal,
            ExceptionalGameCriteria criteria,
            IEnumerable<SeatKind> seats,
            IEnumerable<string> names,
            bool dryRun = false,
            int batchSize = DefaultBatchSize,
            int logCap = DefaultLogCap)
        {
            if (gameCount < 1)
                throw new ArgumentOutOfRangeException(nameof(gameCount));

            var seatList = seats?.ToList() ?? throw new ArgumentNullException(nameof(seats));
            var nameList = names?.ToList() ?? throw new ArgumentNullException(nameof(names));

            if (seatList.Count != 4)
                throw new ArgumentException("Il faut exactement 4 sièges.");

            if (nameList.Count != 4
                || nameList.Any(string.IsNullOrWhiteSpace)
                || nameList.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                throw new ArgumentException("Les 4 noms doivent être non vides et distincts.");

            if (seatList.Count(x => x == SeatKind.Human) > 1)
                throw new ArgumentException("Un seul siège humain est autorisé.");

            if (batchSize < 1)
                throw new ArgumentException("La taille de lot doit être positive.");

            if (logCap < 1)
                throw new ArgumentException("Le plafond de logs doit être positif.");

            Mode = mode;
            GameCount = gameCount;
            StartSeed = startSeed;
            LogMode = logMode;
            Reveal = reveal;
            Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
            Seats = seatList.AsReadOnly();
            Names = nameList.AsReadOnly();
            DryRun = dryRun;
            BatchSize = batchSize;
            LogCap = logCap;
        }

        public static RunConfiguration DefaultSim() => new RunConfiguration(
            RunMode.Sim, 1000, 1, LogMode.ExceptionalOnly, false,
            new ExceptionalGameCriteria(),
            new[] { SeatKind.Random, SeatKind.Random, SeatKind.Random, SeatKind.Random },
            new[] { "Bot1", "Bot2", "Bot3", "Bot4" });

        public static RunConfiguration DefaultPlay() => new RunConfiguration(
            RunMode.Play, 1, Environment.TickCount, LogMode.All, false,
            new ExceptionalGameCriteria(),
            new[] { SeatKind.Human, SeatKind.Random, SeatKind.Random, SeatKind.Random },
            new[] { "Humain", "Bot1", "Bot2", "Bot3" });

        public override string ToString() =>
            $"Mode={Mode}; Games={GameCount}; Seed={StartSeed}; Log={LogMode}; Reveal={Reveal}; "
            + $"Short={Criteria.MinMovesThreshold}; Long={Criteria.MaxMovesThreshold}; "
            + $"MoveCap={Criteria.MoveCap}; Batch={BatchSize}; Cap={LogCap}; "
            + $"Seats={string.Join(",", Seats)}; Names={string.Join(",", Names)}";
    }
}