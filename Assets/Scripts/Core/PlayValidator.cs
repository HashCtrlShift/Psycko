using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Validation de pose de carte selon la contrainte de hauteur active
    /// (Prêtre, ou hauteur libre, ou ≥ sommet). Aucune mutation d'état.
    /// </summary>
    public static class PlayValidator
    {
        /// <summary>
        /// Vérifie si une carte respecte la règle de hauteur active.
        /// Priorité : Prêtre actif (≤ 8) > Joker (toujours jouable) > hauteur libre > ≥ sommet.
        /// </summary>
        public static bool IsPlayable(
            Card card,
            Pile pile,
            bool isPriestActive,
            int priestHeightBlock)
        {
            // Prêtre actif : la contrainte s'applique AVANT tout,
            // sauf pour les Jokers qui restent toujours posables.
            if (isPriestActive)
            {
                if (card.IsJoker)
                    return true;

                return (int)card.Rank <= priestHeightBlock;
            }

            // Hors Prêtre : les Jokers sont toujours jouables
            if (card.IsJoker)
                return true;

            Card? effectiveTop = PileInspector.GetEffectiveTopCard(pile);

            // Pile vide (ou uniquement Jokers de Verre) => jouable
            if (effectiveTop == null)
                return true;

            Card top = effectiveTop.Value;

            // Référence = Joker Noir/Couleur => hauteur libre
            if (top.IsJoker)
                return true;

            return (int)card.Rank >= (int)top.Rank;
        }

        /// <summary>
        /// Une carte est "significative" pour la consommation de la contrainte Prêtre
        /// si elle n'est PAS un Joker de Verre.
        /// Le Joker de Verre est totalement transparent : il ne consomme pas la contrainte,
        /// qui reste donc active pour le joueur suivant.
        /// </summary>
        public static bool IsSignificantForPriest(Card card)
        {
            return !(card.IsJoker && card.JokerType == JokerType.Glass);
        }
    }
}