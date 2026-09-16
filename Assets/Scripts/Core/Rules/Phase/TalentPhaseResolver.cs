using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Phase
{
    /// <summary>
    /// Phase 2 — Le Talent. Seule la main est jouable : les FaceUp ont déjà été
    /// ramassées dans la main à l'entrée de la phase (transition Phase 1 → 2,
    /// pilotée par TurnManager), donc FaceUp n'est plus une couche jouable distincte.
    ///
    /// Transition vers la Chance quand la main est vide. La pioche est par
    /// définition déjà épuisée à ce stade (condition de sortie de Phase 1).
    /// </summary>
    public sealed class TalentPhaseResolver : PhaseResolver
    {
        public override DefPhase Phase => DefPhase.Talent;

        public override DefPhase NextPhase => DefPhase.Luck;

        public override bool IsLayerPlayable(Player player, CardLayer layer)
        {
            if (player is null) throw new System.ArgumentNullException(nameof(player));

            return layer == CardLayer.Hand && player.Hand.Count > 0;
        }

        public override bool ShouldTransitionToNextPhase(Player player)
        {
            if (player is null) throw new System.ArgumentNullException(nameof(player));

            return player.Hand.Count == 0;
        }
    }
}