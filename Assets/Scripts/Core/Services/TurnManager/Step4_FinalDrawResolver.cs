using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 4 — RE-PIOCHE FINALE : ré-pioche si la main du joueur est < 3
    /// et la Pioche non épuisée, après application des effets spéciaux
    /// (notamment après un Don de carte via l'effet du 7).
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets", point 4.
    /// À jauger avec Step3 : le Don du 7 pourrait nécessiter d'appeler
    /// directement cette logique de re-pioche depuis le handler du 7.
    /// </summary>
    public static class Step4_FinalDrawResolver
    {
        public static TurnResult Resolve(GameState state, int playerId)
        {
            throw new System.NotImplementedException("Chat #5");
        }
    }
}