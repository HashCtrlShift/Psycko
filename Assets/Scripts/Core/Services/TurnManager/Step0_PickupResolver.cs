using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 0 — Ramassage (volontaire via bouton "Ramasser" côté Présentation, ou forcé
    /// si le joueur actif n'a aucune carte jouable).
    /// CLAUDE.md, section "Ordre Strict d'Application des Effets" — précède l'étape POSE.
    /// Ne mute JAMAIS l'état (Lecture 1 : Step0 est un décideur, pas un exécutant).
    /// La mutation réelle (IGameStateCommand.PickUpPile) relève de GameOrchestrator,
    /// jamais de TurnManager ni de ses Resolvers.
    /// </summary>
    public static class Step0_PickupResolver
    {
        private static readonly CardPlayabilityChecker _playabilityChecker = new CardPlayabilityChecker();

        /// <summary>
        /// Détermine si ce tour est un tour de ramassage (forcé ou volontaire).
        /// State retourné = state d'entrée, INCHANGÉ. IsPickup=true signale à TurnManager
        /// de court-circuiter Step1-5 et de déléguer le ramassage réel à GameOrchestrator
        /// avant d'appeler Step6.
        /// </summary>
        /// <param name="voluntaryPickup">
        /// True si le joueur a explicitement déclenché le ramassage (bouton "Ramasser"
        /// côté Présentation), indépendamment de la jouabilité de sa main.
        /// </param>
        public static TurnResult Resolve(GameState state, int playerId, bool voluntaryPickup)
        {
            if (voluntaryPickup)
            {
                return new TurnResult(state, skipNext: false, replay: false, isPickup: true);
            }

            var player = state.Players.First(p => p.Id == playerId);
            bool hasPlayableCard = _playabilityChecker.HasAnyPlayableCard(player, state);

            bool forcedPickup = !hasPlayableCard;

            return new TurnResult(state, skipNext: false, replay: false, isPickup: forcedPickup);
        }
    }
}