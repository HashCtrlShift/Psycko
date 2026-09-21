using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 2 — RECONSTRUCTION : reconstitution de la main du poseur.
    /// Pioche (DrawCards) si main < 3 et Pioche non épuisée ; cette étape est sautée
    /// si la Pioche est épuisée. Gère aussi la transition Phase 1 → Phase 2
    /// (AdvancePlayerPhase + ramassage des cartes FaceUp), suivie d'une ré-pioche
    /// si la main est encore < 3 après ramassage.
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets", point 2,
    /// et section Pioche/Fin de Phase 1 (lignes 91-99, 116-120).
    /// </summary>
    public static class Step2_ReconstructionResolver
    {
        public static TurnResult Resolve(GameState state, int playerId)
        {
            throw new System.NotImplementedException("Chat #3");
        }
    }
}