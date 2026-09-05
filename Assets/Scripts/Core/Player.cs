using System;
using System.Collections.Generic;
using System.Linq;
namespace Psycko
{
    public class Player
    {
        public string Id { get; }
        public string Name { get; }
        public List<Card> Hand { get; }
        public List<Card> FaceUp { get; }
        public List<Card> FaceDown { get; }
        public PowerCard? AssignedPowerCard { get; set; }
        public GamePhase CurrentPhase { get; set; }
        public bool HasWon { get; set; }
        public bool IsReady { get; set; }

        public Player(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Player ID cannot be null or empty", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Player name cannot be null or empty", nameof(name));

            Id = id;
            Name = name;
            Hand = new List<Card>();
            FaceUp = new List<Card>();
            FaceDown = new List<Card>();
            AssignedPowerCard = null;
            CurrentPhase = GamePhase.Travail;
            HasWon = false;
            IsReady = false;
        }

        /// <summary>
        /// Retourne le nombre total de cartes du joueur (main + face-up + face-down)
        /// </summary>
        public int TotalCards => Hand.Count + FaceUp.Count + FaceDown.Count;

        /// <summary>
        /// Ajoute une carte à la main
        /// </summary>
        public void AddCardToHand(Card card)
        {
            Hand.Add(card);
        }

        /// <summary>
        /// Ajoute une carte face-up
        /// </summary>
        public void AddCardFaceUp(Card card)
        {
            FaceUp.Add(card);
        }

        /// <summary>
        /// Ajoute une carte face-down
        /// </summary>
        public void AddCardFaceDown(Card card)
        {
            FaceDown.Add(card);
        }

        /// <summary>
        /// Retire une carte de la main
        /// </summary>
        public bool RemoveCardFromHand(Card card)
        {
            return Hand.Remove(card);
        }

        /// <summary>
        /// Retire une carte face-up
        /// </summary>
        public bool RemoveCardFaceUp(Card card)
        {
            return FaceUp.Remove(card);
        }

        /// <summary>
        /// Retire une carte face-down
        /// </summary>
        public bool RemoveCardFaceDown(Card card)
        {
            return FaceDown.Remove(card);
        }

        public override string ToString()
        {
            string powerCardStr = AssignedPowerCard.HasValue ? AssignedPowerCard.Value.ToString() : "None";
            return $"Player({Id}, {Name}, Hand={Hand.Count}, FaceUp={FaceUp.Count}, FaceDown={FaceDown.Count}, PowerCard={powerCardStr})";
        }

        public override bool Equals(object obj) => obj is Player other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
