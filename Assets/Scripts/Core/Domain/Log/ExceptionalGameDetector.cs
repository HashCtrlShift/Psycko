using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain.Log
{
    public static class ExceptionalGameDetector
    {
        /// <summary>
        /// Nombre d'entrées d'action d'un log (l'entrée GameEnd n'est pas un coup).
        /// Information de lecture du log ; les seuils X / Y portent sur GameResult.TotalMoves.
        /// </summary>
        public static int CountTurns(GameLog gameLog)
        {
            if (gameLog == null) throw new ArgumentNullException(nameof(gameLog));
            return gameLog.Entries.Count(e => e.Kind != ActionKind.GameEnd);
        }

        /// <summary>
        /// Critères individuels, évaluables au fil de l'eau :
        /// partie en erreur (dont plafond atteint), moins de Y coups ou plus de X coups globaux.
        /// </summary>
        public static bool IsExceptional(GameResult result, ExceptionalGameCriteria criteria)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));

            if (result.IsError) return criteria.IncludeErrors;
            return result.TotalMoves < criteria.MinMovesThreshold
                || result.TotalMoves > criteria.MaxMovesThreshold;
        }

        /// <summary>
        /// Critère collectif : retourne les indices des BottomN parties les plus courtes
        /// et des TopN plus longues, à appeler une fois toutes les durées connues.
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