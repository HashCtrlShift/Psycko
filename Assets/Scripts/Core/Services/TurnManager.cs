using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;
using Psycko.Core.Rules.Detection;
using Psycko.Core.Rules.Jokers;
using Psycko.Core.Rules.Phase;
using Psycko.Core.Rules.SpecialCards;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services
{
    /// <summary>
    /// Résultat d'un appel à TurnManager : soit l'état est complet et le tour a
    /// progressé, soit l'exécution est SUSPENDUE en attente d'un choix du joueur
    /// (Don du 7), et l'appelant doit fournir ce choix via ResolveGift().
    ///
    /// Motif : l'Ordre Strict impose que le Don soit résolu à l'étape 3, APRÈS
    /// reconstitution de la main — donc après qu'ApplyPlay ait déjà commencé.
    /// La carte donnée peut être une carte piochée à l'étape 2, inconnue du joueur
    /// au moment de composer son Play. Le choix ne peut donc pas voyager dans Play.
    /// </summary>
    public enum TurnStatus
    {
        /// <summary>Les 6 étapes ont été exécutées. State est définitif.</summary>
        Complete = 0,

        /// <summary>
        /// Étapes 1-3 exécutées. Un Don est dû : le poseur doit choisir une carte
        /// de sa main (état final, post-reconstitution) et un destinataire.
        /// Appeler ResolveGift(result, card, recipientId) pour reprendre aux étapes 4-6.
        /// </summary>
        PendingGift = 1
    }

    /// <summary>
    /// Résultat immuable d'une étape d'exécution de tour.
    /// </summary>
    public sealed record TurnResult
    {
        /// <summary>État du jeu à l'issue des étapes exécutées.</summary>
        public GameState State { get; init; }

        /// <summary>Complete, ou PendingGift si un choix de Don est attendu.</summary>
        public TurnStatus Status { get; init; }

        /// <summary>
        /// Coup en cours de résolution. Conservé uniquement si Status == PendingGift,
        /// pour que ResolveGift() puisse reprendre les étapes 4-6 avec le contexte.
        /// Null si Complete.
        /// </summary>
        public Play SuspendedPlay { get; init; }

        /// <summary>
        /// Cartes parmi lesquelles le poseur doit choisir son Don
        /// (= sa main dans son état final). Vide si Complete.
        /// </summary>
        public IReadOnlyList<Card> GiftCandidates { get; init; } = Array.Empty<Card>();

        /// <summary>
        /// Index des joueurs pouvant recevoir le Don : tous les adversaires encore
        /// en jeu (phase != Finished). Vide si Complete.
        /// </summary>
        public IReadOnlyList<int> GiftRecipients { get; init; } = Array.Empty<int>();

        internal static TurnResult Completed(GameState state)
            => new TurnResult { State = state, Status = TurnStatus.Complete };
    }

    /// <summary>
    /// Exécute l'Ordre Strict d'Application des Effets (CLAUDE.md) pour un coup.
    ///
    /// SEUL service du Core autorisé à faire progresser l'état du jeu. Les couches
    /// Rules (PhaseResolver, SpecialCards, Jokers, Detection) sont purement
    /// déclaratives et en lecture seule : TurnManager les INTERROGE et applique
    /// leurs verdicts en construisant de nouvelles instances immuables.
    ///
    /// Classe statique, sans état interne : entièrement déterministe et testable
    /// en isolation. Dépendance strictement unidirectionnelle :
    ///   GameOrchestrator → TurnManager → Rules/*
    ///
    /// NE VALIDE PAS la légalité du coup : CardPlayability et LastCardValidator
    /// sont appelés en amont par GameOrchestrator. Les gardes présentes ici
    /// relèvent de la défense en profondeur, pas de la validation métier.
    /// </summary>
    public static class TurnManager
    {
        private const int HandTargetSize = 3;

        // Resolvers sans état : instanciés une fois, réutilisés.
        private static readonly PhaseResolver Work = new WorkPhaseResolver();
        private static readonly PhaseResolver Talent = new TalentPhaseResolver();
        private static readonly PhaseResolver Luck = new LuckPhaseResolver();

        /// <summary>
        /// Résout le PhaseResolver correspondant à la phase d'un joueur.
        /// Finished n'a pas de resolver : un joueur sorti ne joue plus.
        /// </summary>
        private static PhaseResolver ResolverFor(DefPhase phase) => phase switch
        {
            DefPhase.Work => Work,
            DefPhase.Talent => Talent,
            DefPhase.Luck => Luck,
            _ => throw new InvalidOperationException(
                     $"Aucun PhaseResolver pour la phase {phase}.")
        };

        // ═══════════════════════════════════════════════════════════════════
        //  POINT D'ENTRÉE
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Applique un coup en suivant l'Ordre Strict d'Application des Effets.
        ///
        /// Retourne PendingGift si un Don est dû (étape 3) : l'appelant doit alors
        /// appeler ResolveGift() pour terminer les étapes 4-6. Dans tous les autres
        /// cas, retourne Complete avec l'état final.
        /// </summary>
        public static TurnResult ApplyPlay(GameState state, Play play)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            GuardPlayerIdIsSeatIndex(state, play);

            // ── ÉTAPE 1 — POSE ────────────────────────────────────────────
            // Les cartes quittent leur couche source et rejoignent la Pile.
            // Cas limite « 2 en dernière carte » traité ici : pose visible,
            // puis ramassage intégral, puis fin de tour sans effet ni rejeu.
            state = Step1_PlaceCards(state, play, out bool turnEndedByPickup);
            if (turnEndedByPickup)
                return TurnResult.Completed(Step6_AdvanceTurn(state, play, skipNext: false, replay: false));

            // ── ÉTAPE 2 — RECONSTRUCTION ──────────────────────────────────
            // Pioche, puis transition de phase + ramassage FaceUp le cas échéant,
            // puis re-pioche. La main atteint son état final avant tout effet.
            state = Step2_RebuildHand(state, play);

            // ── ÉTAPE 3 — EFFETS ────────────────────────────────────────────────
            var step3Result = Step3_ResolveCardEffects(state, play);

            // Si un Don est déclaré, suspendre ici
            if (step3Result.Status == TurnStatus.PendingGift)
            {
                return step3Result;  // L'appelant appellera ResolveGift(...) ensuite
            }

            // Sinon, continuer aux étapes 4-6
            var stateAfterStep4 = Step4_FinalDraw(step3Result.State, play.PlayerId);
            // … (suite : étapes 5-6)

            // ── ÉTAPES 4-6 ────────────────────────────────────────────────
            return TurnResult.Completed(
                RunStepsFourToSix(state, play, step3Result.State));

            // Étape 5 : effets de pile (Carré prioritaire sur Doublon)
            var stateAfterStep5 = Step5_ResolvePileEffects(
                stateAfterStep4, play, out bool skipNext, out bool replay);

            // Étape 6 : progression du tour
            var finalState = Step6_AdvanceTurn(stateAfterStep5, skipNext, replay);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = finalState,
                SuspendedPlay = null
            };
        }

        /// <summary>
        /// Reprend l'exécution après un Don, à l'ÉTAPE 4.
        ///
        /// Transfère giftedCard de la main du poseur vers celle de recipientId,
        /// puis exécute la re-pioche finale (le Don ayant retiré une carte de plus),
        /// les effets de pile et la transition de tour.
        /// </summary>
        public static TurnResult ResolveGift(
            TurnResult pending,
            Card giftedCard,
            int recipientId)
        {
            if (pending is null) throw new ArgumentNullException(nameof(pending));
            if (pending.Status != TurnStatus.PendingGift)
                throw new InvalidOperationException(
                    "ResolveGift n'est valide que sur un TurnResult PendingGift.");
            // … corps au morceau D
            throw new NotImplementedException("Morceau D");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ÉTAPES — implémentées morceau par morceau
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Morceau B — Étape 1 : pose des cartes du Play sur la Pile, retrait de la
        /// couche source (Hand/FaceUp/FaceDown) chez le poseur.
        /// Ne gère PAS le ramassage volontaire ou forcé de pile (turnEndedByPickup
        /// réservé à un cas futur détecté ailleurs — ex: FaceDown non conforme
        /// en Phase 3, qui sera traité au morceau consacré à LuckPhaseResolver).
        /// Retire les cartes une par une (jamais via Except) : un Player pourrait
        /// en théorie porter des doublons stricts après ramassage de pile.
        /// </summary>
        private static GameState Step1_PlaceCards(
            GameState state, Play play, out bool turnEndedByPickup)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            turnEndedByPickup = false;

            var player = state.Players[play.PlayerId];

            List<Card> sourceLayer = play.SourceLayer switch
            {
                CardLayer.Hand => new List<Card>(player.Hand),
                CardLayer.FaceUp => new List<Card>(player.FaceUp),
                CardLayer.FaceDown => new List<Card>(player.FaceDown),
                _ => throw new InvalidOperationException($"CardLayer inconnu : {play.SourceLayer}")
            };

            foreach (var card in play.Cards)
            {
                if (!sourceLayer.Remove(card))
                    throw new InvalidOperationException(
                        $"Play invalide : la carte {card} n'est pas présente dans la couche {play.SourceLayer} du joueur {play.PlayerId}.");
            }

            var updatedPlayer = play.SourceLayer switch
            {
                CardLayer.Hand => player.WithHand(sourceLayer),
                CardLayer.FaceUp => player.WithFaceUp(sourceLayer),
                CardLayer.FaceDown => player.WithFaceDown(sourceLayer),
                _ => throw new InvalidOperationException($"CardLayer inconnu : {play.SourceLayer}")
            };

            var updatedPile = state.Pile.Add(play);

            return state
                .WithPlayer(play.PlayerId, updatedPlayer)
                .WithPile(updatedPile);
        }

        /// <summary>
        /// ÉTAPE 2 — Morceau C : reconstitution de la main du poseur, dans l'ordre
        /// verrouillé (CLAUDE.md — Ordre Strict d'Application des Effets) :
        ///   2.1 DrawCards (pioche si Hand.Count < 3 et pioche non épuisée)
        ///   2.2 AdvancePlayerPhase + ramassage FaceUp (transition Work→Talent uniquement)
        ///   2.3 Ré-pioche si Hand.Count < 3 et pioche non épuisée après ramassage
        /// Ne gère PAS les effets spéciaux (étape 3) ni la re-pioche finale (étape 4,
        /// après Don éventuel — voir Step4_FinalDraw).
        /// </summary>
        private static GameState Step2_RebuildHand(GameState state, Play play)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            int playerId = play.PlayerId;

            // 2.1 — Pioche initiale.
            state = DrawUpToThree(state, playerId);

            var player = state.Players[playerId];

            // 2.2 — Transition Work → Talent : ramassage des FaceUp en main.
            if (player.CurrentPhase == DefPhase.Work
                && Work.ShouldTransitionToNextPhase(player))
            {
                var mergedHand = player.Hand.Concat(player.FaceUp).ToList();

                var advancedPlayer = player
                    .WithHand(mergedHand)
                    .WithFaceUp(Array.Empty<Card>())
                    .WithPhase(Work.NextPhase);

                state = state.WithPlayer(playerId, advancedPlayer);

                // 2.3 — Ré-pioche après ramassage.
                state = DrawUpToThree(state, playerId);
            }

            return state;
        }

        /// <summary>
        /// ÉTAPE 4 — Morceau C : re-pioche finale, après résolution des effets
        /// spéciaux (étape 3) et Don éventuel (le Don retire une carte de plus
        /// de la main, d'où la nécessité de re-piocher ici, indépendamment de
        /// l'étape 2).
        /// </summary>
        private static GameState Step4_FinalDraw(GameState state, int playerIndex)
            => DrawUpToThree(state, playerIndex);

        /// <summary>
        /// Pioche carte par carte jusqu'à Hand.Count == 3, ou jusqu'à épuisement
        /// de la pioche. Ne lève jamais : une pioche vide est un état normal de
        /// fin de partie (DrawPile n'est jamais remélangée). Mutualisée entre
        /// Step2_RebuildHand (pioche initiale + après ramassage FaceUp) et
        /// Step4_FinalDraw (re-pioche finale post-Don).
        /// </summary>
        private static GameState DrawUpToThree(GameState state, int playerId)
        {
            var player = state.Players[playerId];

            if (player.Hand.Count >= HandTargetSize || state.DrawPile.Count == 0)
                return state;

            var hand = player.Hand.ToList();
            var remainingDraw = state.DrawPile.ToList();

            while (hand.Count < HandTargetSize && remainingDraw.Count > 0)
            {
                int lastIndex = remainingDraw.Count - 1;
                hand.Add(remainingDraw[lastIndex]);
                remainingDraw.RemoveAt(lastIndex);
            }

            return state
                .WithPlayer(playerId, player.WithHand(hand))
                .WithDrawPile(remainingDraw);
        }

        /// <summary>
        /// ÉTAPE 3 : Morceau D — RÉSOLUTION DES EFFETS SPÉCIAUX DE CARTES.
        /// À ce stade, la main du poseur est à son état FINAL (post-reconstitution).
        /// Les handlers (SevenHandler, TwoHandler, JackHandler, PriestHandler, Jokers)
        /// DÉCLARENT les effets ; cette méthode les APPLIQUE sur GameState.
        /// 
        /// Ordonnancement d'exécution :
        ///   1. Effets spéciaux (Prêtre, 2, 7, Joker de Verre, etc.)
        ///   2. Détection Doublon/Carré (à l'étape 5, pas ici)
        /// 
        /// Retour : TurnStatus.Complete si aucun Don n'est dû,
        ///          TurnStatus.PendingGift si un Don est déclaré (ResolveGift() sera appelé ensuite).
        /// </summary>
        private static TurnResult Step3_ResolveCardEffects(GameState state, Play play)
        {
            GuardPlayerIdIsSeatIndex(state, play);

            var activePlayer = state.GetActivePlayer();

            // Dispatching par type de carte (standard ou Joker)
            if (play.IsJokerPlay)
            {
                return Step3_ResolveJokerEffect(state, play, activePlayer);
            }
            else
            {
                return Step3_ResolveStandardCardEffect(state, play, activePlayer);
            }
        }

        /// <summary>
        /// Sous-étape 3a : Résolution d'un effet de carte STANDARD (hauteur définie).
        /// Inclut les 4 cartes spéciales : Prêtre, Valet, 2, 7.
        /// </summary>
        private static TurnResult Step3_ResolveStandardCardEffect(GameState state, Play play, Player activePlayer)
        {
            var rank = play.EffectiveRank!.Value;

            // Dispatching par rang spécial
            return rank switch
            {
                DefRank.Seven => Step3_ResolveSeven(state, play, activePlayer),
                DefRank.Two => Step3_ResolveTwo(state, play, activePlayer),
                DefRank.Jack => Step3_ResolveJack(state, play, activePlayer),
                DefRank.Priest => Step3_ResolvePriest(state, play, activePlayer),
                _ => Step3_ResolveOrdinaryCard(state, play, activePlayer)
            };
        }

        /// <summary>
        /// Sous-étape 3b : Résolution d'un effet de JOKER (EffectiveRank = null).
        /// </summary>
        private static TurnResult Step3_ResolveJokerEffect(GameState state, Play play, Player activePlayer)
        {
            var jokerType = play.JokerType!.Value;

            return jokerType switch
            {
                DefJokerType.Glass => Step3_ResolveGlassJoker(state, play, activePlayer),
                DefJokerType.Black => Step3_ResolveBlackJoker(state, play, activePlayer),
                DefJokerType.Color => Step3_ResolveColorJoker(state, play, activePlayer),
                _ => throw new InvalidOperationException($"Type de Joker inconnu : {jokerType}")
            };
        }

        /// <summary>Carte 7 — LE DON.</summary>
        private static TurnResult Step3_ResolveSeven(GameState state, Play play, Player activePlayer)
        {
            var (constraintMode, constraintRank) = SevenHandler.ResolveConstraint(state);
            var newState = state.WithConstraint(constraintMode, constraintRank);

            // Vérifier si le Don est déclenché (exception : Face Cachée, ou main vide)
            if (SevenHandler.IsGiftTriggered(state, play))
            {
                // Le Don est dû : suspendre l'exécution du tour
                return new TurnResult
                {
                    Status = TurnStatus.PendingGift,
                    State = newState,
                    SuspendedPlay = play
                };
            }

            // Pas de Don : retour à l'état normal
            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Carte 2 — DESTRUCTION DE LA PILE ET REJEU DU POSEUR.</summary>
        private static TurnResult Step3_ResolveTwo(GameState state, Play play, Player activePlayer)
        {
            var (constraintMode, constraintRank) = TwoHandler.ResolveConstraint(state);

            // La pile est détruite
            var pileAfterTwo = state.Pile.Cleared();

            // Nouvelles contraintes (pile vide = pas de contrainte)
            var newState = state
                .WithPile(pileAfterTwo)
                .WithConstraint(constraintMode, constraintRank);

            // Le 2 ne génère jamais de Don
            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Valet — INVERSION DU SENS DE JEU.</summary>
        private static TurnResult Step3_ResolveJack(GameState state, Play play, Player activePlayer)
        {
            var newDirection = JackHandler.ResolveDirection(state);
            var (constraintMode, constraintRank) = JackHandler.ResolveConstraint(state);

            var newState = state
                .WithDirection(newDirection)
                .WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Prêtre — INVERSION TEMPORAIRE DE LA RÈGLE DE HAUTEUR.</summary>
        private static TurnResult Step3_ResolvePriest(GameState state, Play play, Player activePlayer)
        {
            var (constraintMode, constraintRank) = PriestHandler.ResolveConstraint(state);

            var newState = state.WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Carte ordinaire (3-6, 8-10, Cavalier, Dame, Roi, As) — aucun effet spécial.</summary>
        private static TurnResult Step3_ResolveOrdinaryCard(GameState state, Play play, Player activePlayer)
        {
            // Mise à jour de la contrainte : le joueur suivant doit jouer >= à cette hauteur
            var (constraintMode, constraintRank) = (HeightConstraint.Normal, play.EffectiveRank!.Value);

            var newState = state.WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Joker de Verre — TRANSPARENCE PURE.</summary>
        private static TurnResult Step3_ResolveGlassJoker(GameState state, Play play, Player activePlayer)
        {
            // Le Joker de Verre restitue la contrainte courante (traverse la chaîne)
            var (constraintMode, constraintRank) = GlassJokerResolver.ResolveConstraint(state);

            var newState = state.WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Joker Noir / Passe — RÉINITIALISATION DE LA CONTRAINTE.</summary>
        private static TurnResult Step3_ResolveBlackJoker(GameState state, Play play, Player activePlayer)
        {
            // La contrainte est réinitialisée : pile vide (pas de contrainte)
            var (constraintMode, constraintRank) = BlackJokerResolver.ResolveConstraint(state);

            var newState = state.WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>Joker Couleur / Bombe — DESTRUCTION DE LA PILE.</summary>
        private static TurnResult Step3_ResolveColorJoker(GameState state, Play play, Player activePlayer)
        {
            // La pile est détruite
            var pileAfterBomb = state.Pile.Cleared();

            // Nouvelles contraintes (pile vide = pas de contrainte)
            var (constraintMode, constraintRank) = ColorJokerResolver.ResolveConstraint(state);

            var newState = state
                .WithPile(pileAfterBomb)
                .WithConstraint(constraintMode, constraintRank);

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = newState,
                SuspendedPlay = null
            };
        }

        /// <summary>
        /// RÉSOLUTION DU DON (étape 3.5).
        /// Appelée **uniquement** si Step3_ResolveSeven retourne TurnStatus.PendingGift.
        /// Le joueur a choisi une carte et un destinataire : ce handler transfère la carte
        /// de la main du poseur vers la main du destinataire, puis continue l'exécution
        /// aux étapes 4-6 (re-pioche, détection Doublon/Carré, joueur suivant).
        ///
        /// Signature :
        ///   previousResult    : le TurnResult suspendu (contient SuspendedPlay = le 7 posé)
        ///   giftCard          : la carte exacte que le poseur donne (doit être dans sa main à l'instant T)
        ///   recipientId       : index du joueur destinataire (doit être ≠ poseur et ≠ définitivement sorti)
        ///
        /// Retour : TurnResult avec Status=Complete, State mis à jour avec les mains transférées.
        /// </summary>
        public static TurnResult ResolveGift(
            TurnResult previousResult,
            Card giftCard,
            int recipientId)
        {
            if (previousResult is null)
                throw new ArgumentNullException(nameof(previousResult));
            if (previousResult.Status != TurnStatus.PendingGift)
                throw new InvalidOperationException(
                    "ResolveGift ne peut être appelé que sur un TurnResult avec Status=PendingGift.");
            if (giftCard is null)
                throw new ArgumentNullException(nameof(giftCard));

            var state = previousResult.State;
            var play = previousResult.SuspendedPlay!;
            int poseurId = play.PlayerId;

            // Gardes
            GuardPlayerIdIsSeatIndex(state, play);
            if (recipientId < 0 || recipientId >= state.Players.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(recipientId), recipientId,
                    "Destinataire hors des sièges de la partie.");

            if (recipientId == poseurId)
                throw new InvalidOperationException(
                    "Un joueur ne peut pas se donner une carte à lui-même.");

            var poseur = state.GetActivePlayer();
            if (!poseur.Hand.Contains(giftCard))
                throw new InvalidOperationException(
                    $"La carte {giftCard} n'est pas dans la main du poseur.");

            // Transfert de la carte : retirer de la main du poseur, ajouter à celle du destinataire
            var poseurHandAfterGift = poseur.Hand
                .Where(c => !Equals(c, giftCard))
                .ToList();

            var recipient = state.Players[recipientId];
            var recipientHandAfterGift = recipient.Hand
                .Concat(new[] { giftCard })
                .ToList();

            // Mise à jour des deux joueurs dans l'état
            var stateAfterTransfer = state
                .WithPlayer(poseurId, poseur.WithHand(poseurHandAfterGift))
                .WithPlayer(recipientId, recipient.WithHand(recipientHandAfterGift));

            // Reprendre aux étapes 4-6
            // Étape 4 : re-pioche si main < 3 et pioche non épuisée
            var stateAfterStep4 = Step4_FinalDraw(stateAfterTransfer, poseurId);

            // Étapes 5-6 : détection Doublon/Carré + joueur suivant
            // (voir ApplyPlay pour l'intégration)

            return new TurnResult
            {
                Status = TurnStatus.Complete,
                State = stateAfterStep4,
                SuspendedPlay = null
            };
        }

        /// <summary>
        /// ÉTAPE 5 — EFFETS DE PILE (Carré / Doublon).
        /// Se déduit EXCLUSIVEMENT de la relecture de Pile.Cards / Pile.Plays :
        /// aucune chaîne n'est stockée dans GameState (pas de duplication d'état).
        ///
        /// Appelée APRÈS :
        ///   - la pose (étape 2),
        ///   - la reconstitution de main (étapes 2bis/4),
        ///   - la résolution des effets de cartes (étape 3) et le Don s'il était dû.
        ///
        /// Priorité verrouillée (CLAUDE.md) : CARRÉ PRIORITAIRE SUR DOUBLON.
        /// Un coup qui complète un Carré ne produit JAMAIS un skip de Doublon :
        /// la pile est détruite, le poseur rejoue, le skip est absorbé.
        /// Ordre d'interrogation imposé par le contrat de PairDetection, qui
        /// documente explicitement ne PAS arbitrer cette priorité elle-même.
        ///
        /// Effets :
        ///   - Carré   → pile détruite, contrainte neutralisée, REJEU DU POSEUR.
        ///   - Doublon → pile conservée, contrainte inchangée, SKIP DU SUIVANT.
        ///   - Aucun   → état inchangé.
        ///
        /// Court-circuit : si la pile est déjà vide en entrée, c'est qu'un effet
        /// destructeur de l'étape 3 (2, Joker Couleur/Bombe) l'a détruite. Il n'y a
        /// plus rien à relire : aucun Carré, aucun Doublon. Le rejeu éventuel est déjà
        /// porté par l'étape 3 — ne pas le redéclarer ici (double rejeu).
        /// </summary>
        private static GameState Step5_ResolvePileEffects(
            GameState state, Play play, out bool skipNext, out bool replay)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));
            if (play is null) throw new ArgumentNullException(nameof(play));

            skipNext = false;
            replay = false;

            // Pile détruite en amont (2, Bombe) → rien à détecter.
            if (state.Pile.IsEmpty)
                return state;

            // Un Joker seul ne porte pas de hauteur : il ne peut compléter
            // ni un Doublon ni un Carré. Garde redondante avec les détecteurs
            // (qui la portent déjà), conservée pour lisibilité de l'orchestration.
            if (play.IsJokerPlay)
                return state;

            // ── CARRÉ (priorité absolue, interrogé EN PREMIER) ────────────────
            if (QuadDetection.IsQuadDetected(state))
            {
                replay = true;      // le POSEUR rejoue (≠ Bombe, où c'est le suivant)
                skipNext = false;   // le skip de Doublon est absorbé par le Carré

                return state
                    .WithPile(state.Pile.Cleared())
                    .WithConstraint(HeightConstraint.Normal, DefRank.Three);
            }

            // ── DOUBLON ───────────────────────────────────────────────────────
            if (PairDetection.IsPairDetected(state))
            {
                skipNext = true;    // le joueur suivant est sauté
                replay = false;     // le poseur ne rejoue pas

                // Pile CONSERVÉE et contrainte INCHANGÉE : le Doublon ne touche
                // ni l'une ni l'autre. Une contrainte Prêtre survit au skip.
                return state;
            }

            // ── Aucun effet de pile ───────────────────────────────────────────
            return state;
        }

        /// <summary>
        /// ÉTAPE 6 — PROGRESSION DU TOUR.
        /// Dernière étape du tour : désigne le prochain joueur actif à partir
        /// du sens de jeu COURANT (déjà inversé par un éventuel Valet à l'étape 3)
        /// et des drapeaux produits par l'étape 5.
        ///
        /// Priorité des drapeaux :
        ///   1. replay  → le poseur rejoue : ActivePlayerIndex INCHANGÉ. Sortie immédiate.
        ///                (Carré à l'étape 5, ou 2 / rejeu déclaré à l'étape 3.)
        ///   2. skipNext → on avance de DEUX joueurs actifs au lieu d'un (Doublon).
        ///   3. aucun    → on avance d'UN joueur actif.
        ///
        /// « Joueur actif » = joueur non terminé (HasCards, CurrentPhase != Finished).
        /// Les joueurs sortis sont TRANSPARENTS : ils ne consomment pas un skip.
        /// Un Doublon saute le prochain joueur ENCORE EN JEU, pas un siège vide.
        ///
        /// Ne détermine PAS la fin de partie : si plus aucun joueur n'est éligible,
        /// l'état est retourné inchangé et c'est à GameResultCalculator de conclure.
        /// </summary>
        private static GameState Step6_AdvanceTurn(
            GameState state, bool skipNext, bool replay)
        {
            if (state is null) throw new ArgumentNullException(nameof(state));

            // 1. Rejeu : le poseur garde la main. Aucun avancement.
            if (replay)
                return state;

            int steps = skipNext ? 2 : 1;
            int nextIndex = FindNextActivePlayerIndex(state, steps);

            // Plus aucun joueur éligible (ou le poseur est le dernier en jeu) :
            // état inchangé, la fin de partie est du ressort de GameResultCalculator.
            if (nextIndex < 0)
                return state;

            return state.WithActivePlayerIndex(nextIndex);
        }

        /// <summary>
        /// Avance de <paramref name="steps"/> joueurs ENCORE EN JEU depuis le joueur
        /// actif, dans le sens de jeu courant. Les joueurs terminés sont ignorés
        /// (transparents : ils ne consomment pas une étape).
        /// Retourne -1 si aucun joueur en jeu autre que l'actif n'existe.
        /// </summary>
        private static int FindNextActivePlayerIndex(GameState state, int steps)
        {
            int count = state.Players.Count;
            int delta = state.Direction == PlayDirection.Clockwise ? 1 : -1;

            int index = state.ActivePlayerIndex;
            int found = 0;

            // Au plus un tour complet par étape à franchir : borne stricte
            // anti-boucle-infinie si plus personne n'est en jeu.
            int maxIterations = count * steps;

            for (int i = 0; i < maxIterations; i++)
            {
                index = ((index + delta) % count + count) % count;

                // Revenu sur soi-même sans avoir trouvé personne : seul en jeu.
                if (index == state.ActivePlayerIndex && found == 0)
                    continue;

                if (!IsStillInPlay(state.Players[index]))
                    continue; // joueur sorti : transparent, ne consomme pas l'étape

                found++;
                if (found == steps)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Un joueur est encore en jeu s'il n'a pas terminé sa dernière phase.
        /// Source de vérité = CurrentPhase (Finished), et non HasCards seul :
        /// un joueur peut être momentanément sans carte en main tout en ayant
        /// encore du FaceUp / FaceDown à jouer.
        /// </summary>
        private static bool IsStillInPlay(Player player)
            => player.CurrentPhase != DefPhase.Finished;

        // ═══════════════════════════════════════════════════════════════════
        //  INTERNES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Verdicts de l'étape 3, agrégés avant application.
        /// DestroysPile / GrantsReplay proviennent des handlers ; GiftDue de
        /// SevenHandler.IsGiftTriggered().
        /// </summary>
        private readonly struct CardEffects
        {
            public GameState State { get; }
            public bool DestroysPile { get; }
            public bool GrantsReplay { get; }
            public bool GiftDue { get; }
            public CardEffects(GameState state, bool destroysPile, bool grantsReplay, bool giftDue)
            {
                State = state; DestroysPile = destroysPile;
                GrantsReplay = grantsReplay; GiftDue = giftDue;
            }
        }


        /// <summary>
        /// Chaîne les étapes 4 → 5 → 6. Appelée depuis ApplyPlay (sans Don) et
        /// depuis ResolveGift (après Don) — garantit un ordre identique dans les
        /// deux chemins d'exécution.
        /// </summary>
        private static GameState RunStepsFourToSix(
            GameState state, Play play, CardEffects effects)
        {
            state = Step4_FinalDraw(state, play.PlayerId);

            bool skipNext = false;
            bool replay = effects.GrantsReplay;

            if (!effects.DestroysPile)
                state = Step5_ResolvePileEffects(state, play, out skipNext, out replay);

            return Step6_AdvanceTurn(state, play, skipNext, replay);
        }

        /// <summary>
        /// Construit le résultat suspendu : candidats au Don (main finale du poseur)
        /// et destinataires éligibles (adversaires non-Finished).
        /// CLAUDE.md : « parmi tous les adversaires restants (y compris ceux en
        /// Phase 3), sauf ceux qui ont déjà gagné ».
        /// </summary>
        private static TurnResult BuildPendingGift(GameState state, Play play)
            => new TurnResult
            {
                State = state,
                Status = TurnStatus.PendingGift,
                SuspendedPlay = play,
                GiftCandidates = state.Players[play.PlayerId].Hand,
                GiftRecipients = state.Players
                    .Where(p => p.Id != play.PlayerId
                             && p.CurrentPhase != DefPhase.Finished)
                    .Select(p => p.Id)
                    .ToList()
            };

        /// <summary>
        /// Défense en profondeur : l'invariant Play.PlayerId == index de siège est
        /// supposé partout dans ce service. S'il est rompu, échouer fort et tôt
        /// plutôt que corrompre silencieusement l'état d'un autre joueur.
        /// </summary>
        private static void GuardPlayerIdIsSeatIndex(GameState state, Play play)
        {
            if (play.PlayerId < 0 || play.PlayerId >= state.Players.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(play), play.PlayerId,
                    "Play.PlayerId hors des sièges de la partie.");

            if (state.Players[play.PlayerId].Id != play.PlayerId)
                throw new InvalidOperationException(
                    $"Invariant rompu : Players[{play.PlayerId}].Id vaut " +
                    $"{state.Players[play.PlayerId].Id}. TurnManager exige " +
                    "Play.PlayerId == index de siège.");
        }
    }
}