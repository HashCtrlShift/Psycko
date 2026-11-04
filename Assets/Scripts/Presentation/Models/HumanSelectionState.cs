using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;

namespace Psycko.Presentation.Models
{
    /// <summary>
    /// État de sélection pur C# (zéro UnityEngine). Ne connaît pas la légalité selon l'état de jeu :
    /// seule la cohérence structurelle d'un Play est appliquée (Joker seul, cartes standard de même rang).
    /// </summary>
    public sealed class HumanSelectionState
    {
        private readonly List<Card> _cards = new List<Card>();

        /// <summary>Déclenché une seule fois par modification effective de la sélection.</summary>
        public event Action Changed;

        public IReadOnlyList<Card> Cards { get { return _cards.AsReadOnly(); } }
        public bool IsEmpty { get { return _cards.Count == 0; } }

        public bool Contains(Card card) { return card != null && _cards.Contains(card); }

        /// <summary>
        /// Clic sur une carte de la main.
        /// - Déjà sélectionnée : elle est retirée.
        /// - Sélection vide : la carte s'ajoute.
        /// - Même rang que la sélection (hors Joker) : s'ajoute, les précédentes restent.
        /// - Autre rang, ou Joker, ou sélection actuelle = Joker : remplace la sélection.
        /// Retourne true si la sélection a changé.
        /// </summary>
        public bool Toggle(Card card)
        {
            if (card == null) return false;

            if (_cards.Contains(card))
            {
                _cards.Remove(card);
            }
            else
            {
                if (_cards.Count > 0 && !CanJoinCurrentSelection(card))
                    _cards.Clear();
                _cards.Add(card);
            }

            RaiseChanged();
            return true;
        }

        /// <summary>Retire une carte de la sélection. Retourne true si elle y était.</summary>
        public bool Remove(Card card)
        {
            if (card == null || !_cards.Remove(card)) return false;
            RaiseChanged();
            return true;
        }

        /// <summary>Vide la sélection. Aucun événement si elle était déjà vide.</summary>
        public void Clear()
        {
            if (_cards.Count == 0) return;
            _cards.Clear();
            RaiseChanged();
        }

        /// <summary>
        /// Désélectionne les cartes absentes de la main courante ou non jouables (la main n'est jamais modifiée).
        /// Un seul événement Changed, uniquement si quelque chose a été retiré de la sélection.
        /// </summary>
        public void Prune(IEnumerable<Card> currentHand, Func<Card, bool> isPlayable)
        {
            var hand = new HashSet<Card>(currentHand ?? Enumerable.Empty<Card>());
            int removed = _cards.RemoveAll(c => !hand.Contains(c) || (isPlayable != null && !isPlayable(c)));
            if (removed > 0) RaiseChanged();
        }

        // Cohérence structurelle uniquement : jamais de règle de jeu ici.
        private bool CanJoinCurrentSelection(Card card)
        {
            Card first = _cards[0];
            if (card.IsJoker || first.IsJoker) return false;
            if (!card.Rank.HasValue || !first.Rank.HasValue) return false;
            return card.Rank.Value == first.Rank.Value;
        }

        private void RaiseChanged()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }
    }
}