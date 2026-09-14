using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Core.Domain
{
    /// <summary>
    /// Représente un coup joué : l'ensemble des cartes qui quittent la main
    /// (ou une couche) d'un joueur en une seule action.
    /// Un Play contient soit N cartes standard de MÊME hauteur, soit UN seul Joker.
    /// Jamais un mélange des deux.
    /// Ne porte aucune règle de légalité — c'est le rôle de CardPlayability.
    /// </summary>
    public sealed record Play
    {
        /// <summary>Cartes posées, dans l'ordre où elles ont été sélectionnées.</summary>
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>Identifiant du joueur auteur du coup.</summary>
        public int PlayerId { get; }

        /// <summary>
        /// Hauteur effective du coup. Null si le coup est un Joker
        /// (un Joker n'a pas de hauteur propre).
        /// </summary>
        public DefRank? EffectiveRank { get; }

        /// <summary>True si ce coup est un Joker joué seul.</summary>
        public bool IsJokerPlay => JokerType.HasValue;

        /// <summary>Type du Joker si IsJokerPlay, sinon null.</summary>
        public DefJokerType? JokerType { get; }

        /// <summary>Nombre de cartes posées dans ce coup.</summary>
        public int Count => Cards.Count;

        /// <summary>
        /// Couche d'origine des cartes de ce Play. Toujours Hand en Phase 1.
        /// Peut être FaceUp (Phase 2) ou FaceDown (Phase 3, révélation directe).
        /// </summary>
        public CardLayer SourceLayer { get; }

        private Play(
            IReadOnlyList<Card> cards,
            int playerId,
            DefRank? effectiveRank,
            DefJokerType? jokerType,
            CardLayer sourceLayer)
        {
            Cards = cards;
            PlayerId = playerId;
            EffectiveRank = effectiveRank;
            JokerType = jokerType;
            SourceLayer = sourceLayer;
        }

        /// <summary>
        /// Construit un coup à partir des cartes quittant une couche du joueur.
        /// Valide uniquement la COHÉRENCE STRUCTURELLE (pas la légalité de jeu) :
        /// non vide, pas de mélange Joker/standard, hauteur unique, un seul Joker.
        /// </summary>
        public static Play Create(
            int playerId,
            IEnumerable<Card> cards,
            CardLayer sourceLayer = CardLayer.Hand)
        {
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));

            var list = cards.ToList();

            if (list.Count == 0)
                throw new ArgumentException(
                    "Un Play ne peut pas être vide.", nameof(cards));

            int jokerCount = list.Count(c => c.IsJoker);

            // Cas Joker : obligatoirement seul.
            if (jokerCount > 0)
            {
                if (list.Count > 1)
                    throw new ArgumentException(
                        "Un Joker se joue toujours seul : mélange Joker/carte standard interdit.",
                        nameof(cards));

                var joker = list[0];

                return new Play(
                    cards: list,
                    playerId: playerId,
                    effectiveRank: null,
                    jokerType: joker.JokerType,
                    sourceLayer: sourceLayer);
            }

            // Cas standard : toutes les cartes doivent partager la même hauteur.
            var rank = list[0].Rank!.Value;

            if (list.Any(c => c.Rank!.Value != rank))
                throw new ArgumentException(
                    "Toutes les cartes d'un Play doivent avoir la même hauteur.",
                    nameof(cards));

            return new Play(
                cards: list,
                playerId: playerId,
                effectiveRank: rank,
                jokerType: null,
                sourceLayer: sourceLayer);
        }

        /// <summary>Raccourci pour un coup d'une seule carte.</summary>
        public static Play CreateSingle(
            int playerId,
            Card card,
            CardLayer sourceLayer = CardLayer.Hand)
            => Create(playerId, new[] { card }, sourceLayer);
    }
}