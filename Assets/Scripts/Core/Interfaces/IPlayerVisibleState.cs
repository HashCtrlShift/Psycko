#nullable enable

using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Vue filtrée et en lecture seule de l'état de la partie pour un joueur donné.
    /// Ne doit jamais exposer le contenu de la main ou des FaceDown adverses.
    /// </summary>
    public interface IPlayerVisibleState
    {
        int SelfPlayerId { get; }
        IReadOnlyList<Card> SelfHand { get; }
        IReadOnlyList<Card> SelfFaceUp { get; }
        IReadOnlyList<Card> SelfFaceDown { get; }
        IReadOnlyList<IOpponentVisibleInfo> Opponents { get; }
        Pile Pile { get; }
        int DrawPileCount { get; }
        int ActivePlayerIndex { get; }
        int GetActivePlayerId();
        PlayDirection Direction { get; }
        HeightConstraint Constraint { get; }
        DefRank RefRank { get; }

        /// <summary>
        /// Rang d'arrivée du joueur lui-même (1 = premier), ou null s'il n'a pas fini.
        /// Le dernier joueur (le Psycko) reçoit le dernier rang (4 à quatre joueurs)
        /// dès que tous les autres ont fini.
        /// </summary>
        int? SelfFinishRank { get; }
    }

    /// <summary>Informations filtrées visibles pour un joueur adverse.</summary>
    public interface IOpponentVisibleInfo
    {
        int PlayerId { get; }
        string Name { get; }
        IReadOnlyList<Card> FaceUp { get; }   // contenu complet, public
        int HandCount { get; }                 // compte seulement
        int FaceDownCount { get; }              // compte seulement
        DefPhase CurrentPhase { get; }

        /// <summary>
        /// Rang d'arrivée (1 = premier), ou null s'il n'a pas fini. Donnée publique.
        /// Le Psycko (dernier joueur) reçoit le dernier rang dès que tous les autres ont fini.
        /// </summary>
        int? FinishRank { get; }
    }
}