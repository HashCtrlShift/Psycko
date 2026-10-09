using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain.Log;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Services.Log
{
    /// <summary>Formate les entrées de log en textes lisibles en français.</summary>
    internal sealed class CsvLogFormatter
    {
        private readonly ICardFormatter _cards;

        public CsvLogFormatter(ICardFormatter cards)
        {
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        /// <summary>Formate une liste de cartes, séparées par un espace.</summary>
        public string FormatCards(IEnumerable<Card> cards)
            => string.Join(" ", (cards ?? Enumerable.Empty<Card>()).Select(c => _cards.Format(c)));

        /// <summary>Formate les effets, séparés par " + " (ordre conservé).</summary>
        public string FormatEffects(IEnumerable<EffectLogDetail> effects)
            => string.Join(" + ", effects ?? Enumerable.Empty<EffectLogDetail>());

        /// <summary>Formate une action en texte lisible.</summary>
        public string Format(GameLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            string P(int id) => "Joueur " + id;

            switch (entry.Kind)
            {
                case ActionKind.Play:
                    return P(entry.PlayerId) + " joue " + FormatCards(entry.Cards);
                case ActionKind.PickupPile:
                    return P(entry.PlayerId) + " ramasse la Pile";
                case ActionKind.RequestPickup:
                    return P(entry.PlayerId) + " ramasse volontairement la Pile";
                case ActionKind.GiftCard:
                    return P(entry.PlayerId) + " donne " + FormatCards(entry.Cards)
                        + " à " + P(entry.TargetPlayerId ?? -1);
                case ActionKind.BlindPlay:
                    return P(entry.PlayerId) + " retourne " + FormatCards(entry.Cards);
                case ActionKind.GameStart:
                    return "Premier joueur : " + P(entry.PlayerId);
                case ActionKind.PhaseChange:
                    return P(entry.PlayerId) + " change de phase";
                case ActionKind.GameEnd:
                    return entry.PlayerId < 0
                        ? "Fin de partie (erreur)"
                        : "Fin de partie, Psycko : " + P(entry.PlayerId);
                default:
                    return string.Empty;
            }
        }
    }
}