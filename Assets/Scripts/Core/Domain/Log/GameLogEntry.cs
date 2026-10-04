#nullable enable
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Entrée immuable unique du log d'une partie.
    /// Représente soit une action de joueur, soit un marqueur d'événement.
    /// </summary>
    public sealed class GameLogEntry
    {
        public ActionKind ActionKind { get; }

        /// <summary>
        /// Joueur concerné par l'action. Null pour les marqueurs
        /// qui ne concernent pas un joueur précis (ex. DeckExhausted).
        /// </summary>
        public string? PlayerId { get; }

        /// <summary>
        /// Cible de l'action, uniquement pertinent pour GiftCard
        /// (joueur qui reçoit la carte). Null sinon.
        /// </summary>
        public string? TargetPlayerId { get; }

        /// <summary>
        /// Carte(s) jouée(s) dans cette action (une ou plusieurs, ex. carré).
        /// Vide/non pertinent pour les marqueurs d'événement.
        /// </summary>
        public IReadOnlyList<Card> PlayedCards { get; }

        /// <summary>
        /// Effet(s) déclenché(s) par l'action (ex. Carré + Destruction Pile).
        /// Vide si aucun effet.
        /// </summary>
        public IReadOnlyList<CardEffectType> Effects { get; }

        /// <summary>
        /// Snapshot complet de la pile après l'action. Vide si la pile est vide.
        /// </summary>
        public IReadOnlyList<Card> PileSnapshot { get; }

        /// <summary>
        /// Phase de départ, uniquement pour ActionKind.PhaseChange. Null sinon.
        /// </summary>
        public DefPhase? PhaseFrom { get; }

        /// <summary>
        /// Phase d'arrivée, uniquement pour ActionKind.PhaseChange. Null sinon.
        /// </summary>
        public DefPhase? PhaseTo { get; }

        /// <summary>
        /// Identifiant du joueur désigné Psycko, uniquement pour ActionKind.GameEnded. Null sinon.
        /// </summary>
        public string? PsyckoPlayerId { get; }

        public GameLogEntry(
            ActionKind actionKind,
            string? playerId = null,
            string? targetPlayerId = null,
            IReadOnlyList<Card>? playedCards = null,
            IReadOnlyList<CardEffectType>? effects = null,
            IReadOnlyList<Card>? pileSnapshot = null,
            DefPhase? phaseFrom = null,
            DefPhase? phaseTo = null,
            string? psyckoPlayerId = null)
        {
            ActionKind = actionKind;
            PlayerId = playerId;
            TargetPlayerId = targetPlayerId;
            PlayedCards = playedCards ?? System.Array.Empty<Card>();
            Effects = effects ?? System.Array.Empty<CardEffectType>();
            PileSnapshot = pileSnapshot ?? System.Array.Empty<Card>();
            PhaseFrom = phaseFrom;
            PhaseTo = phaseTo;
            PsyckoPlayerId = psyckoPlayerId;
        }
    }
}