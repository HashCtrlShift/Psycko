using Psycko.Presentation.Models;

namespace Psycko.Presentation.Logic
{
    /// <summary>Décide l’intention associée à un clic sur la pile sans appliquer de règle métier.</summary>
    public enum PileClickAction
    {
        Ignore,
        PlaySelection,
        Pickup
    }

    /// <summary>Routage pur C# d’un clic sur la pile. La légalité des actions reste vérifiée par le Core.</summary>
    public static class PileClickRouter
    {
        /// <summary>
        /// Priorité : Don ignoré, sélection envoyée au chemin Jouer, ramassage volontaire permis,
        /// sinon ignoré. Aucune décision de légalité de jeu n’est prise ici.
        /// </summary>
        public static PileClickAction Route(bool giftRequestPending, bool hasSelectedCards, bool voluntaryPickupAllowed)
        {
            if (giftRequestPending) return PileClickAction.Ignore;
            if (hasSelectedCards) return PileClickAction.PlaySelection;
            if (voluntaryPickupAllowed) return PileClickAction.Pickup;
            return PileClickAction.Ignore;
        }
    }
}
