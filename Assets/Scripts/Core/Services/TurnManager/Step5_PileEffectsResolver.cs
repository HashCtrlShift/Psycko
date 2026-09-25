using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Rules.Detection;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Résout le sous-temps 5 de l'ordre strict d'un tour : les effets de la pile
    /// produits par le coup courant.
    ///
    /// <para>
    /// Le Carré est prioritaire sur le Doublon. Un Carré détruit la pile et peut
    /// accorder un rejeu au poseur ; un Doublon, lorsqu'il est applicable, saute
    /// le joueur suivant. Le Doublon est désactivé lorsqu'il reste au plus deux
    /// joueurs actifs, déterminés à partir de <see cref="DefPhase.Finished"/>,
    /// et non à partir du nombre brut d'entrées dans <c>Players</c>. Le Carré
    /// reste actif quel que soit le nombre de joueurs actifs. Si le poseur a
    /// vidé sa main en complétant le Carré, il ne rejoue pas : le joueur
    /// suivant reprend alors la main normalement.
    /// </para>
    ///
    /// <para>
    /// Ce resolver ne mute pas l'état. Il enrichit le <see cref="TurnResult"/>
    /// déjà produit par l'étape 3 afin de préserver toutes les décisions des
    /// étapes précédentes.
    /// </para>
    /// </summary>
    public static class Step5_PileEffectsResolver
    {
        /// <summary>
        /// Résout les effets Carré et Doublon du sous-temps 5, dans l'ordre
        /// strict Carré puis Doublon.
        /// </summary>
        /// <param name="state">État de jeu à analyser pour les effets de pile.</param>
        /// <param name="play">Coup dont le poseur peut bénéficier d'un rejeu.</param>
        /// <param name="incomingResult">
        /// Résultat entrant produit par l'étape 3. Il est requis pour fusionner
        /// explicitement le rejeu accordé par l'étape 3 (<c>GrantsReplay</c>)
        /// avec le rejeu éventuellement accordé par le Carré à l'étape 5.
        /// Sans cette fusion Step3→Step5, l'application séquentielle de
        /// <c>WithReplay</c> pourrait écraser silencieusement un rejeu déjà
        /// accordé ; la fusion est donc effectuée par OR logique, jamais par
        /// remplacement via <c>WithState</c>.
        /// </param>
        /// <returns>
        /// Le résultat entrant enrichi successivement avec DestroysPile,
        /// SkipNext et le rejeu final fusionné.
        /// </returns>
        public static TurnResult Resolve(GameState state, Play play, TurnResult incomingResult)
        {
            var quadDetected = QuadDetection.IsQuadDetected(state);
            var destroysPile = quadDetected;
            var skipNext = false;
            var step5Replay = false;

            if (quadDetected)
            {
                var poseur = state.Players.First(p => p.Id == play.PlayerId);
                step5Replay = poseur.HasCards;
            }
            else
            {
                var activePlayerCount = state.Players.Count(
                    p => p.CurrentPhase != DefPhase.Finished);

                if (activePlayerCount > 2 && PairDetection.IsPairDetected(state))
                {
                    skipNext = true;
                }
            }

            var finalReplay = incomingResult.GrantsReplay || step5Replay;

            return incomingResult
                .WithDestroysPile(destroysPile)
                .WithSkipNext(skipNext)
                .WithReplay(finalReplay);
        }
    }
}
