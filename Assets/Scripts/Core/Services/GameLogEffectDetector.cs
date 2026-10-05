using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Services.Turn;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Traduit le résultat d'un coup en liste d'effets pour le log.
    /// Lecture seule : ne modifie aucun état et ne décide rien.
    /// Ordre : JokerVerre, JokerNoir, Doublon, PriestEffect, SensReverse, Destruction.
    /// </summary>
    internal static class GameLogEffectDetector
    {
        public static IReadOnlyList<EffectLogDetail> Detect(
            Play play,
            bool destroysPile,
            HeightConstraint? nextConstraint,
            string nextDirection)
        {
            var effects = new List<EffectLogDetail>();
            var cards = play.Cards.ToList();

            if (cards.Any(c => IsJokerOfType(c, DefJokerType.Glass)))
                effects.Add(new EffectLogDetail(EffectKind.JokerVerre));

            // Joker Noir : loggué dès qu'il est joué, ne détruit jamais la pile.
            if (cards.Any(c => IsJokerOfType(c, DefJokerType.Black)))
                effects.Add(new EffectLogDetail(EffectKind.JokerNoir));

            var ranked = cards.Where(c => !IsAnyJoker(c)).ToList();
            if (ranked.Count == 2 && RankOf(ranked[0]) == RankOf(ranked[1]))
                effects.Add(new EffectLogDetail(EffectKind.Doublon));

            if (nextConstraint == HeightConstraint.PriestReversed)
                effects.Add(new EffectLogDetail(EffectKind.PriestEffect));

            if (nextDirection != null)
                effects.Add(new EffectLogDetail(EffectKind.SensReverse, nextDirection));

            if (destroysPile)
            {
                if (cards.Any(c => IsJokerOfType(c, DefJokerType.Color)))
                    effects.Add(new EffectLogDetail(EffectKind.DetruiteBombe));
                else if (play.EffectiveRank == DefRank.Two)
                    effects.Add(new EffectLogDetail(EffectKind.Detruite2));
                else
                    effects.Add(new EffectLogDetail(EffectKind.DetruiteCarre));
            }

            return effects.AsReadOnly();
        }

        // ⚠️ Seuls accès au modèle Card : adapter ici si vos noms diffèrent.
        private static bool IsAnyJoker(Card c) => c.IsJoker;
        private static bool IsJokerOfType(Card c, DefJokerType type) => c.IsJoker && c.JokerType == type;
        private static DefRank? RankOf(Card c) => c.Rank;
    }
}