using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain.Log;

namespace Psycko.Core.Services.Log
{
    public static class ExceptionalGameDetector
    {
        /// <summary>
        /// Nombre de coups d'une partie = nombre d'entrées d'action
        /// (l'entrée GameEnded n'est pas un coup).
        /// </summary>
        public static int CountTurns(GameLog gameLog)
        {
            if (gameLog == null) throw new ArgumentNullException(nameof(gameLog));
            return gameLog.Entries.Count(e => !e.IsGameEnd);
        }

        /// <summary>
        /// Critères individuels, évaluables au fil de l'eau :
        /// partie en erreur ou au-delà du seuil de coups.
        /// </summary>
        public static bool IsExceptional(
            GameLog gameLog,
            ExceptionalGameCriteria criteria,
            bool hadError = false)
        {
            if (gameLog == null) throw new ArgumentNullException(nameof(gameLog));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));

            if (hadError && criteria.IncludeErrors) return true;
            return CountTurns(gameLog) > criteria.MaxTurnThreshold;
        }

        /// <summary>
        /// Critère collectif : retourne les indices des N parties les plus longues
        /// et des N plus courtes, à appeler une fois toutes les durées connues.
        /// </summary>
        public static IReadOnlyCollection<int> SelectExtremeDurations(
            IReadOnlyList<int> turnCounts,
            ExceptionalGameCriteria criteria)
        {
            if (turnCounts == null) throw new ArgumentNullException(nameof(turnCounts));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));

            var ordered = Enumerable.Range(0, turnCounts.Count)
                .OrderBy(i => turnCounts[i])
                .ThenBy(i => i)
                .ToList();

            var selected = new HashSet<int>();
            foreach (var i in ordered.Take(criteria.BottomN)) selected.Add(i);
            foreach (var i in ordered.Skip(Math.Max(0, ordered.Count - criteria.TopN))) selected.Add(i);
            return selected;
        }
    }
}