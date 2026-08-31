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
        /// Vérifie si une carte respecte la règle de hauteur active par rapport au sommet de pile.
        /// Pile vide => toujours jouable.
        /// Ne gère PAS les effets spéciaux (Carré, Doublon, fermeture de pli, rejeu, Prêtre) — voir T7+.
        /// </summary>
        public bool IsPlayable(Card card)
        {
            if (Pile.IsEmpty())
                return true;

            Card top = Pile.Top();

            // Un Joker est toujours jouable par-dessus n'importe quelle carte (règle de base T6 ;
            // les interactions fines Joker/Carré/Doublon seront affinées en T7+).
            if (card.IsJoker)
                return true;

            // Si la carte du dessus est un Joker, on ne peut pas comparer de rang :
            // T6 autorise la pose (règle affinée plus tard si nécessaire).
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
        /// Valide la règle de hauteur en vigueur (ActiveComparisonMode) contre le sommet de la pile.
        /// Ne gère PAS les effets spéciaux (Carré, Doublon, fermeture de pli, rejeu) — voir T7.
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
