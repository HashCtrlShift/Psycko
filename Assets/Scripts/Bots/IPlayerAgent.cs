#nullable enable

using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Bots
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
        /// Propose une pose FaceDown (phase Luck). La carte est choisie à l'aveugle
        /// par position — son contenu n'est pas inspecté avant la décision.
        /// </summary>
        Play? ProposeFaceDownPlay(IPlayerVisibleState state);

        /// <summary>Choisit la résolution d'un Don (appelé si RequiresGiftResolution).</summary>
        GiftResolutionChoice ResolveGift(IPlayerVisibleState state);
    }
}