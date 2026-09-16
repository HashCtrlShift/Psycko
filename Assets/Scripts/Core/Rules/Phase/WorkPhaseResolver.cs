using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Phase
{
    /// <summary>
    /// Phase 1 — Le Travail. Seule la main est jouable.
    ///
    /// Transition vers le Talent quand la main est vide.
    ///
    /// INVARIANT REQUIS (garanti par TurnManager, non vérifiable ici) :
    /// cette méthode doit être appelée APRÈS l'étape 2 de l'ordre strict
    /// (reconstruction de main / re-pioche). À ce point, Hand.Count == 0
    /// implique que la pioche est définitivement épuisée — sinon le joueur
    /// aurait repioché. La condition "DrawPile.Count == 0" est donc déjà
    /// satisfaite par construction et n'a pas à être testée ici.
    /// Player ne porte aucune information sur la pioche.
    /// </summary>
    public sealed class WorkPhaseResolver : PhaseResolver
    {
        public override DefPhase Phase => DefPhase.Work;

        public override DefPhase NextPhase => DefPhase.Talent;

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