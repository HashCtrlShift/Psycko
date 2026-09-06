using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Lecture pure de la Pile : cartes significatives, sommet effectif.
    /// Le Joker de Verre est transparent partout ici. Aucune mutation d'état.
    /// </summary>
    public static class PileInspector
    {
        /// <summary>
        /// Remonte la pile et retourne la première carte "significative" :
        /// - Joker de Verre : transparent, on continue sous lui.
        /// - Joker Noir / Couleur : significatif (hauteur libre).
        /// - Carte standard : significative.
        /// Retourne null si pile vide ou uniquement des Jokers de Verre.
        /// </summary>
        public static Card? GetEffectiveTopCard(Pile pile)
        {
            List<Card> significant = GetSignificantCardsFromTop(pile, 1);
            return significant.Count > 0 ? significant[0] : (Card?)null;
        }

        /// <summary>
        /// Remonte la pile et retourne les N premières cartes significatives.
        /// Joker de Verre ignoré/non compté. S'arrête sur un Joker Noir/Couleur
        /// (inclus dans la liste, mais casse la chaîne en dessous).
        /// </summary>
        public static List<Card> GetSignificantCardsFromTop(Pile pile, int count)
        {
            var result = new List<Card>();

            if (pile == null || pile.IsEmpty())
                return result;

            IReadOnlyList<Card> cards = pile.Cards;

            for (int i = cards.Count - 1; i >= 0 && result.Count < count; i--)
            {
                Card current = cards[i];

                if (current.IsJoker && current.JokerType == JokerType.Glass)
                    continue; // transparent

                result.Add(current);

                if (current.IsJoker)
                    break; // Noir/Couleur : casse la chaîne
            }

            return result;
        }

        /// <summary>
        /// Carré : 4 cartes significatives de même rang au sommet.
        /// Joker de Verre transparent, Joker Noir/Couleur casse la chaîne.
        /// </summary>
        public static bool DetectSquare(Pile pile)
        {
            if (pile == null || pile.Count < 4)
                return false;

            List<Card> significant = GetSignificantCardsFromTop(pile, 4);

            if (significant.Count < 4)
                return false;

            CardRank? referenceRank = null;

            foreach (Card current in significant)
            {
                if (current.IsJoker)
                    return false; // chaîne cassée

                if (referenceRank == null)
                    referenceRank = current.Rank;
                else if (current.Rank != referenceRank.Value)
                    return false;
            }

            return true;
        }
    }
}