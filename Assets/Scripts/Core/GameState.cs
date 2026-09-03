using System;
using System.Collections.Generic;
using Psycko.Core;

namespace Psycko
{
    public class GameState
    {
        public GamePhase CurrentPhase { get; set; }
        public List<Player> Players { get; }
        public Pile Pile { get; }
        public Deck Deck { get; }

        /// <summary>
        /// Mode de comparaison actif pour la validation de pose de carte.
        /// Par défaut GreaterOrEqual. Bascule temporairement à LessOrEqual via l'effet Prêtre.
        /// </summary>
        public ComparisonMode ActiveComparisonMode { get; set; } = ComparisonMode.GreaterOrEqual;

        /// <summary>
        /// Le Prêtre est actif : le prochain joueur à jouer une carte SIGNIFICATIVE
        /// doit poser une hauteur ≤ Prêtre (8).
        /// </summary>
        public bool IsPriestActive { get; private set; } = false;

        /// <summary>
        /// Hauteur plafond imposée par le Prêtre (int)CardRank.Priest = 8. -1 si inactif.
        /// </summary>
        public int PriestHeightBlock { get; private set; } = -1;

        public TurnManager TurnManager { get; private set; }

        public GameState(
            IEnumerable<Player> players = null,
            Pile pile = null,
            Deck deck = null)
        {
            CurrentPhase = GamePhase.Travail;
            Players = players == null ? new List<Player>() : new List<Player>(players);
            Pile = pile ?? new Pile();
            Deck = deck ?? new Deck();
            TurnManager = new TurnManager(Players);
        }

        // ------------------------------------------------------------------
        // PRÊTRE
        // ------------------------------------------------------------------

        /// <summary>
        /// Active le blocage Prêtre. Le prochain joueur qui pose une carte
        /// SIGNIFICATIVE (non-Joker de Verre) est contraint à ≤ 8.
        /// </summary>
        public void ActivatePriestBlock(int playerIndex)
        {
            IsPriestActive = true;
            PriestHeightBlock = (int)CardRank.Priest; // 8
            ActiveComparisonMode = ComparisonMode.LessOrEqual;
        }

        /// <summary>
        /// Réinitialise le blocage Prêtre (retour à la règle standard ≥ sommet).
        /// </summary>
        public void ResetPriestBlock()
        {
            IsPriestActive = false;
            PriestHeightBlock = -1;
            ActiveComparisonMode = ComparisonMode.GreaterOrEqual;
        }

        /// <summary>
        /// Une carte est "significative" pour la consommation de la contrainte Prêtre
        /// si elle n'est PAS un Joker de Verre.
        /// Le Joker de Verre est totalement transparent : il ne consomme pas la contrainte,
        /// qui reste donc active pour le joueur suivant.
        /// </summary>
        public static bool IsSignificantForPriest(Card card)
        {
            return !(card.IsJoker && card.JokerType == JokerType.Glass);
        }

        // ------------------------------------------------------------------
        // ÉTAT DU JOUEUR / PHASES
        // ------------------------------------------------------------------

        /// <summary>
        /// Détecte l'état du joueur sans modifier le joueur ni l'état de la partie.
        /// </summary>
        public PhaseTransitionResult CheckPlayerState(Player player)
        {
            int handCount = player.Hand.Count;
            int faceUpCount = player.FaceUp.Count;
            int faceDownCount = player.FaceDown.Count;
            bool deckIsEmpty = Deck.Count == 0;

            if (handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount == 0)
                return PhaseTransitionResult.Won;

            if (CurrentPhase != GamePhase.Chance &&
                handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount > 0)
                return PhaseTransitionResult.TransitionedToChance;

            if (CurrentPhase == GamePhase.Travail &&
                handCount == 0 && deckIsEmpty && faceUpCount > 0)
                return PhaseTransitionResult.TransitionedToTalent;

            return PhaseTransitionResult.NoChange;
        }

        // ------------------------------------------------------------------
        // LECTURE DE LA PILE
        // ------------------------------------------------------------------

        /// <summary>
        /// Remonte la pile et retourne la première carte "significative" :
        /// - Joker de Verre : transparent, on continue sous lui.
        /// - Joker Noir / Couleur : significatif (hauteur libre).
        /// - Carte standard : significative.
        /// Retourne null si pile vide ou uniquement des Jokers de Verre.
        /// </summary>
        public Card? GetEffectiveTopCard(Pile pile)
        {
            List<Card> significant = GetSignificantCardsFromTop(pile, 1);
            return significant.Count > 0 ? significant[0] : (Card?)null;
        }

        /// <summary>
        /// Remonte la pile et retourne les N premières cartes significatives.
        /// Joker de Verre ignoré/non compté. S'arrête sur un Joker Noir/Couleur
        /// (inclus dans la liste, mais casse la chaîne en dessous).
        /// </summary>
        public List<Card> GetSignificantCardsFromTop(Pile pile, int count)
        {
            var result = new List<Card>();

            if (pile == null || pile.IsEmpty())
                return result;

            IReadOnlyList<Card> cards = pile.Cards;

            for (int i = cards.Count - 1; i >= 0 && result.Count < count; i--)
            {
                Card current = cards[i];

                if (current.IsJoker && current.JokerType == JokerType.Glass)
                    continue; // transparent

                result.Add(current);

                if (current.IsJoker)
                    break; // Noir/Couleur : casse la chaîne
            }

            return result;
        }

