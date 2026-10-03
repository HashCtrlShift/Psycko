using Psycko.Core.Domain;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Contrat de vérification de jouabilité, consommé par TurnManager (validation d'un
    /// coup précis) et par Presentation/Bots (détection d'au moins une carte jouable,
    /// pour surbrillance UI ou passage de tour forcé).
    /// </summary>
    public interface ICardPlayabilityChecker
    {
        /// <summary>Une carte précise est-elle jouable dans l'état courant ?</summary>
        bool IsPlayable(Card card, IGameStateQuery state);

        /// <summary>Ce joueur a-t-il au moins une carte jouable, toutes couches confondues, dans l'état courant ?</summary>
        bool HasAnyPlayableCard(Player player, IGameStateQuery state);

        /// <summary>
        /// Une carte précise est-elle jouable, évaluée depuis une vue filtrée
        /// (Bots/Presentation). Applique strictement la même règle que la
        /// surcharge IGameStateQuery : la jouabilité d'une carte connue ne
        /// dépend jamais des cartes cachées d'autrui (Pile, Constraint, RefRank,
        /// Direction suffisent dans les deux cas).
        /// </summary>
        bool IsPlayable(Card card, IPlayerVisibleState state);

        /// <summary>
        /// Le joueur représenté par cette vue filtrée a-t-il au moins une carte
        /// jouable, toutes couches confondues (Hand, puis FaceUp si Hand vide) ?
        /// </summary>
        bool HasAnyPlayableCard(IPlayerVisibleState state);
    }
}