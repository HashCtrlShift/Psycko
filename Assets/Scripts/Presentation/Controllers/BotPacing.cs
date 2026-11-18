using System;

namespace Psycko.Presentation.Controllers
{
    /// <summary>
    /// Rythme d'affichage des coups de bots (Présentation uniquement, aucune règle de jeu).
    /// Le Core décide instantanément ; ce composant décide seulement QUAND afficher
    /// le prochain StateChanged. Aucune dépendance à UnityEngine : le temps écoulé est injecté.
    /// </summary>
    public sealed class BotPacing
    {
        private readonly float _baseDelay;
        private readonly float _fastDelay;
        private readonly int _fastMaxEventsPerFrame;
        private float _remaining;
        private bool _fast;

        public BotPacing(float baseDelaySeconds, float fastDelaySeconds, int fastMaxEventsPerFrame)
        {
            _baseDelay = Math.Max(0f, baseDelaySeconds);
            _fastDelay = Math.Max(0f, fastDelaySeconds);
            _fastMaxEventsPerFrame = Math.Max(1, fastMaxEventsPerFrame);
            _remaining = 0f;
            _fast = false;
        }

        /// <summary>Vrai dès que l'humain a un rang (gagné ou éliminé). Irréversible.</summary>
        public bool IsFast { get { return _fast; } }

        /// <summary>Nombre maximal de StateChanged consommables pendant cette frame.</summary>
        public int MaxStateChangesThisFrame { get { return _fast ? _fastMaxEventsPerFrame : 1; } }

        /// <summary>Passe en mode accéléré (appelé quand GetFinishRank(humain) est non nul).</summary>
        public void EnterFastMode()
        {
            _fast = true;
            if (_remaining > _fastDelay) _remaining = _fastDelay;
        }

        /// <summary>Fait avancer l'attente de deltaTime secondes.</summary>
        public void Advance(float deltaTime)
        {
            if (_remaining > 0f) _remaining -= Math.Max(0f, deltaTime);
        }

        /// <summary>Vrai si l'attente est écoulée : un StateChanged peut être affiché.</summary>
        public bool CanDisplayNext { get { return _remaining <= 0f; } }

        /// <summary>Démarre l'attente qui suit un StateChanged affiché.</summary>
        public void StartWait()
        {
            _remaining = _fast ? _fastDelay : _baseDelay;
        }
    }
}