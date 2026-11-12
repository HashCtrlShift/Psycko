#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Ordre d'arrivée des joueurs (IMMUABLE). Contient les Ids de joueurs
    /// dans l'ordre où ils sont passés en DefPhase.Finished.
    /// Rang = position + 1 (1 = premier à finir). Un joueur absent n'a pas fini.
    /// Règle : un Id n'est enregistré qu'UNE fois ; Add sur un Id déjà présent
    /// retourne l'instance inchangée (jamais de réécriture ni de doublon).
    /// Fins multiples : l'ordre est celui des transitions effectuées.
    /// Le Psycko (dernier joueur, jamais Finished) n'est PAS enregistré ici :
    /// son rang est déduit par GameState.GetFinishRank.
    /// </summary>
    public sealed class FinishOrder
    {
        public static readonly FinishOrder Empty = new FinishOrder(new List<int>());

        private readonly List<int> _playerIds;

        /// <summary>Ids des joueurs terminés, du 1er au dernier à finir.</summary>
        public IReadOnlyList<int> PlayerIds => _playerIds;

        public int Count => _playerIds.Count;

        private FinishOrder(List<int> playerIds)
        {
            _playerIds = playerIds;
        }

        /// <summary>Retourne un nouvel ordre avec le joueur ajouté en dernier, ou this s'il est déjà présent.</summary>
        public FinishOrder Add(int playerId)
        {
            if (_playerIds.Contains(playerId)) return this;

            var updated = new List<int>(_playerIds) { playerId };
            return new FinishOrder(updated);
        }

        /// <summary>Rang à partir de 1, ou null si le joueur n'a pas fini.</summary>
        public int? GetRank(int playerId)
        {
            int index = _playerIds.IndexOf(playerId);
            return index < 0 ? (int?)null : index + 1;
        }
    }
}