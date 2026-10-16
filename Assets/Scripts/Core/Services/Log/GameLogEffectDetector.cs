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
    /// Ordre : Carré / Doublon (exclusifs) d'abord, puis l'effet propre de la carte,
    /// puis le Proverbe.
    ///   - Carré : jamais sur 2 / Joker Couleur / Joker Noir (D1), garanti par
    ///     QuadDetection qui s'arrête sur Noir/Couleur et par la destruction du 2.
    ///   - Doublon : exclu si un Carré est détecté (le Carré prime). Affiché uniquement
    ///     si skipApplied est vrai : c'est la décision officielle de Step5 (saut
    ///     effectivement appliqué, jamais à 2 joueurs actifs). Aucune redétection
    ///     par PairDetection ici, pour ne jamais diverger de Step5.
    ///   - Doublon après Don (afterGift) : jamais redétecté, seul le Carré l'est.
    ///   - Don du 7 : l'effet Don est porté par la ligne GiftCard, loggée par
    ///     l'orchestrateur (ResolveGiftAndContinue). Le 7 n'a aucun effet de carte
    ///     ici (la carte transite de main en main).
    ///   - Prêtre et Valet ne coexistent jamais dans un Play (D2).
    ///   - Proverbe : As réel posé qui déclenche une transition de phase du poseur
    ///     (ou sa fin de partie). Décision fournie par l'appelant (phaseTransition).
    /// </summary>
    internal static class GameLogEffectDetector
    {
        /// <param name="play">Coup joué.</param>
        /// <param name="stateAfterPlacement">
        /// État avec le coup DÉJÀ posé sur la Pile, AVANT toute destruction.
        /// Sert à relire Pile.Cards / Pile.Plays pour le Carré.
        /// </param>
        /// <param name="destroysPile">Intention de destruction (2, Bombe, Carré).</param>
        /// <param name="nextDirection">Sens de jeu après le coup (Valet : toujours inversé).</param>
        /// <param name="skipApplied">Le saut du joueur suivant est-il appliqué (Doublon effectif) ? Défaut : false.</param>
        /// <param name="afterGift">Vrai pour la ligne qui suit le Don : seul le Carré est retenu. Défaut : false.</param>
        /// <param name="phaseTransition">Le poseur a-t-il changé de phase avec ce coup (ou fini) ? Défaut : false.</param>
        public static IReadOnlyList<EffectLogDetail> Detect(
            Play play,
            IGameStateQuery stateAfterPlacement,
            bool destroysPile,
            PlayDirection? nextDirection,
            bool skipApplied = false,
            bool afterGift = false,
            bool phaseTransition = false)
        {
            var effects = new List<EffectLogDetail>(3);

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
            else if (skipApplied && !play.IsJokerPlay)
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
                        // 7 (Don : effet porté par la ligne GiftCard) et rangs ordinaires.
                        break;
                }
            }

            // 3) Proverbe : As réel + transition de phase (ou fin) du poseur.
            if (ProverbDetection.IsProverb(play, phaseTransition))
                effects.Add(new EffectLogDetail(EffectKind.Proverbe));

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