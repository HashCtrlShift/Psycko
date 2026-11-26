namespace Psycko.Presentation.Logic
{
    /// <summary>Garde réutilisable qui ne laisse réclamer qu'une fois un ramassage forcé.</summary>
    public sealed class ForcedPickupOnce
    {
        private bool _claimed;

        /// <summary>Retourne true à la première réclamation, puis false pour tous les appels suivants.</summary>
        public bool TryClaim()
        {
            if (_claimed) return false;
            _claimed = true;
            return true;
        }
    }
}