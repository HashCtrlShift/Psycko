using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Résultat immutable d'une étape de résolution de tour.
    /// Porte l'état (jamais muté par un Resolver) ainsi que les drapeaux de flux
    /// (skip du joueur suivant, rejeu du joueur actif, ramassage détecté par Step0),
    /// les INTENTIONS de reconstruction produites par Step2/Step4 (pioche, ramassage FaceUp,
    /// transition de phase) et les INTENTIONS d'effets produites par Step3 (contrainte,
    /// direction, destruction de pile, rejeu, Don du 7).
    /// Toutes sont traduites en IGameStateCommand par GameOrchestrator, seul habilité à muter.
    /// Struct volontairement simple : pas de record/init (compatibilité Unity/IsExternalInit).
    /// </summary>
    public readonly struct TurnResult
    {
        public GameState State { get; }
        public bool SkipNext { get; }
        public bool Replay { get; }
        public bool IsPickup { get; }

        // --- Intentions Step2 / Step4 (reconstruction) ---

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

        // --- Intentions Step3 (effets spéciaux) ---

        /// <summary>Contrainte de hauteur à appliquer pour le coup suivant ; null si Step3 n'a pas été exécuté.</summary>
        public HeightConstraint? NextConstraint { get; }

        /// <summary>Rang de référence associé à NextConstraint ; null si Step3 n'a pas été exécuté.</summary>
        public DefRank? NextRefRank { get; }

        /// <summary>Sens de jeu à appliquer ; null si Step3 n'a pas été exécuté.</summary>
        public PlayDirection? NextDirection { get; }

        /// <summary>Intention de destruction de la pile centrale (ex. 2/Bombe selon handler).</summary>
        public bool DestroysPile { get; }

        /// <summary>Intention de rejeu portée par un effet de carte (distinct de Replay, décidé par Step5 sur Carré).</summary>
        public bool GrantsReplay { get; }

        /// <summary>
        /// Don du 7 requis. Booléen (UN don par coup, quel que soit le nombre de 7 posés).
        /// Si vrai, TurnManager.ApplyPlay s'arrête après Step3 ; GameOrchestrator résout le Don
        /// puis appelle TurnManager.ResolveRemainder pour Step4→Step6.
        /// </summary>
        public bool RequiresGiftResolution { get; }

        public TurnResult(
            GameState state,
            bool skipNext,
            bool replay,
            bool isPickup = false,
            int drawCount = 0,
            bool triggersFaceUpPickup = false,
            bool triggersPhaseTransition = false,
            DefPhase? targetPhase = null,
            HeightConstraint? nextConstraint = null,
            DefRank? nextRefRank = null,
            PlayDirection? nextDirection = null,
            bool destroysPile = false,
            bool grantsReplay = false,
            bool requiresGiftResolution = false)
        {
            State = state;
            SkipNext = skipNext;
            Replay = replay;
            IsPickup = isPickup;
            DrawCount = drawCount;
            TriggersFaceUpPickup = triggersFaceUpPickup;
            TriggersPhaseTransition = triggersPhaseTransition;
            TargetPhase = targetPhase;
            NextConstraint = nextConstraint;
            NextRefRank = nextRefRank;
            NextDirection = nextDirection;
            DestroysPile = destroysPile;
            GrantsReplay = grantsReplay;
            RequiresGiftResolution = requiresGiftResolution;
        }

        /// <summary>Copie sélective : chaque paramètre null conserve la valeur courante.</summary>
        private TurnResult Copy(
            GameState state = null,
            bool? skipNext = null, bool? replay = null, bool? isPickup = null,
            int? drawCount = null, bool? faceUp = null, bool? phaseTr = null,
            DefPhase? target = null, bool clearTarget = false,
            HeightConstraint? cons = null, DefRank? refRank = null, PlayDirection? dir = null,
            bool? destroys = null, bool? grants = null, bool? gift = null)
        {
            return new TurnResult(
                state ?? State,
                skipNext ?? SkipNext,
                replay ?? Replay,
                isPickup ?? IsPickup,
                drawCount ?? DrawCount,
                faceUp ?? TriggersFaceUpPickup,
                phaseTr ?? TriggersPhaseTransition,
                clearTarget ? null : (target ?? TargetPhase),
                cons ?? NextConstraint,
                refRank ?? NextRefRank,
                dir ?? NextDirection,
                destroys ?? DestroysPile,
                grants ?? GrantsReplay,
                gift ?? RequiresGiftResolution);
        }

        // --- Flux ---
        public TurnResult WithState(GameState state) => Copy(state: state);
        public TurnResult WithSkipNext(bool skipNext) => Copy(skipNext: skipNext);
        public TurnResult WithReplay(bool replay) => Copy(replay: replay);
        public TurnResult WithIsPickup(bool isPickup) => Copy(isPickup: isPickup);

        // --- Reconstruction (Step2 / Step4) ---
        public TurnResult WithDrawCount(int drawCount) => Copy(drawCount: drawCount);
        public TurnResult WithFaceUpPickup(bool triggersFaceUpPickup) => Copy(faceUp: triggersFaceUpPickup);
        public TurnResult WithPhaseTransition(DefPhase targetPhase) => Copy(phaseTr: true, target: targetPhase);
        public TurnResult WithoutPhaseTransition() => Copy(phaseTr: false, clearTarget: true);

        // --- Effets (Step3) ---
        public TurnResult WithNextConstraint(HeightConstraint constraint, DefRank refRank) => Copy(cons: constraint, refRank: refRank);
        public TurnResult WithNextDirection(PlayDirection direction) => Copy(dir: direction);
        public TurnResult WithDestroysPile(bool destroysPile) => Copy(destroys: destroysPile);
        public TurnResult WithGrantsReplay(bool grantsReplay) => Copy(grants: grantsReplay);
        public TurnResult WithRequiresGiftResolution(bool requiresGift) => Copy(gift: requiresGift);
    }
}