        // ------------------------------------------------------------------
        // VALIDATION DE POSE
        // ------------------------------------------------------------------

        /// <summary>
        /// Vérifie si une carte respecte la règle de hauteur active.
        /// Priorité : Prêtre actif (≤ 8) > Joker (toujours jouable) > hauteur libre > ≥ sommet.
        /// </summary>
        public bool IsPlayable(Card card)
        {
            // Prêtre actif : la contrainte s'applique AVANT tout,
            // sauf pour les Jokers qui restent toujours posables.
            if (IsPriestActive)
            {
                if (card.IsJoker)
                    return true;

                return (int)card.Rank <= PriestHeightBlock;
            }

            // Hors Prêtre : les Jokers sont toujours jouables
            if (card.IsJoker)
                return true;

            Card? effectiveTop = GetEffectiveTopCard(Pile);

            // Pile vide (ou uniquement Jokers de Verre) => jouable
            if (effectiveTop == null)
                return true;

            Card top = effectiveTop.Value;

            // Référence = Joker Noir/Couleur => hauteur libre
            if (top.IsJoker)
                return true;

            return (int)card.Rank >= (int)top.Rank;
        }

        // ------------------------------------------------------------------
        // RAMASSAGE / PIOCHE
        // ------------------------------------------------------------------

        /// <summary>
        /// Un joueur ramasse la pile entière. Les cartes rejoignent sa main.
        /// </summary>
        public bool PickUpPile(Player player)
        {
            if (player == null)
                return false;

            if (Pile.IsEmpty())
                return false;

            while (!Pile.IsEmpty())
            {
                player.AddCardToHand(Pile.Pop());
            }

            // La pile disparaît => la contrainte Prêtre disparaît avec elle
            ResetPriestBlock();

            return true;
        }

        /// <summary>
        /// Renfloue la main du joueur jusqu'à 3 cartes si la pioche le permet.
        /// </summary>
        public void RefillHand(Player player)
        {
            if (player == null)
                return;

            while (player.Hand.Count < 3 && Deck.Count > 0)
            {
                player.AddCardToHand(Deck.Draw());
            }
        }

        // ------------------------------------------------------------------
        // DÉTECTIONS
        // ------------------------------------------------------------------

