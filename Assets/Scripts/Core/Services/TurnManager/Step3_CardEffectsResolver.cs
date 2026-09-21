using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 3 — EFFETS SPÉCIAUX DE CARTES : l'effet de la ou des cartes posées
    /// (7-Don, 2, Valet, Prêtre) ou du Joker joué (Verre, Noir, Couleur/Bombe)
    /// est interrogé ICI, une fois la main du poseur dans son état final (post-reconstruction).
    /// Un 7 en dernière carte peut ainsi permettre au joueur de donner la carte
    /// qu'il vient de piocher à l'étape 2.
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets", point 3
    /// et avertissement associé.
    /// Les handlers (SevenHandler, TwoHandler, JackHandler, PriestHandler,
    /// GlassJokerResolver, BlackJokerResolver, ColorJokerResolver) DÉCLARENT
    /// les effets via IGameStateQuery ; ce Step est responsable de les APPLIQUER.
    /// </summary>
    public static class Step3_CardEffectsResolver
    {
        public static TurnResult Resolve(GameState state, Play play)
        {
            throw new System.NotImplementedException("Chat #4");
        }
    }
}