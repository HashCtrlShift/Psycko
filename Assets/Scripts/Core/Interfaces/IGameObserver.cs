#nullable enable

using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Écoute passive d'une partie en direct (Console, futur Présentation).
    /// Side-effect pur : aucune logique de jeu ne dépend de l'observateur.
    /// </summary>
    public interface IGameObserver
    {
        /// <summary>Appelé à chaque entrée de log, dans l'ordre.</summary>
        void OnLogEntry(GameLogEntry entry);

        /// <summary>Appelé après la pose et le débit des cartes, avant les effets du coup.</summary>
        void OnCardsPlaced(GameState stateAfterPlacement, PlacementInfo info);

        /// <summary>Appelé après chaque échange pré-partie appliqué, avant la requête suivante.</summary>
        void OnExchangeStateChanged(PreGameExchangeState state);

        /// <summary>Appelé après chaque tour avec l'état réel de la partie.</summary>
        void OnStateChanged(GameState state);
    }
}