using Psycko.Core.Domain;

namespace Psycko.Presentation.Logic
{
    /// <summary>Validation structurelle du siège cible d'un Don; n'applique aucune règle de jeu.</summary>
    public static class GiftTargetValidator
    {
        public static bool IsValid(int donorPlayerId, int targetPlayerId, DefPhase targetPhase)
        {
            return donorPlayerId != targetPlayerId && targetPhase != DefPhase.Finished;
        }
    }
}
