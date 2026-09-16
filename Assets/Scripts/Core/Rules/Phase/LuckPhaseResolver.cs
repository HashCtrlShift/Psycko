using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Phase
{
    /// <summary>
    /// Phase 3 — La Chance. Exclusivité mutuelle entre Hand et FaceDown selon l'état du joueur :
    /// tant que la main n'est pas vide, elle est seule jouable (le joueur doit s'en débarrasser
    /// avant de pouvoir révéler une FaceDown). Une fois la main vide, seule FaceDown est jouable.
    ///
    /// Transition vers Finished quand le joueur n'a plus aucune carte dans aucune couche
    /// (Hand, FaceUp et FaceDown tous vides) : il sort du jeu, la partie continue sans lui.
    /// </summary>
    public sealed class LuckPhaseResolver : PhaseResolver
    {
        public override DefPhase Phase => DefPhase.Luck;

        public override DefPhase NextPhase => DefPhase.Finished;

        public override bool IsLayerPlayable(Player player, CardLayer layer)
        {
            if (player is null) throw new System.ArgumentNullException(nameof(player));

            if (player.Hand.Count > 0)
                return layer == CardLayer.Hand;

            return layer == CardLayer.FaceDown && player.FaceDown.Count > 0;
        }

        public override bool ShouldTransitionToNextPhase(Player player)
        {
            if (player is null) throw new System.ArgumentNullException(nameof(player));

            return !player.HasCards;
        }
    }
}