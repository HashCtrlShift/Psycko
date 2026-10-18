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
    /// puis le Proverbe, puis PhaseTransition (le Proverbe s'affiche avant la transition).
    ///   - Carré : jamais sur 2 / Joker Couleur / Joker Noir (D1), garanti par
    ///     QuadDetection qui s'arrête sur Noir/Couleur et par la destruction du 2.
    ///   - Doublon : exclu si un Carré est détecté (le Carré prime). Affiché uniquement
    ///     si skipApplied est vrai (décision officielle de Step5) ET includeDoublon est vrai.
    ///     Pour la continuation d'un Don, l'appelant passe includeDoublon = false :
    ///     le Doublon est déjà écrit sur la ligne Play.
    ///   - Don du 7 : l'effet Don est porté par la ligne GiftCard, loggée par
    ///     l'orchestrateur, qui y ajoute aussi les effets de la suite du tour.
    ///   - Prêtre et Valet ne coexistent jamais dans un Play (D2).
    ///   - Valet : SensReverse loggé uniquement si le sens a réellement changé
    ///     (le Valet est sans effet à 2 joueurs actifs ou moins).
    ///   - Proverbe : As réel posé qui déclenche une transition de phase du poseur
    ///     (ou sa fin de partie).
    ///   - PhaseTransition : Detail = phase atteinte (Talent / Luck / Finished).
    /// </summary>
    internal static class GameLogEffectDetector
    {
        /// <param name="play">Coup joué.</param>
        /// <param name="stateAfterPlacement">
        /// État avec le coup DÉJÀ posé sur la Pile, AVANT toute destruction et AVANT
        /// l'application de la nouvelle direction.
        /// Sert à relire Pile.Cards / Pile.Plays pour le Carré, et Direction (sens
        /// d'avant coup) pour détecter une inversion effective.
        /// </param>
        /// <param name="destroysPile">Intention de destruction (2, Bombe, Carré).</param>        
        /// <param name="nextDirection">
        /// Sens de jeu après le coup (le Valet l'inverse seulement à 3 joueurs actifs ou plus).
        /// </param>
        /// <param name="previousDirection">
        /// Sens de jeu AVANT le coup. Sert à détecter une inversion effective du Valet.
        /// Si null, l'inversion est considérée comme effective dès que nextDirection est renseigné.
        /// </param>
        /// <param name="skipApplied">Le saut du joueur suivant est-il appliqué (Doublon effectif) ? Défaut : false.</param>
        /// <param name="includeDoublon">Faux pour la continuation d'un Don (Doublon déjà loggé). Défaut : true.</param>
        /// <param name="reachedPhase">Phase atteinte par le poseur avec ce coup, null si inchangée. Défaut : null.</param>
        public static IReadOnlyList<EffectLogDetail> Detect(
            Play play,
            IGameStateQuery stateAfterPlacement,
            bool destroysPile,
            PlayDirection? nextDirection,
            PlayDirection? previousDirection = null,
            bool skipApplied = false,
            bool includeDoublon = true,
            DefPhase? reachedPhase = null)
        {
            var effects = new List<EffectLogDetail>(4);

            bool isQuad = QuadDetection.IsQuadDetected(stateAfterPlacement);

            // 1) Carré / Doublon d'abord (exclusifs : le Carré prime).
            if (isQuad)
            {
                effects.Add(new EffectLogDetail(EffectKind.DetruiteCarre));
            }
            else if (includeDoublon && skipApplied && !play.IsJokerPlay)
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
                        // Effet loggé seulement si le sens a réellement changé
                        // (inchangé à 2 joueurs actifs ou moins).
                        if (nextDirection.HasValue
                            && (!previousDirection.HasValue
                                || nextDirection.Value != previousDirection.Value))
                        {
                            effects.Add(new EffectLogDetail(
                                EffectKind.SensReverse,
                                DirectionLabel(nextDirection)));
                        }
                        break;

                    default:
                        // 7 (Don : effet porté par la ligne GiftCard) et rangs ordinaires.
                        break;
                }
            }

            // 3) Proverbe : As réel + transition de phase (ou fin) du poseur.
            if (ProverbDetection.IsProverb(play, reachedPhase.HasValue))
                effects.Add(new EffectLogDetail(EffectKind.Proverbe));

            // 4) Changement de phase du poseur (après le Proverbe).
            if (reachedPhase.HasValue)
                effects.Add(new EffectLogDetail(
                    EffectKind.PhaseTransition,
                    PhaseLabel(reachedPhase.Value)));

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

        private static string PhaseLabel(DefPhase phase)
        {
            switch (phase)
            {
                case DefPhase.Talent:   return "Talent";
                case DefPhase.Luck:     return "Luck";
                case DefPhase.Finished: return "Finished";
                default:                return phase.ToString();
            }
        }
    }
}