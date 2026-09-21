using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Résultat immutable d'une étape de résolution de tour.
    /// Porte l'état mis à jour ainsi que les drapeaux de flux (skip du joueur suivant,
    /// rejeu du joueur actif) déterminés par Step5_PileEffectsResolver.
    /// Struct volontairement simple : pas de record/init (compatibilité Unity/IsExternalInit).
    /// </summary>
    public readonly struct TurnResult
    {
        public GameState State { get; }
        public bool SkipNext { get; }
        public bool Replay { get; }

        public TurnResult(GameState state, bool skipNext, bool replay)
        {
            State = state;
            SkipNext = skipNext;
            Replay = replay;
        }

        public TurnResult WithState(GameState state)
        {
            return new TurnResult(state, SkipNext, Replay);
        }

        public TurnResult WithSkipNext(bool skipNext)
        {
            return new TurnResult(State, skipNext, Replay);
        }

        public TurnResult WithReplay(bool replay)
        {
            return new TurnResult(State, SkipNext, replay);
        }
    }
}