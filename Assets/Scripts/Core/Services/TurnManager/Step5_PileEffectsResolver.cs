using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 5 — EFFETS DE PILE : évaluation Doublon/Carré en relisant Pile.Cards,
    /// avec priorité au Carré (cumul ≥ 4) sur le Doublon (cumul ≥ 2).
    /// Carré : détruit la Pile (DestroyPile), le poseur rejoue (Replay = true).
    /// Doublon : redéclenche un skip du joueur suivant (SkipNext = true) ;
    /// désactivé à ≤ 2 joueurs ; jamais sur pile vide.
    /// Joker de Verre : transparent pour la chaîne. Joker Noir : casse la chaîne.
    /// Joker Couleur/Bombe : détruit la pile.
    /// CLAUDE.md, section "Effets de Pile — Doublon / Carré" (lignes 220-262).
    /// Renseigne SkipNext / Replay dans le TurnResult retourné.
    /// </summary>
    public static class Step5_PileEffectsResolver
    {
        public static TurnResult Resolve(GameState state, Play play)
        {
            throw new System.NotImplementedException("Chat #6");
        }
    }
}