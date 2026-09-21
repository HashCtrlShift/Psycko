using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Résultat immutable d'une étape de résolution de tour.
    /// Porte l'état mis à jour ainsi que les drapeaux de flux (skip du joueur suivant,
    /// rejeu du joueur actif, ramassage détecté par Step0) déterminés en cours de séquence.
    /// Struct volontairement simple : pas de record/init (compatibilité Unity/IsExternalInit).
    /// </summary>
    public readonly struct TurnResult
    {
        public GameState State { get; }
        public bool SkipNext { get; }
        public bool Replay { get; }
        public bool IsPickup { get; }

        public TurnResult(GameState state, bool skipNext, bool replay, bool isPickup = false)
        {
            State = state;
            SkipNext = skipNext;
            Replay = replay;
            IsPickup = isPickup;
        }

        public TurnResult WithState(GameState state)
        {
            return new TurnResult(state, SkipNext, Replay, IsPickup);
        }

        public TurnResult WithSkipNext(bool skipNext)
        {
            return new TurnResult(State, skipNext, Replay, IsPickup);
        }

        public TurnResult WithReplay(bool replay)
        {
            return new TurnResult(State, SkipNext, replay, IsPickup);
        }

        public TurnResult WithIsPickup(bool isPickup)
        {
            return new TurnResult(State, SkipNext, Replay, isPickup);
        }
    }
}