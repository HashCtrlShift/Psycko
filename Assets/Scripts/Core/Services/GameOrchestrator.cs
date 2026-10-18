using System;
using System.Linq;
using System.Collections.Generic;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;
using Psycko.Core.Rules.Detection;
using Psycko.Core.Rules.Validation;
using Psycko.Core.Services.Turn;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.Log;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Source unique d'exécution des intentions, notamment Step6 : le resolver
    /// calcule, cet orchestrateur appelle SetActivePlayer au plus une fois.
    /// Logging (T38b) : side-effect pur via IGameLogRecorder optionnel,
    /// aucune branche logique ne dépend du recorder.
    /// Effets du log (T38d) : GameLogEffectDetector lit l'état APRÈS pose du coup
    /// et AVANT destruction de la Pile (Carré relu sur Pile.Cards / Plays).
    /// Pile détruite (2, Bombe, Carré) : la contrainte est réinitialisée à
    /// (Normal, Three) — pile vide = aucune contrainte.
    /// Fin de joueur : Hand = 0 ET FaceDown = 0 (FaceUp déjà ramassé) → Finished,
    /// vérifiée à la sortie de chaque chemin qui retire des cartes (FinishIfEmpty).
    /// Doublon (log) : Step6 remet TurnResult.SkipNext à false une fois le tour avancé,
    /// le drapeau n'est donc plus exploitable. Le log recalcule la règle de Step5
    /// via IsDoublonApplied (Carré prime, plus de 2 joueurs actifs, PairDetection).
    /// Don du 7 : la ligne Play porte l'éventuel Doublon, la ligne GiftCard porte
    /// l'effet Don, et la ligne post-Don ne porte que le Carré.
    /// Proverbe (log) : As réel + changement de phase du poseur entre le début et la fin
    /// du coup (transition ou Finished). Calculé par comparaison de CurrentPhase avant/après.
    /// Valet (log) : SensReverse n'est loggé que si le sens a réellement changé ;
    /// le sens d'AVANT coup est lu sur l'état reçu en entrée et passé à Detect
    /// (previousDirection), car les états intermédiaires ne sont pas fiables pour ça.
    /// </summary>
    public sealed class GameOrchestrator
    {
        public PlayResult ApplyPlay(
            GameState state,
            Play play,
            int playerIndex,
            IGameLogRecorder recorder = null,
            GameResultTracker tracker = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (play == null) throw new ArgumentNullException(nameof(play));
            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (GameResultCalculator.IsGameOver(state))
                return PlayResult.Rejected(state, PlayRejectionReason.GameAlreadyOver);

            if (playerIndex != state.ActivePlayerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            if (state.Players[playerIndex].CurrentPhase == DefPhase.Finished)
                return PlayResult.Rejected(state, PlayRejectionReason.PlayerFinished);

            if (state.GetSeatIndex(play.PlayerId) != playerIndex)
                return PlayResult.Rejected(state, PlayRejectionReason.NotYourTurn);

            var beginTurn = TurnManager.BeginTurn(state);

            if (beginTurn.IsPickup)
            {
                var pickup = TurnManager.ResolvePickup(state, play.PlayerId);
                if (!pickup.IsAccepted)
                {
                    if (!pickup.RejectionReason.HasValue)
                        throw new InvalidOperationException(
                            "Invariant violé : un PickupResolution rejeté doit fournir une raison.");

                    return PlayResult.Rejected(state, pickup.RejectionReason.Value);
                }

                var seatIndex = state.GetSeatIndex(play.PlayerId);

                return ExecutePickup(
                    state,
                    seatIndex,
                    new List<int> { play.PlayerId },
                    ActionKind.PickupPile,
                    recorder,
                    tracker);
            }

            // Validation défensive (Bug L) : un coup injouable ne doit jamais être appliqué.
            // Le joueur a au moins une carte jouable (sinon ramassage forcé ci-dessus) ;
            // il doit donc proposer un coup légal.
            if (play.Cards.Count == 0
                || !play.Cards.All(c => CardPlayability.IsPlayable(c, state)))
                return PlayResult.Rejected(state, PlayRejectionReason.InvalidCards);

            // Phase du poseur AVANT le coup : sert au Proverbe (log).
            var phaseBefore = state.Players[playerIndex].CurrentPhase;

            // CHANGE : sens de jeu AVANT le coup, lu sur l'état reçu en entrée (log du Valet).
            var directionBefore = state.Direction;

            var turnResult = TurnManager.ApplyPlay(state, play);

            // GARDE CRITIQUE : 2 terminal → ramassage forcé, pas destruction/rejeu
            // CLAUDE.md § 2 Card : "Si c'est la dernière carte du joueur : le joueur pose son 2,
            // ramasse la pile et passe son tour (y compris le 2 qui vient d'être posé).
            // Pas de destruction, pas de rejeu."
            //
            // Cette garde s'applique AVANT la branche Don (Step3), donc:
            // - On court-circuite Step3→Step4→Step5→Step6
            // - On applique un ramassage forcé immédiat
            // - Aucun rejet (le coup est valide, il est simplement "consommé" par le ramassage)
            bool isLastCard = LastCardValidator.TerminatesOnTwo(play, state.Players[playerIndex], state);
            if (isLastCard)
            {
                var forced = TurnManager.ResolvePickup(state, play.PlayerId);
                if (!forced.IsAccepted)
                    throw new InvalidOperationException(
                        $"Bug: ramassage forcé (2 terminal) rejeté. Raison: {forced.RejectionReason}");

                var placed = turnResult.State;   // pose + reconstruction déjà appliquées

                recorder?.Record(GameLogEntry.PlayerAction(
                    play.PlayerId, ActionKind.Play, play.Cards, PileSnapshot(placed)));

                return ExecutePickup(placed, playerIndex, new List<int>(),
                    ActionKind.PickupPile, recorder, tracker);
            }

            // Don du 7 : le tour est suspendu après Step3. Aucune mutation n'est appliquée ici
            // (ni pioche, ni destruction, ni changement de joueur) : tout est rejoué par
            // ResolveGiftAndContinue via ResolveRemainder. Le coup est loggé tout de suite,
            // avec la pile telle qu'elle est à ce stade.
            // Le Doublon est loggé ici (une seule fois), par miroir de Step5 qui n'a pas
            // encore tourné : pas de Carré (le Carré prime), plus de 2 joueurs actifs, paire.
            // La destruction (Carré) n'est PAS loggée ici : elle l'est après le Don,
            // sur l'entrée post-Don (Bug 5 : doublon de log).
            // Pas de Proverbe ici : un 7 n'est jamais un As.
            if (turnResult.RequiresGiftResolution)
            {
                var giftPlayEffects = new List<EffectLogDetail>(1);

                if (IsDoublonApplied(turnResult.State, play))
                    giftPlayEffects.Add(new EffectLogDetail(EffectKind.Doublon));

                recorder?.Record(GameLogEntry.PlayerAction(
                    play.PlayerId,
                    ActionKind.Play,
                    play.Cards,
                    PileSnapshot(turnResult.State),
                    effects: giftPlayEffects.AsReadOnly()));

                tracker?.RecordPlay(playerIndex);

                return PlayResult.AwaitingGift(turnResult);
            }

            // État après pose, avant toute destruction : sert au détecteur du log.
            var stateAfterPlacement = turnResult.State;

            var newState = ApplyTurnIntents(
                turnResult.State,
                turnResult,
                state.ActivePlayerIndex);

            newState = FinishIfEmpty(newState, playerIndex);

            // Phase atteinte par le poseur avec ce coup (null si inchangée) : Proverbe + PhaseTransition.
            DefPhase? reachedPhase = newState.Players[playerIndex].CurrentPhase != phaseBefore
                ? newState.Players[playerIndex].CurrentPhase
                : (DefPhase?)null;

            recorder?.Record(GameLogEntry.PlayerAction(
                play.PlayerId,
                ActionKind.Play,
                play.Cards,
                PileSnapshot(newState),
                effects: GameLogEffectDetector.Detect(
                    play,
                    stateAfterPlacement,
                    turnResult.DestroysPile,
                    turnResult.NextDirection,
                    previousDirection: directionBefore,   // CHANGE
                    skipApplied: IsDoublonApplied(stateAfterPlacement, play),
                    reachedPhase: reachedPhase)));

            tracker?.RecordPlay(playerIndex);
            tracker?.ObserveState(newState);
            return Accept(newState, recorder, tracker);
        }

        /// <summary>
        /// Résout le Don obligatoire puis reprend la séquence :
        /// Don (TransferCard) → Step4 (re-pioche finale, exécutée ici sur l'état réel après Don)
        /// → Step5 (effets de pile) → Step6 (avancement de tour).
        /// La reconstruction Step2 a déjà été appliquée en amont par TurnManager.ApplyPlay.
        /// </summary>
        public PlayResult ResolveGiftAndContinue(
            GameState state,
            Play play,
            TurnResult pendingResult,
            GiftResolutionChoice choice,
            IGameLogRecorder recorder = null,
            GameResultTracker tracker = null)
        {
            if (!pendingResult.RequiresGiftResolution)
                throw new InvalidOperationException(
                    "ResolveGiftAndContinue requiert un TurnResult en attente de Don.");

            var donorIndex = pendingResult.State.ActivePlayerIndex;
            var donor = pendingResult.State.Players[donorIndex];

            if (!donor.Hand.Contains(choice.CardToGive))
                throw new InvalidOperationException(
                    $"Garde1: carte {choice.CardToGive} absente de la main du donneur Id={donor.Id} " +
                    $"(main: {string.Join(", ", donor.Hand)}) ; pending.ActivePlayerIndex={pendingResult.State.ActivePlayerIndex} ; " +
                    $"state.ActivePlayerIndex={state.ActivePlayerIndex} ; play.PlayerId={play.PlayerId}");

            int recipientSeatIndex;
            try
            {
                recipientSeatIndex = pendingResult.State.GetSeatIndex(choice.RecipientPlayerId);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Garde2: destinataire Id={choice.RecipientPlayerId} introuvable");
            }

            if (recipientSeatIndex == donorIndex)
                throw new InvalidOperationException(
                    $"Garde3: destinataire == donneur (Id={choice.RecipientPlayerId})");

            var recipient = pendingResult.State.Players[recipientSeatIndex];

            if (recipient.CurrentPhase == DefPhase.Finished)
                throw new InvalidOperationException(
                    $"Garde4: destinataire {recipient.Id} en phase Finished");

            // 1. Don exécuté
            var transferred = (GameState)((IGameStateCommand)pendingResult.State)
                .TransferCard(donorIndex, recipientSeatIndex, choice.CardToGive);

            // 2. Step4 : décision de re-pioche finale sur l'état RÉEL après Don,
            //    puis exécution immédiate (le Core décide, l'orchestrateur exécute).
            var step4 = Step4_FinalDrawResolver.Resolve(
                pendingResult.WithState(transferred),
                play);

            var afterGift = step4.State;
            var final = step4.FinalReconstruction ?? HandReconstructionResult.None;

            if (final.DrawCount > 0)
            {
                afterGift = (GameState)afterGift.DrawCards(
                    donorIndex,
                    Math.Min(final.DrawCount, afterGift.DrawPile.Count));
            }

            if (final.TriggersPhaseTransition)
            {
                if (final.TriggersFaceUpPickup)
                    afterGift = (GameState)afterGift.PickUpFaceUp(donorIndex);

                afterGift = (GameState)afterGift.AdvancePlayerPhase(donorIndex);
            }

            // Le donneur peut finir sur son Don (main vide, plus de FaceDown) : avant Step6.
            afterGift = FinishIfEmpty(afterGift, donorIndex);

            // 3. Reprise à Step5 → Step6 (Step4 déjà consommé : pas de double exécution)
            var remainder = TurnManager.ResolveFromStep5(
                pendingResult
                    .WithState(afterGift)
                    .WithFinalReconstruction(HandReconstructionResult.None),
                play);

            // État après Don, avant destruction : la Pile contient toujours le coup.
            var stateBeforeDestruction = remainder.State;

            var newState = ApplyTurnIntents(
                remainder.State,
                remainder,
                donorIndex);

            newState = FinishIfEmpty(newState, donorIndex);

            // Une seule ligne après le Play : la ligne GiftCard, écrite ICI (après calcul de la
            // suite du tour) pour porter Don + effets de suite (Carré, Proverbe, PhaseTransition
            // du donneur). Plus de 3e ligne Play : les cartes du coup ne sont loggées qu'une fois.
            var donorPhaseBefore = state.Players[donorIndex].CurrentPhase;
            DefPhase? donorReachedPhase = newState.Players[donorIndex].CurrentPhase != donorPhaseBefore
                ? newState.Players[donorIndex].CurrentPhase
                : (DefPhase?)null;

            var giftEffects = new List<EffectLogDetail> { new EffectLogDetail(EffectKind.Don) };
            giftEffects.AddRange(GameLogEffectDetector.Detect(
                play,
                stateBeforeDestruction,
                remainder.DestroysPile,
                remainder.NextDirection,
                previousDirection: state.Direction,   // CHANGE : sens avant coup (état reçu en entrée)
                includeDoublon: false,
                reachedPhase: donorReachedPhase));

            recorder?.Record(GameLogEntry.PlayerAction(
                donor.Id,
                ActionKind.GiftCard,
                new[] { choice.CardToGive },
                PileSnapshot(newState),
                targetPlayerId: choice.RecipientPlayerId,
                effects: giftEffects));

            return Accept(newState, recorder, tracker);
        }

        /// <summary>
        /// Révèle et résout une carte de la couche FaceDown en phase Luck.
        /// </summary>
        public BlindPlayResolution ApplyBlindPlay(
            GameState state,
            int playerIndex,
            int faceDownIndex,
            IGameLogRecorder recorder = null,
            GameResultTracker tracker = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (playerIndex < 0 || playerIndex >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (faceDownIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(faceDownIndex));

            if (GameResultCalculator.IsGameOver(state))
                throw new InvalidOperationException("GameAlreadyOver: impossible de jouer, la partie est terminée.");

            if (playerIndex != state.ActivePlayerIndex)
                throw new InvalidOperationException("NotYourTurn: ce n'est pas le tour de ce joueur.");

            var player = state.Players[playerIndex];

            if (player.CurrentPhase != DefPhase.Luck)
                throw new InvalidOperationException("InvalidCards: le joueur n'est pas en phase Luck.");

            if (player.Hand.Count != 0)
                throw new InvalidOperationException("InvalidCards: la main doit être vide en phase Luck.");

            if (faceDownIndex >= player.FaceDown.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(faceDownIndex),
                    $"InvalidCards: index FaceDown invalide. index={faceDownIndex}, " +
                    $"FaceDown.Count={player.FaceDown.Count}, playerIndex={playerIndex}, " +
                    $"playerId={player.Id}, phase={player.CurrentPhase}");

            var card = player.FaceDown[faceDownIndex];
            var play = Play.CreateSingle(player.Id, card, CardLayer.FaceDown);

            var playable = CardPlayability.IsPlayable(card, state);

            var terminatesOnTwo =
                play.EffectiveRank == DefRank.Two
                && player.FaceDown.Count == 1;

            // Phase du poseur AVANT le coup : sert au Proverbe (log).
            var phaseBefore = player.CurrentPhase;

            // CHANGE : sens de jeu AVANT le coup (log du Valet).
            var directionBefore = state.Direction;

            var revealedState = (GameState)state.PlayCards(play);

            // ⭐ 2 terminal OU carte non jouable → ramassage forcé
            // CLAUDE.md § 2 Card : "Si c'est la dernière carte du joueur : ramasse la pile et passe son tour."
            if (!playable || terminatesOnTwo)
            {
                // Entrée 1 : "[PlayerId] retourne [carte]" (sans effet).
                recorder?.Record(GameLogEntry.PlayerAction(
                    player.Id, ActionKind.BlindPlay, new[] { card }, PileSnapshot(revealedState)));

                // Entrée 2 : "[PlayerId] ramasse la Pile" (écrite par ExecutePickup).
                var pickupResult = ExecutePickup(
                    revealedState,
                    playerIndex,
                    new List<int>(),
                    ActionKind.PickupPile,
                    recorder,
                    tracker);

                return BlindPlayResolution.Of(pickupResult, card);
            }

            var effects = Step3_CardEffectsResolver.Resolve(revealedState, play);

            var pileEffects = Step5_PileEffectsResolver.Resolve(
                revealedState,
                play,
                effects.WithState(revealedState));

            // État après pose, avant destruction : sert au détecteur du log.
            var stateBeforeDestruction = pileEffects.State;

            var next = pileEffects.State;
            var destroysPile = effects.DestroysPile || pileEffects.DestroysPile;

            if (destroysPile)
            {
                next = (GameState)next.DestroyPile();

                // Pile vide = aucune contrainte (couvre 2, Bombe et Carré).
                next = (GameState)next.SetConstraint(
                    HeightConstraint.Normal,
                    DefRank.Three);
            }
            else if (effects.NextConstraint.HasValue
                    && effects.NextRefRank.HasValue)
            {
                next = (GameState)next.SetConstraint(
                    effects.NextConstraint.Value,
                    effects.NextRefRank.Value);
            }

            if (effects.NextDirection.HasValue)
            {
                next = (GameState)next.WithDirection(effects.NextDirection.Value);
            }

            // Fin de joueur AVANT Step6 : le siège suivant est calculé sans lui.
            next = FinishIfEmpty(next, playerIndex);

            // Phase atteinte par le poseur avec ce coup (null si inchangée) : Proverbe + PhaseTransition.
            DefPhase? reachedPhase = next.Players[playerIndex].CurrentPhase != phaseBefore
                ? next.Players[playerIndex].CurrentPhase
                : (DefPhase?)null;

            var advanceValid = Step6_AdvanceTurnResolver.Resolve(
                next,
                pileEffects.SkipNext,
                pileEffects.Replay);

            next = ExecuteActivePlayerIntent(next, advanceValid);

            // Entrée unique : "[PlayerId] retourne [carte]" + effets, pile après résolution.
            recorder?.Record(GameLogEntry.PlayerAction(
                player.Id,
                ActionKind.BlindPlay,
                new[] { card },
                PileSnapshot(next),
                effects: GameLogEffectDetector.Detect(
                    play,
                    stateBeforeDestruction,
                    destroysPile,
                    effects.NextDirection,
                    previousDirection: directionBefore,   // CHANGE
                    skipApplied: IsDoublonApplied(stateBeforeDestruction, play),
                    reachedPhase: reachedPhase)));

            tracker?.RecordPlay(playerIndex);
            return BlindPlayResolution.Of(Accept(next, recorder, tracker), card);
        }

        /// <summary>
        /// Ramassage volontaire de la pile (séquence T23 via ExecutePickup).
        /// </summary>
        public static PlayResult RequestPickup(
            GameState state,
            int playerId,
            IGameLogRecorder recorder = null,
            GameResultTracker tracker = null)
        {
            var pickup = TurnManager.ResolvePickup(state, playerId);
            if (!pickup.IsAccepted)
            {
                if (!pickup.RejectionReason.HasValue)
                    throw new InvalidOperationException(
                        "Invariant violé : un PickupResolution rejeté doit fournir une raison.");

                return PlayResult.Rejected(state, pickup.RejectionReason.Value);
            }

            var seatIndex = state.GetSeatIndex(playerId);

            return ExecutePickup(
                state,
                seatIndex,
                new List<int>(),
                ActionKind.RequestPickup,
                recorder,
                tracker);
        }

        /// <summary>
        /// Exécute, dans l'ordre strict, les intentions portées par un TurnResult :
        /// destruction de Pile (+ contrainte neutre) → contrainte → direction →
        /// joueur actif Step6.
        /// La reconstruction (Step2/Step4) est déjà appliquée en amont.
        /// Factorisée : partagée par ApplyPlay et ResolveGiftAndContinue.
        /// </summary>
        private static GameState ApplyTurnIntents(
            GameState startState,
            TurnResult result,
            int activeSeatIndex)
        {
            var newState = startState;

            if (result.DestroysPile)
            {
                newState = (GameState)newState.DestroyPile();
                newState = (GameState)newState.SetConstraint(HeightConstraint.Normal, DefRank.Three);
            }
            else if (result.NextConstraint.HasValue && result.NextRefRank.HasValue)
            {
                newState = (GameState)newState.SetConstraint(
                    result.NextConstraint.Value,
                    result.NextRefRank.Value);
            }

            if (result.NextDirection.HasValue)
                newState = (GameState)newState.WithDirection(result.NextDirection.Value);

            return ExecuteActivePlayerIntent(newState, result);
        }

        private static GameState ExecuteActivePlayerIntent(
            GameState state,
            TurnResult step6Result)
        {
            if (!step6Result.NextActivePlayerIndex.HasValue)
                return state;

            return (GameState)((IGameStateCommand)state).SetActivePlayer(
                step6Result.NextActivePlayerIndex.Value);
        }

        /// <summary>
        /// Passe le joueur en Finished s'il n'a plus aucune carte :
        /// Hand = 0 ET FaceUp = 0 ET FaceDown = 0 (règle : FaceUp est ramassé en main
        /// avant Talent, donc Hand = 0 et FaceDown = 0 suffit en pratique).
        /// No-op si le joueur est déjà Finished ou possède encore des cartes.
        /// </summary>
        private static GameState FinishIfEmpty(GameState state, int seatIndex)
        {
            var p = state.Players[seatIndex];
            if (p.CurrentPhase == DefPhase.Finished)
                return state;

            if (p.Hand.Count == 0 && p.FaceUp.Count == 0 && p.FaceDown.Count == 0)
                return (GameState)state.AdvancePlayerPhase(seatIndex);

            return state;
        }

        /// <summary>
        /// Séquence de ramassage T23 : PickUpPile → SetConstraint(Normal, Three)
        /// → fin de joueur éventuelle → Step6 → intention SetActivePlayer.
        /// La direction n'est jamais modifiée.
        /// </summary>
        private static PlayResult ExecutePickup(
            GameState state,
            int seatIndex,
            IReadOnlyList<int> forcedPickupPlayerIds,
            ActionKind logKind,
            IGameLogRecorder recorder,
            GameResultTracker tracker = null)
        {
            var pickedCards = PileSnapshot(state);

            var afterPickup = (GameState)state.PickUpPile(seatIndex);

            recorder?.Record(GameLogEntry.PlayerAction(
                state.Players[seatIndex].Id, logKind, pickedCards, PileSnapshot(afterPickup)));

            afterPickup = (GameState)afterPickup.SetConstraint(
                HeightConstraint.Normal,
                DefRank.Three);

            // Garde : un joueur sans aucune carte ne reste jamais dans une phase active
            // (cas d'une Pile vide ramassée en dernière FaceDown).
            afterPickup = FinishIfEmpty(afterPickup, seatIndex);

            var advance = Step6_AdvanceTurnResolver.Resolve(
                afterPickup,
                skipNext: false,
                replay: false);

            afterPickup = ExecuteActivePlayerIntent(afterPickup, advance);
            tracker?.RecordPickup(seatIndex);
            tracker?.ObserveState(afterPickup);

            var isGameOver = GameResultCalculator.IsGameOver(afterPickup);
            if (isGameOver)
                recorder?.Record(GameLogEntry.GameEnd(
                    GameResultCalculator.GetPsyckoPlayerId(afterPickup)));

            return PlayResult.Accepted(afterPickup, isGameOver, forcedPickupPlayerIds);
        }

        /// <summary>Construit le PlayResult accepté et logge la fin de partie si besoin.</summary>
        private static PlayResult Accept(
            GameState newState,
            IGameLogRecorder recorder,
            GameResultTracker tracker = null)
        {
            tracker?.ObserveState(newState);

            var isGameOver = GameResultCalculator.IsGameOver(newState);
            if (isGameOver)
                recorder?.Record(GameLogEntry.GameEnd(
                    GameResultCalculator.GetPsyckoPlayerId(newState)));

            return PlayResult.Accepted(newState, isGameOver);
        }

        /// <summary>
        /// Miroir de la règle Doublon de Step5, lu sur l'état APRÈS pose et AVANT
        /// destruction. Nécessaire car Step6 remet TurnResult.SkipNext à false une
        /// fois le tour avancé : le drapeau n'est plus exploitable par le log.
        /// Le Carré prime sur le Doublon ; pas de Doublon à 2 joueurs actifs ou moins.
        /// Le Doublon se détecte entre le Play et la Pile (PairDetection lit Pile.Plays
        /// et traverse les Jokers de Verre). Un Joker posé en dernier n'a pas de hauteur
        /// effective : PairDetection renvoie false, aucun filtre supplémentaire requis.
        /// </summary>
        private static bool IsDoublonApplied(GameState stateAfterPlacement, Play play)
            => !QuadDetection.IsQuadDetected(stateAfterPlacement)
               && HasMoreThanTwoActivePlayers(stateAfterPlacement)
               && PairDetection.IsPairDetected(stateAfterPlacement);

        /// <summary>
        /// Désactivation du Doublon à 2 joueurs actifs ou moins (miroir de Step5).
        /// </summary>
        private static bool HasMoreThanTwoActivePlayers(GameState state)
            => state.Players.Count(p => p.CurrentPhase != DefPhase.Finished) > 2;

        /// <summary>
        /// Copie figée des cartes de la pile centrale (pour le log uniquement).
        /// ⚠️ Seul endroit où le nom de la propriété pile est utilisé : adapter si besoin.
        /// </summary>
        private static IReadOnlyList<Card> PileSnapshot(GameState state)
            => state.Pile.Cards.ToList();
    }
}