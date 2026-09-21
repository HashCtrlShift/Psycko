using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 1 — POSE : les cartes du Play quittent leur couche d'origine
    /// (Hand, FaceUp ou FaceDown selon Play.SourceLayer) et entrent dans Pile.
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets", point 1.
    /// Doit s'appuyer sur IGameStateCommand.PlayCards(Play play).
    /// </summary>
    public static class Step1_PlaceCardsResolver
    {
        public static TurnResult Resolve(GameState state, Play play)
        {
            throw new System.NotImplementedException("Chat #2");
        }
    }
}