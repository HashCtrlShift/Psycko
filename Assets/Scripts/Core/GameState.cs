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
        private Card? GetEffectiveTopCard(Pile pile)
        {
            if (pile == null || pile.IsEmpty())
                return null;

            IReadOnlyList<Card> cards = pile.Cards;

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                Card current = cards[i];

                if (current.IsJoker && current.JokerType == JokerType.Glass)
                    continue; // transparent, on continue de chercher en dessous

                return current;
            }

            return null; // uniquement des Jokers de Verre empilés
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
        /// Tente de jouer une carte pour le joueur donné depuis sa main.
        /// Valide la règle de hauteur en vigueur (ActiveComparisonMode) contre la référence effective de la pile.
        /// Ne gère PAS les effets spéciaux (Carré, Doublon, fermeture de pli, rejeu) — voir T7/T7ter.
        /// </summary>
        /// <returns>true si le coup a été joué, false si le coup est refusé (carte non jouable ou absente de la main).</returns>
        public bool PlayCard(Player player, Card card)
        {
            if (player == null)
                return false;

            if (!player.Hand.Contains(card))
                return false;

            if (!IsPlayable(card))
                return false;

            player.RemoveCardFromHand(card);
            Pile.Add(card);
            return true;
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
        /// Détecte si les cartes du sommet de la pile forment un Carré (4 cartes de même rang).
        /// Le Joker de Verre est transparent (n'interrompt pas la chaîne et n'est pas compté comme une des 4).
        /// Le Joker Noir (et le Joker Couleur) casse la chaîne : il n'est jamais transparent pour le Carré.
        /// </summary>
        /// <returns>true si les 4 dernières cartes significatives (hors Joker de Verre) partagent le même rang.</returns>
        public bool DetectSquare(Pile pile)
        {
            if (pile == null || pile.Count < 4)
                return false;

            IReadOnlyList<Card> cards = pile.Cards;
            int matchCount = 0;
            CardRank? referenceRank = null;

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                Card current = cards[i];

                if (current.IsJoker)
                {
                    if (current.JokerType == JokerType.Glass)
                        continue; // transparent : on continue de remonter la chaîne

                    break; // Joker Noir ou Couleur : casse la chaîne
                }

                if (referenceRank == null)
                {
                    referenceRank = current.Rank;
                    matchCount = 1;
                }
                else if (current.Rank == referenceRank.Value)
                {
                    matchCount++;
                }
                else
                {
                    break;
                }

                if (matchCount == 4)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Détecte un Doublon : deux tours consécutifs où la même hauteur a été jouée
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

            IReadOnlyList<Card> cards = pile.Cards;
            var significant = new List<Card>();

            for (int i = cards.Count - 1; i >= 0 && significant.Count < 2; i--)
            {
                Card current = cards[i];

                if (current.IsJoker && current.JokerType == JokerType.Glass)
                    continue; // transparent

                significant.Add(current);
            }

            if (significant.Count < 2)
                return false;

            Card mostRecent = significant[0];
            Card previous = significant[1];

            if (mostRecent.IsJoker || previous.IsJoker)
                return false;

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