using System;

namespace Psycko.Presentation.Models
{
    /// <summary>Résultat du calcul de disposition d'une main.</summary>
    public struct HandLayoutResult
    {
        public readonly int VisibleCount;
        public readonly float Spacing;
        public readonly bool Truncated;

        public HandLayoutResult(int visibleCount, float spacing, bool truncated)
        {
            VisibleCount = visibleCount;
            Spacing = spacing;
            Truncated = truncated;
        }
    }

    /// <summary>Calcul pur C# sans Unity : géométrie uniquement.</summary>
    public static class HandSpacing
    {
        public const float DefaultMinSpacing = 12f;

        /// <summary>Calcule l'espacement et le nombre d'éléments visibles, en appliquant d'abord le plafond maxVisible, puis la capacité de la zone.</summary>
        /// <param name="count">Nombre réel d'éléments.</param>
        /// <param name="defaultSpacing">Espacement souhaité.</param>
        /// <param name="zoneLength">Longueur disponible dans la zone.</param>
        /// <param name="cardLength">Longueur d'une carte.</param>
        /// <param name="minSpacing">Espacement minimal.</param>
        /// <param name="maxVisible">Plafond du nombre d'éléments visibles avant prise en compte de la capacité de la zone.</param>
        /// <returns>Disposition calculée ; le compte réel reste indépendant de ce résultat.</returns>
        public static HandLayoutResult Compute(int count, float defaultSpacing, float zoneLength, float cardLength, float minSpacing, int maxVisible = int.MaxValue)
        {
            if (count <= 0) return new HandLayoutResult(0, 0f, false);
            if (maxVisible < 1) maxVisible = 1;
            bool capped = count > maxVisible;
            int shown = capped ? maxVisible : count;
            HandLayoutResult fit = ComputeFit(shown, defaultSpacing, zoneLength, cardLength, minSpacing);
            return new HandLayoutResult(fit.VisibleCount, fit.Spacing, capped || fit.Truncated);
        }

        private static HandLayoutResult ComputeFit(int count, float defaultSpacing, float zoneLength, float cardLength, float minSpacing)
        {
            if (count <= 0) return new HandLayoutResult(0, 0f, false);
            if (cardLength < 0f) cardLength = 0f;
            if (minSpacing < 0f) minSpacing = 0f;
            if (defaultSpacing < minSpacing) defaultSpacing = minSpacing;
            // Une seule carte : aucune division.
            if (count == 1) return new HandLayoutResult(1, defaultSpacing, false);
            float free = zoneLength - cardLength;
            // Zone trop petite pour deux cartes : on n'en montre qu'une.
            if (free <= 0f) return new HandLayoutResult(1, defaultSpacing, true);
            float fitted = free / (count - 1);
            float spacing = Math.Min(defaultSpacing, fitted);
            if (spacing >= minSpacing) return new HandLayoutResult(count, spacing, false);
            // Plancher atteint : on plafonne au nombre de dos qui tiennent proprement.
            int capacity = 1 + (int)Math.Floor(free / minSpacing);
            if (capacity < 1) capacity = 1;
            if (capacity >= count) return new HandLayoutResult(count, minSpacing, false);
            return new HandLayoutResult(capacity, minSpacing, true);
        }
    }
}
