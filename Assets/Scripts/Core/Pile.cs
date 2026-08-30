using System;
using System.Collections.Generic;
using Psycko.Core;

namespace Psycko.Core
{

    /// <summary>
    /// Représente la pile de cartes jouées au centre de la table.
    /// Fonctionnement : LIFO (Last In, First Out) — la dernière carte jouée est au sommet.
    /// </summary>
    public class Pile
    {
        private readonly List<Card> cards;

        public Pile()
        {
            cards = new List<Card>();
        }

        /// <summary>
        /// Nombre de cartes actuellement dans la pile
        /// </summary>
        public int Count => cards.Count;

        /// <summary>
        /// Retourne toutes les cartes de la pile (copie, non-mutable)
        /// </summary>
        public IReadOnlyList<Card> Cards => cards.AsReadOnly();

        /// <summary>
        /// Ajoute une carte au sommet de la pile
        /// </summary>
        public void Add(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            cards.Add(card);
        }

        /// <summary>
        /// Retourne la carte au sommet de la pile sans la retirer
        /// </summary>
        public Card Top()
        {
            if (cards.Count == 0)
                throw new InvalidOperationException("Cannot get Top() from empty Pile");
            return cards[^1];
        }

        /// <summary>
        /// Retire et retourne la carte au sommet de la pile
        /// </summary>
        public Card Pop()
        {
            if (cards.Count == 0)
                throw new InvalidOperationException("Cannot Pop() from empty Pile");
            Card topCard = cards[^1];
            cards.RemoveAt(cards.Count - 1);
            return topCard;
        }

        /// <summary>
        /// Vide complètement la pile
        /// </summary>
        public void Clear()
        {
            cards.Clear();
        }

        /// <summary>
        /// Indique si la pile est vide
        /// </summary>
        public bool IsEmpty() => cards.Count == 0;

        public override string ToString()
        {
            if (IsEmpty())
                return "Pile(empty)";
            return $"Pile({Count} cards, Top={Top()})";
        }
    }
}