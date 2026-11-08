using System;

namespace Psycko.Presentation.Models
{
    /// <summary>Minuteur monotone indépendant de Unity.</summary>
    public sealed class CountdownTimer
    {
        private bool _running;
        private bool _unlimited;
        private bool _expired;
        private bool _expirationRaised;
        private float _remaining;
        private float _total;

        /// <summary>Émis une seule fois lorsque le compte à rebours atteint zéro.</summary>
        public event Action Expired;

        public float Remaining { get { return _unlimited ? float.PositiveInfinity : _remaining; } }
        public float Total { get { return _total; } }
        public bool IsRunning { get { return _running; } }
        public bool IsUnlimited { get { return _unlimited; } }
        public bool IsExpired { get { return _expired; } }

        /// <summary>Démarre ou réinitialise le minuteur; une durée non positive est illimitée.</summary>
        public void Start(float seconds)
        {
            _unlimited = seconds <= 0f;
            _total = _unlimited ? 0f : seconds;
            _remaining = _unlimited ? 0f : seconds;
            _expired = false;
            _expirationRaised = false;
            _running = true;
        }

        /// <summary>Fait avancer le compte à rebours si le temps écoulé est positif.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f || !_running || _unlimited) return;

            _remaining -= dt;
            if (_remaining > 0f) return;

            _remaining = 0f;
            _running = false;
            _expired = true;
            if (_expirationRaised) return;

            _expirationRaised = true;
            Action handler = Expired;
            if (handler != null) handler();
        }

        /// <summary>Arrête le minuteur sans émettre l’événement d’expiration.</summary>
        public void Stop()
        {
            _running = false;
            _unlimited = false;
            _expired = false;
            _remaining = 0f;
            _total = 0f;
        }
    }
}