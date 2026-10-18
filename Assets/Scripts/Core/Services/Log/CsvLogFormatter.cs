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

        /// <summary>
        /// Formate les effets (noms seuls), séparés par " + " (ordre conservé).
        /// Les précisions (sens, phase) sont affichées dans la colonne Détail.
        /// </summary>
        public string FormatEffects(IEnumerable<EffectLogDetail> effects)
            => string.Join(" + ", (effects ?? Enumerable.Empty<EffectLogDetail>())
                .Select(e => e.Kind.ToString()));

        /// <summary>
        /// Formate la colonne Détail : le texte propre de l'entrée (raison de départ, etc.)
        /// puis les précisions des effets (sens de jeu, phase atteinte), séparés par " ; ".
        /// Chaîne vide s'il n'y a rien à afficher.
        /// </summary>
        public string FormatDetail(GameLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(entry.Detail))
                parts.Add(entry.Detail);

            foreach (var effect in entry.Effects)
            {
                if (string.IsNullOrEmpty(effect.Detail))
                    continue;

                switch (effect.Kind)
                {
                    case EffectKind.SensReverse:
                        parts.Add("Sens : " + effect.Detail);
                        break;
                    case EffectKind.PhaseTransition:
                        parts.Add("Phase : " + effect.Detail);
                        break;
                    default:
                        parts.Add(effect.Kind + " : " + effect.Detail);
                        break;
                }
            }

            return string.Join(" ; ", parts);
        }

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