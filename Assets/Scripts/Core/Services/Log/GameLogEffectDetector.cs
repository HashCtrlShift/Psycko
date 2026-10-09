using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Detection;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Traduit un coup en liste d'effets pour le log. Lecture seule : ne modifie
    /// aucun état et ne décide rien.
    /// Ordre : Carré / Doublon (exclusifs) d'abord, puis l'effet propre de la carte.
    ///   - Carré : jamais sur 2 / Joker Couleur / Joker Noir (D1), garanti par
    ///     QuadDetection qui s'arrête sur Noir/Couleur et par la destruction du 2.
    ///   - Doublon : exclu si un Carré est détecté (le Carré prime). Affiché seulement
    ///     si skipApplied est vrai (saut effectivement appliqué, jamais à 2 joueurs actifs).
    ///   - Doublon après Don (afterGift) : jamais redétecté, seul le Carré l'est.
    ///   - Don du 7 : aucun effet de carte (la carte transite de main en main).
    ///   - Prêtre et Valet ne coexistent jamais dans un Play (D2).
    /// </summary>
    internal static class GameLogEffectDetector
    {
        /// <param name="play">Coup joué.</param>
        /// <param name="stateAfterPlacement">
        /// État avec le coup DÉJÀ posé sur la Pile, AVANT toute destruction.
        /// Sert à relire Pile.Cards / Pile.Plays pour Carré et Doublon.
        /// </param>
        /// <param name="destroysPile">Intention de destruction (2, Bombe, Carré).</param>
        /// <param name="nextDirection">Sens de jeu après le coup (Valet : toujours inversé).</param>
        /// <param name="skipApplied">Le saut du joueur suivant est-il appliqué (Doublon effectif) ? Défaut : false.</param>
        /// <param name="afterGift">Vrai pour la ligne qui suit le Don : seul le Carré est retenu. Défaut : false.</param>
        public static IReadOnlyList<EffectLogDetail> Detect(
            Play play,
            IGameStateQuery stateAfterPlacement,
            bool destroysPile,
            PlayDirection? nextDirection,
            bool skipApplied = false,
            bool afterGift = false)
        {
            var effects = new List<EffectLogDetail>(2);

            bool isQuad = QuadDetection.IsQuadDetected(stateAfterPlacement);

            // Après un Don : seul le Carré (destruction) est loggé.
            if (afterGift)
            {
                if (isQuad)
                    effects.Add(new EffectLogDetail(EffectKind.DetruiteCarre));
                return effects.AsReadOnly();
            }

            // 1) Carré / Doublon d'abord (exclusifs : le Carré prime).
            if (isQuad)
            {
                effects.Add(new EffectLogDetail(EffectKind.DetruiteCarre));
            }
            else if (skipApplied
                     && !play.IsJokerPlay
                     && PairDetection.IsPairDetected(stateAfterPlacement))
            {
                effects.Add(new EffectLogDetail(EffectKind.Doublon));
            }

            // 2) Effet propre de la carte.
            if (play.IsJokerPlay)
            {
                switch (play.JokerType)
                {
                    case DefJokerType.Glass:
                        effects.Add(new EffectLogDetail(EffectKind.JokerVerre));
                        break;

                    case DefJokerType.Black:
                        effects.Add(new EffectLogDetail(EffectKind.JokerNoir));
                        break;

                    default:
                        // Joker Couleur/Bombe : détruit toujours la Pile.
                        if (destroysPile)
                            effects.Add(new EffectLogDetail(EffectKind.DetruiteBombe));
                        break;
                }
            }
            else
            {
                switch (play.EffectiveRank)
                {
                    case DefRank.Two:
                        // Le Carré a déjà été loggé si c'en est un ; un 2 ne peut
                        // pas former de Carré (la Pile est détruite dès le premier).
                        if (destroysPile)
                            effects.Add(new EffectLogDetail(EffectKind.Detruite2));
                        break;

                    case DefRank.Priest:
                        effects.Add(new EffectLogDetail(EffectKind.PriestEffect));
                        break;

                    case DefRank.Jack:
                        // Le Valet change toujours le sens de jeu.
                        effects.Add(new EffectLogDetail(
                            EffectKind.SensReverse,
                            DirectionLabel(nextDirection)));
                        break;

                    default:
                        // 7 (Don) et rangs ordinaires : aucun effet de carte.
                        break;
                }
            }

            return effects.AsReadOnly();
        }

        private static string DirectionLabel(PlayDirection? direction)
        {
            if (!direction.HasValue)
                return null;

            return direction.Value == PlayDirection.Clockwise
                ? "Horaire"
                : "Antihoraire";
        }
    }
}