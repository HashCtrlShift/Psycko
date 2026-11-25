using System;

namespace Psycko.Presentation.Controllers
{
    /// <summary>Garde C# pur contre l’expiration répétée d’une requête temporisée.</summary>
    public sealed class HumanRequestController
    {
        private bool _expired;

        /// <summary>Réarme la garde lorsqu’une requête temporisée commence.</summary>
        public void BeginRequest() { _expired = false; }

        /// <summary>Appelle l’expiration fournie une seule fois. Retourne false si déjà expirée ou callback absent.</summary>
        public bool ExpirePendingOnce(Action expirePending)
        {
            if (_expired || expirePending == null) return false;
            _expired = true;
            expirePending();
            return true;
        }
    }
}