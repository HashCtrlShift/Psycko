using System;
using System.Collections.Generic;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Représente le paquet de 63 cartes (60 standards + 3 Jokers uniques).
    /// Aucune notion de main/pile/joueur — uniquement la constitution et le mélange du paquet.
    /// </summary>
    public sealed class Deck
    {
        private readonly List<Card> _cards;

        public IReadOnlyList<Card> Cards => _cards;

        private Deck(List<Card> cards)
        {
            _cards = cards;
        }

        /// <summary>
        /// Crée le paquet complet : 4 couleurs x 15 rangs (60 cartes) + 3 Jokers uniques.
        /// Ordre non mélangé (ordre de création déterministe).
        /// </summary>
        public static Deck CreateFull()
        {
            var cards = new List<Card>(63);

            foreach (DefSuit suit in Enum.GetValues(typeof(DefSuit)))
            {
                foreach (DefRank rank in Enum.GetValues(typeof(DefRank)))
                {
                    cards.Add(Card.CreateStandard(suit, rank));
                }
            }

            cards.Add(Card.CreateJoker(DefJokerType.Glass));
            cards.Add(Card.CreateJoker(DefJokerType.Black));
            cards.Add(Card.CreateJoker(DefJokerType.Color));

            return new Deck(cards);
        }

        /// <summary>
        /// Mélange le paquet selon l'ordre produit par le Random fourni.
        /// Le Random doit être créé via GameSeed.CreateRandom(seed) par l'appelant
        /// (Console ou GameOrchestrator) — Deck n'a aucune notion de seed,
        /// aucune création de Random en interne.
        /// </summary>
        public void Shuffle(Random random)
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }
    }
}