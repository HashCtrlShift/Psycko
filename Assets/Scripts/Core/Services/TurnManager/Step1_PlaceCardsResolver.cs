using System;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Phase;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 1 — Pose des Cartes (PLACE CARDS).
    /// Valide que le coup proposé est structurellement et légalement jouable,
    /// puis retourne un TurnResult inchangé (aucune mutation d'état ici).
    /// La mutation réelle (removal des cartes de la couche source) relève de
    /// Step2_ReconstructionResolver ou de GameOrchestrator.
    /// </summary>
    public static class Step1_PlaceCardsResolver
    {
        private static readonly CardPlayabilityChecker _playabilityChecker = new CardPlayabilityChecker();

        /// <summary>
        /// Valide un coup joué. Ne mute jamais l'état.
        /// </summary>
        /// <param name="state">État courant de la partie (lecture seule).</param>
        /// <param name="play">Coup proposé par le joueur/bot.</param>
        /// <returns>TurnResult avec state inchangé. Lève exception si anomalie.</returns>
        public static TurnResult Resolve(GameState state, Play play)
        {
            if (play is null)
                throw new ArgumentNullException(nameof(play));

            if (state is null)
                throw new ArgumentNullException(nameof(state));

            // === Validation 1 : PlayerId correspond au joueur actif ===
            var activePlayer = state.GetActivePlayer();
            if (play.PlayerId != activePlayer.Id)
                throw new InvalidOperationException(
                    $"Seul le joueur actif (ID={activePlayer.Id}) peut jouer. " +
                    $"Le coup proposé vient du joueur ID={play.PlayerId}.");

            // === Validation 2 : Play non-vide ===
            if (play.Count < 1)
                throw new ArgumentException(
                    "Un coup doit contenir au moins une carte.", nameof(play));

            // === Validation 3 : Count ne dépasse pas 4 (max Carré V1) ===
            if (play.Count > 4)
                throw new ArgumentException(
                    $"Un coup ne peut contenir plus de 4 cartes (reçu : {play.Count}).", nameof(play));

            // === Validation 4 : Couche source est jouable pour cette phase ===
            var phaseResolver = GetPhaseResolver(activePlayer.CurrentPhase);
            if (!phaseResolver.IsLayerPlayable(activePlayer, play.SourceLayer))
                throw new InvalidOperationException(
                    $"La couche {play.SourceLayer} n'est pas jouable en phase {activePlayer.CurrentPhase}.");

            // === Validation 5 : Le joueur possède réellement toutes les cartes de ce coup ===
            var sourceCards = GetSourceCards(activePlayer, play.SourceLayer);
            foreach (var card in play.Cards)
            {
                if (!sourceCards.Contains(card))
                    throw new InvalidOperationException(
                        $"Le joueur n'a pas la carte {card} dans la couche {play.SourceLayer}.");
            }

            // === Validation 6 : Chaque carte est jouable (hauteur, contrainte) ===
            foreach (var card in play.Cards)
            {
                if (!CardPlayability.IsPlayable(card, state))
                    throw new InvalidOperationException(
                        $"La carte {card} n'est pas jouable : contrainte de hauteur non respectée.");
            }

            // === Validation 7 : Si Joker, doit être seul (Play.Create le garantit déjà, mais redondance OK) ===
            if (play.IsJokerPlay && play.Count > 1)
                throw new ArgumentException(
                    "Un Joker se joue toujours seul.", nameof(play));

            // === Pas d'anomalie : état inchangé, retour success ===
            return new TurnResult(state, skipNext: false, replay: false, isPickup: false);
        }

        /// <summary>
        /// Retourne le PhaseResolver correspondant à la phase du joueur.
        /// </summary>
        private static PhaseResolver GetPhaseResolver(DefPhase phase)
        {
            return phase switch
            {
                DefPhase.Work => new WorkPhaseResolver(),
                DefPhase.Talent => new TalentPhaseResolver(),
                DefPhase.Luck => new LuckPhaseResolver(),
                _ => throw new InvalidOperationException(
                    $"Pas de PhaseResolver pour la phase {phase}."),
            };
        }

        /// <summary>
        /// Retourne la couche source demandée du joueur (Hand, FaceUp, ou FaceDown).
        /// </summary>
        private static System.Collections.Generic.IReadOnlyList<Card> GetSourceCards(
            Player player,
            CardLayer sourceLayer)
        {
            return sourceLayer switch
            {
                CardLayer.Hand => player.Hand,
                CardLayer.FaceUp => player.FaceUp,
                CardLayer.FaceDown => player.FaceDown,
                _ => throw new ArgumentException(
                    $"Couche invalide : {sourceLayer}.", nameof(sourceLayer)),
            };
        }
    }
}