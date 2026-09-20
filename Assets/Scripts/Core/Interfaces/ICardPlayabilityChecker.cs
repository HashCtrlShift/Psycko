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
    }
}