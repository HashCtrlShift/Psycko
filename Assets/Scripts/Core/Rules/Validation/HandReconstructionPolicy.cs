using System;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Validation
{
    /// <summary>
    /// Politique pure de reconstruction de la main après retrait de cartes
    /// (pose validée en Step1, ou Don exécuté entre Step3 et Step4).
    /// Une seule responsabilité : DÉCIDER (jamais muter) pioche / ramassage FaceUp /
    /// transition de phase. Réutilisée par Step2 (cardsRemovedFromHand = play.Count)
    /// et Step4 (cardsRemovedFromHand = 1 si Don effectué, sinon 0).
    /// </summary>
    public static class HandReconstructionPolicy
    {
        private const int HandTarget = 3;

        public static HandReconstructionResult Reconstruct(
            IGameStateQuery state, int playerIndex, int cardsRemovedFromHand)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            if (cardsRemovedFromHand < 0)
                throw new ArgumentOutOfRangeException(nameof(cardsRemovedFromHand));

            var player = state.Players[playerIndex];
            int handAfter = player.Hand.Count - cardsRemovedFromHand;
            if (handAfter < 0)
                throw new InvalidOperationException(
                    $"cardsRemovedFromHand ({cardsRemovedFromHand}) dépasse la main du joueur " +
                    $"ID={player.Id} (Hand.Count={player.Hand.Count}). Step1 aurait dû rejeter ce cas.");

            switch (player.CurrentPhase)
            {
                case DefPhase.Work:   return ResolveWork(state, handAfter);
                case DefPhase.Talent: return ResolveTalent(player, handAfter);
                case DefPhase.Luck:   return HandReconstructionResult.None;
                default:              return HandReconstructionResult.None; // Finished
            }
        }

        /// <summary>Phase 1 : pioche jusqu'à 3 bornée par la pioche ; Work→Talent ssi main vide ET pioche vide après pioche.</summary>
        private static HandReconstructionResult ResolveWork(IGameStateQuery state, int handAfter)
        {
            int pile = state.DrawPile.Count;
            int needed = HandTarget - handAfter;
            int draw = needed <= 0 ? 0 : Math.Min(needed, pile);
            bool transition = (handAfter + draw) == 0 && (pile - draw) == 0;

            return new HandReconstructionResult(
                drawCount: draw,
                triggersFaceUpPickup: transition,
                triggersPhaseTransition: transition,
                targetPhase: transition ? DefPhase.Talent : (DefPhase?)null);
        }

        /// <summary>Phase 2 : aucune pioche ; Talent→Luck ssi main vide après retrait.</summary>
        private static HandReconstructionResult ResolveTalent(Player player, int handAfter)
        {
            if (player.FaceUp.Count != 0)
                throw new InvalidOperationException(
                    $"Invariant violé : FaceUp doit être vide en phase Talent (joueur ID={player.Id}).");

            bool transition = handAfter == 0;
            return new HandReconstructionResult(
                drawCount: 0,
                triggersFaceUpPickup: false,
                triggersPhaseTransition: transition,
                targetPhase: transition ? DefPhase.Luck : (DefPhase?)null);
        }
    }

    /// <summary>Résultat immutable de la politique de reconstruction.</summary>
    public readonly struct HandReconstructionResult
    {
        public int DrawCount { get; }
        public bool TriggersFaceUpPickup { get; }
        public bool TriggersPhaseTransition { get; }
        public DefPhase? TargetPhase { get; }

        /// <summary>
        /// Toujours false : un joueur ne peut JAMAIS donner une carte Face Cachée (Couche 3),
        /// même en phase Chance. Exposé pour que GameOrchestrator restreigne le choix du Don
        /// à la main reconstituée (et aux FaceUp ramassées le cas échéant).
        /// </summary>
        public bool AllowsFaceDownGift => false;

        public HandReconstructionResult(int drawCount, bool triggersFaceUpPickup,
            bool triggersPhaseTransition, DefPhase? targetPhase)
        {
            DrawCount = drawCount;
            TriggersFaceUpPickup = triggersFaceUpPickup;
            TriggersPhaseTransition = triggersPhaseTransition;
            TargetPhase = targetPhase;
        }

        public static HandReconstructionResult None => new HandReconstructionResult(0, false, false, null);
    }
}