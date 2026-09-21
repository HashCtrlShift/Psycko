using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 6 — JOUEUR SUIVANT : transition du tour selon PlayDirection,
    /// en tenant compte de SkipNext (Doublon) et Replay (Carré).
    /// Gère aussi les transitions de Phase pour le joueur (AdvancePlayerPhase,
    /// passage Phase 2 → Phase 3 "Chance"/FaceDown, détection de fin de partie
    /// "Psycko" pour le dernier joueur restant avec des cartes).
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets", point 6,
    /// et section "Phase 3 / Chance / Fin de Partie" (lignes 154-183).
    /// </summary>
    public static class Step6_AdvanceTurnResolver
    {
        public static TurnResult Resolve(GameState state, bool skipNext, bool replay)
        {
            throw new System.NotImplementedException("Chat #7");
        }
    }
}