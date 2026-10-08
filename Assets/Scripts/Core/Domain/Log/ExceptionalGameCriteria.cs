using System;

namespace Psycko.Core.Domain.Log
{
    /// <summary>Critères combinables de sélection des parties exceptionnelles.</summary>
    public sealed class ExceptionalGameCriteria
    {
        /// <summary>Nombre de parties les plus longues à isoler.</summary>
        public int TopN { get; }

        /// <summary>Nombre de parties les plus courtes à isoler.</summary>
        public int BottomN { get; }

        /// <summary>Une partie dépassant ce nombre de coups est exceptionnelle.</summary>
        public int MaxTurnThreshold { get; }

        /// <summary>Si vrai, toute partie en erreur est exceptionnelle.</summary>
        public bool IncludeErrors { get; }

        public ExceptionalGameCriteria(
            int topN = 10,
            int bottomN = 10,
            int maxTurnThreshold = 200,
            bool includeErrors = true)
        {
            if (topN < 0) throw new ArgumentOutOfRangeException(nameof(topN));
            if (bottomN < 0) throw new ArgumentOutOfRangeException(nameof(bottomN));
            if (maxTurnThreshold < 1) throw new ArgumentOutOfRangeException(nameof(maxTurnThreshold));

            TopN = topN;
            BottomN = bottomN;
            MaxTurnThreshold = maxTurnThreshold;
            IncludeErrors = includeErrors;
        }
    }
}