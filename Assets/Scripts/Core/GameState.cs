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
        /// Par défaut GreaterOrEqual. Bascule temporairement à LessOrEqual via l'effet Prêtre (ticket dédié).
        /// </summary>
        public ComparisonMode ActiveComparisonMode { get; set; } = ComparisonMode.GreaterOrEqual;

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

        /// <summary>
        /// Remonte la pile depuis le sommet et retourne la première carte "significative" :
        /// - Un Joker de Verre est transparent : on continue de chercher sous lui.
        /// - Un Joker Noir ou Joker Couleur est lui-même significatif (il n'est pas transparent).
        /// - Une carte standard est significative.
        /// Retourne null si la pile est vide ou ne contient que des Jokers de Verre.
        /// </summary>
        public Card? GetEffectiveTopCard(Pile pile)
        {
            List<Card> significant = GetSignificantCardsFromTop(pile, 1);
            return significant.Count > 0 ? significant[0] : (Card?)null;
        }
        /// <summary>
        /// Vérifie si une carte respecte la règle de hauteur active par rapport à la référence effective
        /// (dernière carte significative de la pile, transparente au Joker de Verre).
        /// Pile vide (ou uniquement des Jokers de Verre) => toujours jouable.
        /// Ne gère PAS le rejeu, la fermeture de pli ni les transitions de phase — voir PlayCard/ResolvePileDestruction.
        /// </summary>
        public bool IsPlayable(Card card)
        {
            Card? effectiveTop = GetEffectiveTopCard(Pile);

            if (effectiveTop == null)
                return true;

            Card top = effectiveTop.Value;

            // Un Joker (Verre, Noir ou Couleur) est toujours jouable par-dessus n'importe quelle référence.
            if (card.IsJoker)
                return true;

            // Si la référence effective est elle-même un Joker (Noir ou Couleur), on ne compare pas de rang :
            // la pose est autorisée (Joker Noir "casse" toute contrainte de hauteur héritée).
            if (top.IsJoker)
                return true;

            int cardRank = (int)card.Rank;
            int topRank = (int)top.Rank;

            return ActiveComparisonMode == ComparisonMode.GreaterOrEqual
                ? cardRank >= topRank
                : cardRank <= topRank;
        }

        /// <summary>
        /// Un joueur ramasse la pile entière (volontaire ou forcé).
        /// Les cartes ramassées rejoignent la main du joueur.
        /// Ne déclenche pas de transition de phase (utiliser CheckPlayerState après appel).
        /// </summary>
        /// <returns>true si la pile contenait des cartes et a été ramassée, false si la pile était déjà vide.</returns>
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

            return true;
        }

        /// <summary>
        /// Renfloue la main du joueur jusqu'à 3 cartes si la pioche le permet.
        /// S'applique après toute pose de carte(s), indépendamment d'un rejeu ou non.
        /// Si la pioche s'épuise en cours de route, le joueur termine avec moins de 3 cartes.
        /// No-op si la main a déjà 3 cartes ou plus, ou si la pioche est vide.
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
        /// <summary>
        /// Remonte la pile depuis le sommet et retourne les N premières cartes "significatives"
        /// (Joker de Verre ignoré/transparent, non compté). S'arrête dès qu'un Joker Noir ou Couleur
        /// est rencontré (il est lui-même significatif et devient la dernière carte de la liste,
        /// car il casse toute chaîne Doublon/Carré au-delà).
        /// Retourne une liste de taille <= count.
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
                    continue; // transparent : ignoré, ne compte pas, on continue sous lui

                result.Add(current);

                if (current.IsJoker)
                    break; // Joker Noir/Couleur : significatif MAIS casse la chaîne en dessous
            }

            return result;
        }
        /// <summary>
        /// Détecte si les cartes du sommet de la pile forment un Carré (4 cartes de même rang).
        /// Le Joker de Verre est transparent (n'interrompt pas la chaîne et n'est pas compté comme une des 4).
        /// Le Joker Noir (et le Joker Couleur) casse la chaîne : il n'est jamais transparent pour le Carré.
        /// </summary>
        /// <returns>true si les 4 dernières cartes significatives (hors Joker de Verre) partagent le même rang.</returns>
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
                    return false; // Noir/Couleur rencontré avant d'avoir 4 cartes standard = chaîne cassée

                if (referenceRank == null)
                    referenceRank = current.Rank;
                else if (current.Rank != referenceRank.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Détecte un Doublet : deux tours consécutifs où la même hauteur a été jouée
        /// (pas deux cartes consécutives brutes de la pile — le Joker de Verre est transparent).
        /// Désactivé dès qu'il ne reste que 2 joueurs (à vérifier par l'appelant selon le contexte de la partie).
        /// </summary>
        /// <returns>true si les 2 dernières cartes significatives partagent le même rang.</returns>
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
                return false; // Noir/Couleur cassent toujours la chaîne Doublon

            return mostRecent.Rank == previous.Rank;
        }
        /// <summary>
        /// Résout la destruction/fermeture de la pile suite à un Carré, un "2", ou une Bombe (Joker Couleur).
        /// Vide entièrement la pile. Ne gère PAS le rejeu/skip du TurnManager :
        /// c'est à l'appelant de décider (même joueur rejoue pour Square/Two, joueur suivant pour Bomb).
        /// No-op si la pile est déjà vide.
        /// </summary>
        public void ResolvePileDestruction(DestructionReason reason)
        {
            while (!Pile.IsEmpty())
            {
                Pile.Pop();
            }
        }

        /// <summary>
        /// Gère la fermeture spéciale du "2". La carte a déjà été posée sur Pile par PlayCard.
        /// Cas normal (il reste des cartes) : pile détruite, même joueur rejoue.
        /// Cas dernière carte (transition de phase imminente, interdit de finir sur un 2) :
        ///   pile conservée, le joueur la ramasse entièrement (y compris le 2), joueur suivant ouvre.
        /// </summary>
        private void HandleTwoCardPlayed(Player player)
        {
            PhaseTransitionResult result = CheckPlayerState(player);

            if (result == PhaseTransitionResult.NoChange)
            {
                // Cas normal : pile détruite (y compris le 2), même joueur rejoue
                ResolvePileDestruction(DestructionReason.Two);
                TurnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: false);
            }
            else
            {
                // Cas dernière carte avant transition : le joueur ramasse toute la pile au lieu de la détruire
                PickUpPile(player);
                TurnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: true);

                // Note : la transition de phase réelle (Talent/Chance/Won) doit être appliquée
                // séparément par l'appelant de PlayCard (CheckPlayerState ne fait que détecter, pas appliquer).
            }
        }

        /// <summary>
        /// Un joueur joue une carte de sa main.
        /// Étapes :
        /// 1. Valide la carte (existe en main, jouable selon la hauteur active)
        /// 2. Pose la carte sur la pile
        /// 3. Détecte Carré/Doublet/2/Bombe et applique les effets
        /// 4. Gère les transitions de tour (rejeu, skip, joueur suivant)
        /// 5. Rembobine la main du joueur qui vient de jouer jusqu'à 3 cartes
        /// </summary>
        /// <returns>true si la pose a réussi, false si invalide (carte inexistante, non jouable).</returns>
        public bool PlayCard(Player player, Card card)
        {
            if (player == null)
                return false;

            // Étape 1 : Valide la carte
            if (!player.Hand.Contains(card))
                return false;

            if (!IsPlayable(card))
                return false;

            // Étape 2 : Pose la carte sur la pile
            player.RemoveCardFromHand(card);
            Pile.Add(card);

            // Étape 3 : Détecte les effets spéciaux et applique
            if (card.IsJoker && card.JokerType == JokerType.Color)
            {
                // Bombe : destruction + joueur suivant
                ResolvePileDestruction(DestructionReason.Bomb);
                TurnManager.HandleBombPlayed();
            }
            else if (card.Rank == CardRank.Two)
            {
                // 2 : gestion spéciale (peut être normal ou dernière carte)
                HandleTwoCardPlayed(player);
            }
            else if (DetectSquare(Pile))
            {
                // Carré : destruction + même joueur rejoue
                ResolvePileDestruction(DestructionReason.Square);
                // TurnManager.CurrentTurn ne change pas — rejeu automatique
            }
            else if (DetectDoublet(Pile))
            {
                // Doublet : saute le joueur suivant
                TurnManager.AdvanceToNextPlayer();  // Avance au prochain
                TurnManager.AdvanceToNextPlayer();  // Avance au joueur après (skip du premier)
            }
            else if (card.Rank == CardRank.Jack)
            {
                // Valet : inverse direction + joueur suivant (dans la nouvelle direction) joue
                HandleJackPlayed(player);
                TurnManager.AdvanceToNextPlayer();
            }
            else
            {
             // Cas normal : joueur suivant joue
             TurnManager.AdvanceToNextPlayer();
            }

            // Étape 5 : Rembobine la main du joueur qui VIENT DE JOUER jusqu'à 3 cartes
            // (avant toute avance de tour dans les étapes d'après)
            // Exception : Carré/Square où le même joueur rejoue immédiatement (rebobinage au prochain tour)
            RefillHand(player);

            return true;
        }
        /// <summary>
        /// Gère l'effet du 7 (Don / Gift).
        /// Le joueur qui a posé le 7 choisit un adversaire et une carte de sa main à donner.
        /// 
        /// Cas spéciaux :
        /// - Phase 1→2 : don après ramassage des cartes FaceUp
        /// - Phase 2→3 : don ANNULÉ (main vide après pose du 7)
        /// - Phase 3 retournement : pas d'effet (silencieux)
        /// - Phase 3 pose avec cartes restantes : don applicable
        /// - Phase 3 pose sans cartes restantes : don N/A
        /// - 2 joueurs : don applicable normalement
        /// </summary>
        public void HandleSevenPlayed(Player playerWhoPlayed, Player nextPlayer, Card cardToGift)
        {
            if (playerWhoPlayed == null || nextPlayer == null || cardToGift == null)
                return;

            // Cas 1 : Phase Travail ou Talent, playerWhoPlayed a cette carte en main
            if ((CurrentPhase == GamePhase.Travail || CurrentPhase == GamePhase.Talent) &&
                playerWhoPlayed.Hand.Contains(cardToGift))
            {
                // Joueur qui a joué le 7 choisit une carte DE SA MAIN et la donne
                playerWhoPlayed.Hand.Remove(cardToGift);
                nextPlayer.AddCardToHand(cardToGift);

                // ✅ Rebobiner le joueur qui a joué le 7 APRÈS le don
                RefillHand(playerWhoPlayed);
            }

            // Cas 2 & 3 : Phase Chance ou playerWhoPlayed sans cette carte = effet silencieux
        }

        /// <summary>
        /// Gère l'effet du Valet (L'Inverseur) : inverse le sens de jeu.
        /// Le joueur actuel reste le même, le prochain joueur change selon la nouvelle direction.
        /// À 2 joueurs : inversion + avance = retour au même joueur = "passe".
        /// </summary>
        public void HandleJackPlayed(Player player)
        {
            TurnManager.ReverseDirection();
            // Aucune autre action : le joueur suivant joue dans la nouvelle direction
        }
        /// <summary>
        /// Factory method pour créer une GameState avec un TurnManager custom.
        /// Utilisé principalement dans les tests pour injecter un TurnManager spécifique.
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