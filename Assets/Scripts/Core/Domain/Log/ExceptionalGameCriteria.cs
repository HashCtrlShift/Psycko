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

        /// <summary>Seuil bas Y : une partie avec moins de Y coups globaux est exceptionnelle.</summary>
        public int MinMovesThreshold { get; }

        /// <summary>Seuil haut X : une partie avec plus de X coups globaux est exceptionnelle.</summary>
        public int MaxMovesThreshold { get; }

        /// <summary>Plafond anti-boucle : partie interrompue à ce nombre de coups (toujours exceptionnelle).</summary>
        public int MoveCap { get; }

        /// <summary>Si vrai, toute partie en erreur (dont plafond atteint) est exceptionnelle.</summary>
        public bool IncludeErrors { get; }

        public ExceptionalGameCriteria(
            int topN = 10,
            int bottomN = 10,
            int minMovesThreshold = 250,
            int maxMovesThreshold = 6000,
            int moveCap = 250000,
            bool includeErrors = true)
        {
            if (topN < 0) throw new ArgumentOutOfRangeException(nameof(topN));
            if (bottomN < 0) throw new ArgumentOutOfRangeException(nameof(bottomN));
            if (minMovesThreshold < 0) throw new ArgumentOutOfRangeException(nameof(minMovesThreshold));
            if (maxMovesThreshold < 1) throw new ArgumentOutOfRangeException(nameof(maxMovesThreshold));
            if (moveCap < 1) throw new ArgumentOutOfRangeException(nameof(moveCap));
            if (minMovesThreshold >= maxMovesThreshold)
                throw new ArgumentException("Le seuil bas doit être inférieur au seuil haut.", nameof(minMovesThreshold));

            TopN = topN;
            BottomN = bottomN;
            MinMovesThreshold = minMovesThreshold;
            MaxMovesThreshold = maxMovesThreshold;
            MoveCap = moveCap;
            IncludeErrors = includeErrors;
        }
    }
}