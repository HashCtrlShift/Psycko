using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Orchestrateur du tour : enchaîne les 7 étapes de l'Ordre Strict
    /// défini dans CLAUDE.md, sans aucune logique métier propre.
    /// Chaque étape est implémentée dans son propre Resolver (Step0 → Step6).
    /// </summary>
    public static class TurnManager
    {
        public static GameState ApplyPlay(GameState state, Play play)
        {
            var result = Step0_PickupResolver.Resolve(state, play.PlayerId);
            result = Step1_PlaceCardsResolver.Resolve(result.State, play);
            result = Step2_ReconstructionResolver.Resolve(result.State, play.PlayerId);
            result = Step3_CardEffectsResolver.Resolve(result.State, play);
            result = Step4_FinalDrawResolver.Resolve(result.State, play.PlayerId);
            result = Step5_PileEffectsResolver.Resolve(result.State, play);
            result = Step6_AdvanceTurnResolver.Resolve(result.State, result.SkipNext, result.Replay);

            return result.State;
        }
    }
}