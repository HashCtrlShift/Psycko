namespace Psycko.Presentation.Logic
{
    /// <summary>Calcule une pose stable de carte depuis son index absolu dans la Pile.</summary>
    public static class PilePoseCalculator
    {
        /// <summary>Valeurs de pose (unités d'interface pour X/Y et degrés pour RotationZ).</summary>
        public struct Pose
        {
            public readonly float X;
            public readonly float Y;
            public readonly float RotationZ;

            public Pose(float x, float y, float rotationZ)
            {
                X = x;
                Y = y;
                RotationZ = rotationZ;
            }
        }

        /// <summary>
        /// Calcule une pose déterministe indépendante du nombre de cartes de la Pile.
        /// Les plages négatives sont ramenées à zéro.
        /// </summary>
        public static Pose Calculate(int absoluteIndex, float maxOffsetX, float maxOffsetY, float maxRotation)
        {
            float xRange = NonNegative(maxOffsetX);
            float yRange = NonNegative(maxOffsetY);
            float rotationRange = NonNegative(maxRotation);

            float x = (Unit(Hash(absoluteIndex, 1)) * 2f - 1f) * xRange;
            float y = (Unit(Hash(absoluteIndex, 2)) * 2f - 1f) * yRange;
            float rotation = (Unit(Hash(absoluteIndex, 3)) * 2f - 1f) * rotationRange;
            return new Pose(x, y, rotation);
        }

        private static float NonNegative(float value)
        {
            return value > 0f ? value : 0f;
        }

        /// <summary>Hash entier déterministe identique à l'ancien calcul de ZoneView.</summary>
        private static uint Hash(int index, int salt)
        {
            unchecked
            {
                uint h = (uint)index * 2654435761u + (uint)salt * 40503u;
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>Hash converti dans [0, 1], comme le calcul historique.</summary>
        private static float Unit(uint hash)
        {
            return (hash & 0xFFFFu) / 65535f;
        }
    }
}
