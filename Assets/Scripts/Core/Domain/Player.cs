using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// État IMMUABLE d'un joueur : ses trois couches de cartes et sa phase courante.
    /// Aucune logique de jeu — les transitions de phase, les poses et les ramassages
    /// sont décidés dans Rules/ et Services/.
    /// </summary>
    public sealed class Player
    {
        private readonly List<Card> _hand;
        private readonly List<Card> _faceUp;
        private readonly List<Card> _faceDown;

        /// <summary>Identifiant stable du joueur (siège à la table).</summary>
        public int Id { get; }

        /// <summary>Nom affiché (humain ou bot).</summary>
        public string Name { get; }

        /// <summary>Couche 1 — Main, privée : visible du seul propriétaire.</summary>
        public IReadOnlyList<Card> Hand => _hand;

        /// <summary>Couche 2 — Face Découverte : visible de tous.</summary>
        public IReadOnlyList<Card> FaceUp => _faceUp;

        /// <summary>Couche 3 — Face Cachée : visible de personne, révélée une à une.</summary>
        public IReadOnlyList<Card> FaceDown => _faceDown;

        /// <summary>Phase à laquelle se trouve CE joueur (progression individuelle).</summary>
        public DefPhase CurrentPhase { get; }

        public bool HasCards
            => _hand.Count > 0 || _faceUp.Count > 0 || _faceDown.Count > 0;

        public int TotalCardCount
            => _hand.Count + _faceUp.Count + _faceDown.Count;

        private Player(
            int id,
            string name,
            List<Card> hand,
            List<Card> faceUp,
            List<Card> faceDown,
            DefPhase currentPhase)
        {
            Id = id;
            Name = name;
            _hand = hand;
            _faceUp = faceUp;
            _faceDown = faceDown;
            CurrentPhase = currentPhase;
        }

        /// <summary>
        /// Crée un joueur en début de partie : main + 3 Face Découverte + 3 Face Cachée,
        /// positionné en Phase 1 (Le Travail).
        /// </summary>
        public static Player Create(
            int id,
            string name,
            IEnumerable<Card> hand,
            IEnumerable<Card> faceUp,
            IEnumerable<Card> faceDown)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (hand is null) throw new ArgumentNullException(nameof(hand));
            if (faceUp is null) throw new ArgumentNullException(nameof(faceUp));
            if (faceDown is null) throw new ArgumentNullException(nameof(faceDown));

            var handList = hand.ToList();
            var faceUpList = faceUp.ToList();
            var faceDownList = faceDown.ToList();

            if (faceUpList.Count != 3)
                throw new ArgumentException($"FaceUp doit contenir exactement 3 cartes (reçu : {faceUpList.Count}).", nameof(faceUp));

            if (faceDownList.Count != 3)
                throw new ArgumentException($"FaceDown doit contenir exactement 3 cartes (reçu : {faceDownList.Count}).", nameof(faceDown));

            return new Player(
                id,
                name,
                handList,
                faceUpList,
                faceDownList,
                DefPhase.Work);
        }

        /// <summary>Retourne une copie du joueur avec une main remplacée.</summary>
        public Player WithHand(IEnumerable<Card> hand)
            => new Player(Id, Name, hand.ToList(), _faceUp, _faceDown, CurrentPhase);

        /// <summary>Retourne une copie du joueur avec une couche Face Découverte remplacée.</summary>
        public Player WithFaceUp(IEnumerable<Card> faceUp)
            => new Player(Id, Name, _hand, faceUp.ToList(), _faceDown, CurrentPhase);

        /// <summary>Retourne une copie du joueur avec une couche Face Cachée remplacée.</summary>
        public Player WithFaceDown(IEnumerable<Card> faceDown)
            => new Player(Id, Name, _hand, _faceUp, faceDown.ToList(), CurrentPhase);

        /// <summary>Retourne une copie du joueur positionné sur une autre phase.</summary>
        public Player WithPhase(DefPhase phase)
            => new Player(Id, Name, _hand, _faceUp, _faceDown, phase);
    }
}