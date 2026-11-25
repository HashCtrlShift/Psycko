using Psycko.Presentation.Logic;
using UnityEngine;

namespace Psycko.Presentation.Models
{
    /// <summary>Configuration Unity des temporisations de présentation.</summary>
    [CreateAssetMenu(menuName = "Psycko/Presentation Timings")]
    public sealed class PresentationTimings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _previewBeforeClearSeconds = 1.0f;
        [SerializeField, Min(0f)] private float _forcedPickupDelaySeconds = 1.0f;
        // BotPacing du contrôleur sur main : _botDelaySeconds = 1.0f.
        [SerializeField, Min(0f)] private float _botDelaySeconds = 1.0f;

        public float PreviewBeforeClearSeconds { get { return _previewBeforeClearSeconds; } }
        public float ForcedPickupDelaySeconds { get { return _forcedPickupDelaySeconds; } }
        public float BotDelaySeconds { get { return _botDelaySeconds; } }

        public PresentationTimingValues ToSettings()
        {
            return new PresentationTimingValues(_previewBeforeClearSeconds,
                _forcedPickupDelaySeconds, _botDelaySeconds);
        }

        private void OnValidate()
        {
            _previewBeforeClearSeconds = Mathf.Max(0f, _previewBeforeClearSeconds);
            _forcedPickupDelaySeconds = Mathf.Max(0f, _forcedPickupDelaySeconds);
            _botDelaySeconds = Mathf.Max(0f, _botDelaySeconds);
        }
    }
}
