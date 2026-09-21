using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 0 — Ramassage (volontaire via bouton "Ramasser" côté Présentation, ou forcé
    /// si le joueur actif n'a aucune carte jouable).
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets" — précède l'étape POSE.
    /// Doit s'appuyer sur IGameStateCommand.PickUpPile(int playerIndex).
    /// </summary>
    public static class Step0_PickupResolver
    {
        public static TurnResult Resolve(GameState state, int playerId)
        {
            throw new System.NotImplementedException("Chat #1");
        }
    }
}