using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Résultat immutable d'une étape de résolution de tour.
    /// Porte l'état (jamais muté par un Resolver) ainsi que les drapeaux de flux
    /// (skip du joueur suivant, rejeu du joueur actif, ramassage détecté par Step0)
    /// et les INTENTIONS de reconstruction produites par Step2 (pioche, ramassage FaceUp,
    /// transition de phase), traduites en IGameStateCommand par GameOrchestrator.
    /// Struct volontairement simple : pas de record/init (compatibilité Unity/IsExternalInit).
    /// </summary>
    public readonly struct TurnResult
    {
        public GameState State { get; }
        public bool SkipNext { get; }
        public bool Replay { get; }
        public bool IsPickup { get; }

        /// <summary>
        /// Cartes à piocher (sous-temps 1 de la reconstruction Work). 0 hors Work.
        /// Vaut 0 par construction quand TriggersPhaseTransition est vrai en Work
        /// (la pioche est alors épuisée).
        /// </summary>
        public int DrawCount { get; }

        /// <summary>Intention de ramassage FaceUp→Hand (sous-temps 2 de Work). Toujours false hors Work.</summary>
        public bool TriggersFaceUpPickup { get; }

        /// <summary>Intention de transition de phase (Work→Talent ou Talent→Luck).</summary>
        public bool TriggersPhaseTransition { get; }

        /// <summary>Phase cible si TriggersPhaseTransition est vrai ; null sinon.</summary>
        public DefPhase? TargetPhase { get; }

        public TurnResult(
            GameState state,
            bool skipNext,
            bool replay,
            bool isPickup = false,
            int drawCount = 0,
            bool triggersFaceUpPickup = false,
            bool triggersPhaseTransition = false,
            DefPhase? targetPhase = null)
        {
            State = state;
            SkipNext = skipNext;
            Replay = replay;
            IsPickup = isPickup;
            DrawCount = drawCount;
            TriggersFaceUpPickup = triggersFaceUpPickup;
            TriggersPhaseTransition = triggersPhaseTransition;
            TargetPhase = targetPhase;
        }

        public TurnResult WithState(GameState state)
        {
            return new TurnResult(state, SkipNext, Replay, IsPickup,
                DrawCount, TriggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithSkipNext(bool skipNext)
        {
            return new TurnResult(State, skipNext, Replay, IsPickup,
                DrawCount, TriggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithReplay(bool replay)
        {
            return new TurnResult(State, SkipNext, replay, IsPickup,
                DrawCount, TriggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithIsPickup(bool isPickup)
        {
            return new TurnResult(State, SkipNext, Replay, isPickup,
                DrawCount, TriggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithDrawCount(int drawCount)
        {
            return new TurnResult(State, SkipNext, Replay, IsPickup,
                drawCount, TriggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithFaceUpPickup(bool triggersFaceUpPickup)
        {
            return new TurnResult(State, SkipNext, Replay, IsPickup,
                DrawCount, triggersFaceUpPickup, TriggersPhaseTransition, TargetPhase);
        }

        public TurnResult WithPhaseTransition(DefPhase targetPhase)
        {
            return new TurnResult(State, SkipNext, Replay, IsPickup,
                DrawCount, TriggersFaceUpPickup, true, targetPhase);
        }

        public TurnResult WithoutPhaseTransition()
        {
            return new TurnResult(State, SkipNext, Replay, IsPickup,
                DrawCount, TriggersFaceUpPickup, false, null);
        }
    }
}