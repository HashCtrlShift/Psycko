using System;
using System.Collections.Generic;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Pile de jeu centrale : conteneur ordonné et IMMUABLE des coups posés.
    /// Maintient deux vues cohérentes par construction :
    ///   - Plays : historique des coups (qui a joué quoi, en combien de cartes)
    ///   - Cards : liste plate ordonnée de toutes les cartes (comptage de hauteur,
    ///             détection de Carré traversant les Jokers de Verre)
    /// Les deux listes ne sont écrites qu'au moment du Add() : aucune
    /// désynchronisation possible puisque la pile est immuable.
    /// Ne porte AUCUNE règle : Carré, Doublon, transparence du Verre et reset
    /// sont résolus dans Rules/ (QuadDetection, PairDetection, GlassJokerResolver,
    /// BlackJokerResolver).
    /// </summary>
    public sealed class Pile
    {
        private readonly List<Play> _plays;
        private readonly List<Card> _cards;

        /// <summary>Coups posés, du plus ancien au plus récent.</summary>
        public IReadOnlyList<Play> Plays => _plays;

        /// <summary>
        /// Toutes les cartes de la pile, aplaties dans l'ordre de pose.
        /// Pré-calculée : accès O(1), aucune allocation à la lecture.
        /// </summary>
        public IReadOnlyList<Card> Cards => _cards;

        /// <summary>Pile vide (nouvelle pile ouverte).</summary>
        public static Pile Empty { get; } = new Pile(new List<Play>(), new List<Card>());

        private Pile(List<Play> plays, List<Card> cards)
        {
            _plays = plays;
            _cards = cards;
        }

        /// <summary>Nombre total de cartes dans la pile (à ramasser le cas échéant).</summary>
        public int Count => _cards.Count;

        /// <summary>Nombre de coups posés.</summary>
        public int PlayCount => _plays.Count;

        public bool IsEmpty => _plays.Count == 0;

        /// <summary>Dernier coup posé, null si la pile est vide.</summary>
        public Play LastPlay
            => _plays.Count == 0 ? null : _plays[^1];

        /// <summary>
        /// Dernière carte posée, null si la pile est vide.
        /// ATTENTION : brut. Si c'est un Joker de Verre, ce n'est PAS
        /// la hauteur de référence — voir GlassJokerResolver.
        /// </summary>
        public Card TopCard
            => _cards.Count == 0 ? null : _cards[^1];

        /// <summary>
        /// Ajoute un coup et retourne une NOUVELLE pile.
        /// L'instance courante reste inchangée (immuabilité Domain).
        /// </summary>
        public Pile Add(Play play)
        {
            if (play is null)
                throw new ArgumentNullException(nameof(play));

            var nextPlays = new List<Play>(_plays.Count + 1);
            nextPlays.AddRange(_plays);
            nextPlays.Add(play);

            var nextCards = new List<Card>(_cards.Count + play.Count);
            nextCards.AddRange(_cards);
            nextCards.AddRange(play.Cards);

            return new Pile(nextPlays, nextCards);
        }

        /// <summary>
        /// Retourne une pile vide : destruction de la pile
        /// (Carré, Joker Couleur/Bombe, 2).
        /// </summary>
        public Pile Cleared() => Empty;
    }
}