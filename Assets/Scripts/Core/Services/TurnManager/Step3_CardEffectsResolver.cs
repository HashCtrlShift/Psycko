using System;
using Psycko.Core.Domain;
using Psycko.Core.Rules.SpecialCards;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 3 — Effets spéciaux. Appelée APRÈS Step2 (main reconstituée) et AVANT Step4.
    /// Interroge les handlers (contrat uniforme) et PRODUIT des intentions :
    /// contrainte, direction, destruction de pile, rejeu, et le drapeau RequiresGiftResolution.
    /// Aucune mutation, aucun choix de destinataire (→ GameOrchestrator).
    /// Le Don est un booléen : UN seul don par coup, quel que soit le nombre de 7 posés.
    /// </summary>
    public static class Step3_CardEffectsResolver
    {
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

            switch (play.EffectiveRank)
            {
                case DefRank.Seven:
                    constraint = SevenHandler.ResolveConstraint(state);
                    direction = SevenHandler.ResolveDirection(state);
                    destroysPile = SevenHandler.DestroysPile;
                    grantsReplay = SevenHandler.GrantsReplay;
                    break;
                case DefRank.Two:
                    constraint = TwoHandler.ResolveConstraint(state);
                    direction = TwoHandler.ResolveDirection(state);
                    destroysPile = TwoHandler.DestroysPile;
                    grantsReplay = TwoHandler.GrantsReplay;
                    break;
                case DefRank.Jack:
                    constraint = JackHandler.ResolveConstraint(state);
                    direction = JackHandler.ResolveDirection(state);
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
                    break; // rang non spécial : intentions neutres (état courant conservé)
            }

            bool requiresGift = SevenHandler.IsGiftTriggered(state, play);

            return new TurnResult(state, skipNext: false, replay: false)
                .WithNextConstraint(constraint.Item1, constraint.Item2)
                .WithNextDirection(direction)
                .WithDestroysPile(destroysPile)
                .WithGrantsReplay(grantsReplay)
                .WithRequiresGiftResolution(requiresGift);
        }
    }
}