using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;

namespace Psycko.Presentation.Models
{
    /// <summary>État de sélection pur C# ; ne connaît ni Unity ni la légalité selon l'état de jeu.</summary>
    public sealed class HumanSelectionState
    {
        private readonly List<Card> _cards = new List<Card>();
        public IReadOnlyList<Card> Cards { get { return _cards.AsReadOnly(); } }
        public bool IsEmpty { get { return _cards.Count == 0; } }

        /// <summary>Ajoute/retire une carte si elle respecte la cohérence structurelle d'un Play.</summary>
        public bool Toggle(Card card)
        {
            if (card == null) return false;
            if (_cards.Contains(card)) { _cards.Remove(card); return true; }
            if (_cards.Count != 0)
            {
                // Tout Joker doit être joué seul ; les cartes standard doivent partager leur rang.
                if (card.IsJoker || _cards[0].IsJoker) return false;
                if (!card.Rank.HasValue || !_cards[0].Rank.HasValue || card.Rank.Value != _cards[0].Rank.Value) return false;
            }
            _cards.Add(card);
            return true;
        }

        public bool Remove(Card card) { return card != null && _cards.Remove(card); }
        public void Clear() { _cards.Clear(); }

        /// <summary>Retire les cartes qui ne sont plus dans la main courante ou plus jouables.</summary>
        public void Prune(IEnumerable<Card> currentHand, System.Func<Card, bool> isPlayable)
        {
            var hand = new HashSet<Card>(currentHand ?? Enumerable.Empty<Card>());
            _cards.RemoveAll(card => !hand.Contains(card) || (isPlayable != null && !isPlayable(card)));
        }
    }
}