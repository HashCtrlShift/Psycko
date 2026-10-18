using System;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Rules.Jokers;
using Psycko.Core.Rules.SpecialCards;

namespace Psycko.Core.Services.Turn
{
    public static class Step3_CardEffectsResolver
    {
        /// <summary>
        /// Étape 3 — Effets spéciaux. Appelée APRÈS Step2 (main reconstituée) et AVANT Step4.
        /// Interroge les handlers (contrat uniforme) et PRODUIT des intentions :
        /// contrainte, direction, destruction de pile, rejeu, et le drapeau RequiresGiftResolution.
        /// Aucune mutation, aucun choix de destinataire (→ GameOrchestrator).
        /// Le Don est un booléen : UN seul don par coup, quel que soit le nombre de 7 posés.
        /// </summary>
        public static TurnResult Resolve(GameState state, Play play)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));
            if (play.Count <= 0)
                throw new ArgumentException("Step3 reçoit un coup sans cartes.", nameof(play));

            (HeightConstraint, DefRank) constraint = (state.Constraint, state.RefRank);
            PlayDirection direction = state.Direction;
            bool destroysPile = false;
            bool grantsReplay = false;
            bool requiresGift = false;

            if (play.EffectiveRank is DefRank rank)
            {
                switch (rank)
                {
                    case DefRank.Seven:
                        constraint = SevenHandler.ResolveConstraint(state);
                        direction = SevenHandler.ResolveDirection(state);
                        destroysPile = SevenHandler.DestroysPile;
                        grantsReplay = SevenHandler.GrantsReplay;
                        requiresGift = SevenHandler.IsGiftTriggered(state, play);
                        break;
                    case DefRank.Two:
                        constraint = TwoHandler.ResolveConstraint(state);
                        direction = TwoHandler.ResolveDirection(state);
                        destroysPile = TwoHandler.DestroysPile;
                        grantsReplay = TwoHandler.GrantsReplay;
                        break;
                    case DefRank.Jack:
                        constraint = JackHandler.ResolveConstraint(state);
                        // À 2 joueurs actifs ou moins, inverser le sens ne change pas le
                        // joueur suivant : le Valet n'a alors aucun effet de direction
                        // (miroir de la règle du Doublon).
                        direction = JackHandler.ResolveDirection(state, CountActivePlayers(state));
                        destroysPile = JackHandler.DestroysPile;
                        grantsReplay = JackHandler.GrantsReplay;
                        break;
                    case DefRank.Priest:
                        constraint = PriestHandler.ResolveConstraint(state);
                        direction = PriestHandler.ResolveDirection(state);
                        destroysPile = PriestHandler.DestroysPile;
                        grantsReplay = PriestHandler.GrantsReplay;
                        break;
                    default:
                        // Carte ordinaire : elle devient la référence, contrainte normale.
                        constraint = (HeightConstraint.Normal, rank);
                        break;
                }
            }
            else if (play.JokerType is DefJokerType jokerType)
            {
                switch (jokerType)
                {
                    case DefJokerType.Glass:
                        // Transparence : délègue au résolveur (gère le cas « aucune hauteur dessous »).
                        constraint = GlassJokerResolver.ResolveConstraint(state);
                        break;
                    case DefJokerType.Black:
                        // Réinitialisation totale de la contrainte.
                        constraint = (HeightConstraint.Normal, DefRank.Three);
                        break;
                    case DefJokerType.Color:
                        // Bombe : détruit la Pile, le joueur suivant joue sur pile neuve.
                        destroysPile = true;
                        grantsReplay = false;
                        constraint = (HeightConstraint.Normal, DefRank.Three);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Type de Joker non géré en Step3 : {jokerType}");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    "Invariant violé : un Play n'a ni rang effectif ni type de Joker.");
            }

            return new TurnResult(state, skipNext: false, replay: false)
                .WithNextConstraint(constraint.Item1, constraint.Item2)
                .WithNextDirection(direction)
                .WithDestroysPile(destroysPile)
                .WithGrantsReplay(grantsReplay)
                .WithRequiresGiftResolution(requiresGift);
        }

        /// <summary>
        /// Nombre de joueurs encore actifs (phase différente de Finished).
        /// Évalué avant FinishIfEmpty : le poseur compte encore comme actif.
        /// </summary>
        private static int CountActivePlayers(GameState state)
            => state.Players.Count(p => p.CurrentPhase != DefPhase.Finished);
    }
}