        /// <summary>
        /// Carré : 4 cartes significatives de même rang au sommet.
        /// Joker de Verre transparent, Joker Noir/Couleur casse la chaîne.
        /// </summary>
        public bool DetectSquare(Pile pile)
        {
            if (pile == null || pile.Count < 4)
                return false;

            List<Card> significant = GetSignificantCardsFromTop(pile, 4);

            if (significant.Count < 4)
                return false;

            CardRank? referenceRank = null;

            foreach (Card current in significant)
            {
                if (current.IsJoker)
                    return false; // chaîne cassée

                if (referenceRank == null)
                    referenceRank = current.Rank;
                else if (current.Rank != referenceRank.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Doublon : les 2 dernières cartes significatives partagent le même rang.
        /// Désactivé s'il ne reste que 2 joueurs actifs.
        /// </summary>
        public bool DetectDoublet(Pile pile)
        {
            if (TurnManager.GetActivePlayerCount() <= 2)
                return false;

            if (pile == null || pile.Count < 2)
                return false;

            List<Card> significant = GetSignificantCardsFromTop(pile, 2);

            if (significant.Count < 2)
                return false;

            Card mostRecent = significant[0];
            Card previous = significant[1];

            if (mostRecent.IsJoker || previous.IsJoker)
                return false;

            return mostRecent.Rank == previous.Rank;
        }

        // ------------------------------------------------------------------
        // DESTRUCTION DE PILE
        // ------------------------------------------------------------------

        /// <summary>
        /// Détruit la pile (Carré, "2", ou Bombe). La contrainte Prêtre disparaît
        /// avec la pile : nouvelle pile => hauteur libre.
        /// </summary>
        public void ResolvePileDestruction(DestructionReason reason)
        {
            while (!Pile.IsEmpty())
            {
                Pile.Pop();
            }

            ResetPriestBlock();
        }

        /// <summary>
        /// Fermeture spéciale du "2". La carte est déjà sur la pile.
        /// Cas normal : pile détruite, même joueur rejoue.
        /// Cas dernière carte avant transition : pile ramassée par le joueur, suivant ouvre.
        /// </summary>
        private void HandleTwoCardPlayed(Player player)
        {
            PhaseTransitionResult result = CheckPlayerState(player);

            if (result == PhaseTransitionResult.NoChange)
            {
                ResolvePileDestruction(DestructionReason.Two);
                TurnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: false);
            }
            else
            {
                PickUpPile(player);
                TurnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: true);
            }
        }

        /// <summary>
        /// Valet (L'Inverseur) : inverse le sens de jeu.
        /// </summary>
        public void HandleJackPlayed(Player player)
        {
            TurnManager.ReverseDirection();
        }

        // ------------------------------------------------------------------
        // POSE DE CARTE
        // ------------------------------------------------------------------

        /// <summary>
        /// Un joueur joue une carte de sa main.
        /// Ordre de résolution :
        /// 1. Validation (carte en main + hauteur active)
        /// 2. Pose sur la pile
        /// 3. Consommation de la contrainte Prêtre (Joker de Verre exempté)
        /// 4. Effets spéciaux (Bombe, 2, Carré, Doublon, Valet)
        /// 5. Avance de tour si aucun effet ne l'a fait
        /// 6. Renflouage de la main
        /// 7. Activation d'un nouveau Prêtre si la carte posée en est un
        /// </summary>
        public bool PlayCard(Player player, Card card)
        {
            if (player == null || !player.Hand.Contains(card) || !IsPlayable(card))
                return false;

            int currentPlayerIndex = TurnManager.CurrentTurn.CurrentPlayerIndex;

            // Pose
            player.RemoveCardFromHand(card);
            Pile.Add(card);

            // ---- Consommation de la contrainte Prêtre ----
            // Le Joker de Verre est TRANSPARENT : il ne consomme pas la contrainte,
            // qui reste active pour le joueur suivant.
            // Toute autre carte (standard ou Joker Noir/Couleur) la consomme.
            if (IsPriestActive && IsSignificantForPriest(card))
            {
                ResetPriestBlock();
            }

            // ---- Effets spéciaux ----
            bool effectHandled = false;

            if (card.IsJoker && card.JokerType == JokerType.Color)
            {
                // Bombe : destruction + joueur suivant. Jamais de rejeu.
                ResolvePileDestruction(DestructionReason.Bomb);
                TurnManager.HandleBombPlayed();
                effectHandled = true;
            }
            else if (card.IsStandardCard && card.Rank == CardRank.Two)
            {
                HandleTwoCardPlayed(player);
                effectHandled = true;
            }
            else if (DetectSquare(Pile))
            {
                // Carré : pile détruite, même joueur rejoue (CurrentTurn inchangé)
                ResolvePileDestruction(DestructionReason.Square);
                effectHandled = true;
            }
            else if (DetectDoublet(Pile))
            {
                // Doublon : saute le joueur suivant
                TurnManager.AdvanceToNextPlayer();
                TurnManager.AdvanceToNextPlayer();
                effectHandled = true;
            }
            else if (card.IsStandardCard && card.Rank == CardRank.Jack)
            {
                HandleJackPlayed(player);
                TurnManager.AdvanceToNextPlayer();
                effectHandled = true;
            }

            if (!effectHandled)
            {
                TurnManager.AdvanceToNextPlayer();
            }

            // Renfloue la main du joueur qui vient de jouer
            RefillHand(player);

            // ---- Activation d'un nouveau Prêtre ----
            // Placée en DERNIER pour survivre à tous les resets ci-dessus.
            // Exception : si la pile a été détruite (Carré/2/Bombe), la contrainte
            // ne s'applique pas — nouvelle pile = hauteur libre.
            if (card.IsStandardCard && card.Rank == CardRank.Priest && !Pile.IsEmpty())
            {
                ActivatePriestBlock(currentPlayerIndex);
            }

            return true;
        }

        // ------------------------------------------------------------------
        // 7 (DON)
        // ------------------------------------------------------------------

        /// <summary>
        /// Effet du 7 (Don). Le joueur qui a posé le 7 donne une carte de sa main.
        /// Silencieux en Phase Chance (révélation face cachée) ou si la carte n'est plus en main.
        /// </summary>
        public void HandleSevenPlayed(Player playerWhoPlayed, Player nextPlayer, Card cardToGift)
        {
            if (playerWhoPlayed == null || nextPlayer == null)
                return;

            if ((CurrentPhase == GamePhase.Travail || CurrentPhase == GamePhase.Talent) &&
                playerWhoPlayed.Hand.Contains(cardToGift))
            {
                playerWhoPlayed.Hand.Remove(cardToGift);
                nextPlayer.AddCardToHand(cardToGift);
                RefillHand(playerWhoPlayed);
            }

            // Phase Chance ou carte absente => effet silencieux
        }

        // ------------------------------------------------------------------
        // FACTORY
        // ------------------------------------------------------------------

        /// <summary>
        /// Crée une GameState avec un TurnManager injecté (tests).
        /// </summary>
        public static GameState CreateWithTurnManager(
            IEnumerable<Player> players,
            Pile pile,
            Deck deck,
            TurnManager turnManager)
        {
            var gameState = new GameState(players, pile, deck);
            gameState.TurnManager = turnManager ?? new TurnManager(gameState.Players);
            return gameState;
        }
    }
}