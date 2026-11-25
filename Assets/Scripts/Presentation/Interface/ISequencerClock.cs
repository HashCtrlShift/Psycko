namespace Psycko.Presentation.Logic
{
    /// <summary>Source injectable de temps, indépendante de UnityEngine.</summary>
    public interface ISequencerClock
    {
        float DeltaTimeSeconds { get; }
    }

    /// <summary>Valeurs pures transmises du modèle Unity au séquenceur.</summary>
    public struct PresentationTimingValues
    {
        public readonly float PreviewBeforeClearSeconds;
        public readonly float ForcedPickupDelaySeconds;
        public readonly float BotDelaySeconds;

        public PresentationTimingValues(float previewBeforeClearSeconds, float forcedPickupDelaySeconds, float botDelaySeconds)
        {
            PreviewBeforeClearSeconds = NonNegative(previewBeforeClearSeconds);
            ForcedPickupDelaySeconds = NonNegative(forcedPickupDelaySeconds);
            BotDelaySeconds = NonNegative(botDelaySeconds);
        }

        private static float NonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }
    }
}
