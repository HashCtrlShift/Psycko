#nullable enable

using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Exchange;

namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Contrat de décision d'un agent joueur. Toute intention retournée est
    /// revalidée par Core (CardPlayability, GameOrchestrator) — jamais garantie
    /// acceptée. Play? == null signifie "aucun coup proposé" ; le fallback est
    /// géré par l'appelant (GameOrchestrator), jamais par l'agent.
    /// </summary>
    public interface IPlayerAgent
    {
        /// <summary>Propose une pose normale (phases Work/Talent).</summary>
        Play? ProposeNormalPlay(IPlayerVisibleState state);

        /// <summary>Décide d'accepter ou non un pickup (forcé ou volontaire).</summary>
        bool DecidePickup(IPlayerVisibleState state, bool isForced);

        /// <summary>
        /// Choisit, à l'aveugle, la position (0 à SelfFaceDownCount - 1) de la carte FaceDown
        /// à retourner (phase Luck). La vue ne donne que le nombre de cartes : aucun contenu
        /// n'est accessible. Un index null ou hors bornes est une erreur de l'agent, rejetée
        /// par l'appelant (aucun repli silencieux).
        /// </summary>
        int? ProposeFaceDownIndex(IFaceDownBlindView view);

        /// <summary>Choisit la résolution d'un Don (appelé si RequiresGiftResolution).</summary>
        GiftResolutionChoice ResolveGift(IPlayerVisibleState state);

        /// <summary>
        /// Propose, pendant l'échange pré-partie, une liste d'échanges Main↔FaceUp à appliquer
        /// dans l'ordre. Chacun est revalidé par Core (PreGameExchange). Liste vide = aucun échange.
        /// </summary>
        IReadOnlyList<ExchangeChoice> ProposeExchanges(IPlayerVisibleState state);
    }
}