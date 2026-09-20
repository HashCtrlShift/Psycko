using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Phase;

namespace Psycko.Core.Rules.Validation
{
    /// <summary>
    /// Implémentation d'ICardPlayabilityChecker. Compose CardPlayability (légalité de hauteur)
    /// et PhaseResolver (couches actives) — ne réécrit aucune des deux logiques.
    /// </summary>
    public sealed class CardPlayabilityChecker : ICardPlayabilityChecker
    {
        private static readonly IReadOnlyDictionary<DefPhase, PhaseResolver> _resolvers =
            new Dictionary<DefPhase, PhaseResolver>
            {
                [DefPhase.Work] = new WorkPhaseResolver(),
                [DefPhase.Talent] = new TalentPhaseResolver(),
                [DefPhase.Luck] = new LuckPhaseResolver(),
            };

        public bool IsPlayable(Card card, IGameStateQuery state)
            => CardPlayability.IsPlayable(card, state);

        public bool HasAnyPlayableCard(Player player, IGameStateQuery state)
        {
            if (player is null) throw new System.ArgumentNullException(nameof(player));
            if (state is null) throw new System.ArgumentNullException(nameof(state));

            if (!_resolvers.TryGetValue(player.CurrentPhase, out var resolver))
            {
                // DefPhase.Finished (ou toute phase sans resolver) : plus aucune couche jouable.
                return false;
            }

            if (resolver.IsLayerPlayable(player, CardLayer.Hand))
            {
                return CardPlayability.GetPlayableCards(player.Hand, state).Any();
            }

            if (resolver.IsLayerPlayable(player, CardLayer.FaceDown))
            {
                return CardPlayability.GetPlayableCards(player.FaceDown, state).Any();
            }

            return false;
        }
    }
}