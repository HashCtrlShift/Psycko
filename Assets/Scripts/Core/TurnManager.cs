using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko
{
    public class TurnManager
    {
        private List<Player> players;
        public TurnState CurrentTurn { get; private set; }

        public TurnManager(List<Player> players, Player startingPlayer = null)
        {
            if (players == null || players.Count == 0)
                throw new ArgumentException("Players list cannot be null or empty", nameof(players));

            this.players = players;

            if (startingPlayer == null)
                startingPlayer = players[0];

            int startIndex = players.IndexOf(startingPlayer);
            if (startIndex == -1)
                throw new ArgumentException("Starting player not found in players list", nameof(startingPlayer));

            CurrentTurn = new TurnState(startingPlayer, startIndex, GameDirection.Clockwise);
        }

        /// <summary>
        /// Retourne le nombre de joueurs encore actifs (HasWon = false).
        /// </summary>
        public int GetActivePlayerCount()
        {
            return players.Count(p => !p.HasWon);
        }

        /// <summary>
        /// Retourne l'indice du prochain joueur actif en fonction de la direction actuelle.
        /// </summary>
        private int GetNextPlayerIndex()
        {
            int nextIndex;

            if (CurrentTurn.Direction == GameDirection.Clockwise)
            {
                nextIndex = (CurrentTurn.CurrentPlayerIndex + 1) % players.Count;
            }
            else
            {
                nextIndex = (CurrentTurn.CurrentPlayerIndex - 1 + players.Count) % players.Count;
            }

            // Skip joueurs qui ont gagné
            int attempts = 0;
            while (players[nextIndex].HasWon && attempts < players.Count)
            {
                if (CurrentTurn.Direction == GameDirection.Clockwise)
                {
                    nextIndex = (nextIndex + 1) % players.Count;
                }
                else
                {
                    nextIndex = (nextIndex - 1 + players.Count) % players.Count;
                }
                attempts++;
            }

            // Si tous les autres joueurs ont gagné, on retourne l'index du prochain même s'il a gagné
            return nextIndex;
        }

        /// <summary>
        /// Passe au joueur suivant (cas standard : pas de rejeu).
        /// </summary>
        public void AdvanceToNextPlayer()
        {
            int nextIndex = GetNextPlayerIndex();
            CurrentTurn = new TurnState(players[nextIndex], nextIndex, CurrentTurn.Direction);
        }

        /// <summary>
        /// Inverse le sens de jeu (Valet). Le joueur actuel reste le même.
        /// </summary>
        public void ReverseDirection()
        {
            CurrentTurn.Direction = CurrentTurn.Direction == GameDirection.Clockwise
                ? GameDirection.CounterClockwise
                : GameDirection.Clockwise;
        }

        /// <summary>
        /// Joueur joue un 2.
        /// Cas normal (il reste des cartes) : aucune avance (même joueur rejoue).
        /// Cas dernière carte (transition de phase imminente) : avance au joueur suivant.
        /// </summary>
        public void HandleTwoPlayed(bool isLastCardBeforePhaseChange)
        {
            if (isLastCardBeforePhaseChange)
            {
                AdvanceToNextPlayer();
            }
            // Cas normal : CurrentTurn ne change pas, le même joueur rejoue
        }

        /// <summary>
        /// Joueur joue Joker Couleur (Bombe) : pile détruite, joueur suivant commence la nouvelle pile.
        /// </summary>
        public void HandleBombPlayed()
        {
            AdvanceToNextPlayer();
        }

        /// <summary>
        /// Joueur ramasse (forcé ou volontaire) : joueur suivant ouvre.
        /// </summary>
        public void HandlePlayerPickedUp()
        {
            AdvanceToNextPlayer();
        }

        /// <summary>
        /// Joueur passe (déconnexion, timeout, etc.) : joueur suivant ouvre.
        /// </summary>
        public void HandlePlayerPassed()
        {
            AdvanceToNextPlayer();
        }

        /// <summary>
        /// Joueur termine sa phase ou la partie : joueur suivant joue (pas d'interruption).
        /// </summary>
        public void HandlePlayerTransitionedOrWon(Player player)
        {
            AdvanceToNextPlayer();
        }

        /// <summary>
        /// Retourne true si un seul joueur reste actif (condition de victoire).
        /// </summary>
        public bool IsGameOver()
        {
            return GetActivePlayerCount() == 1;
        }

        /// <summary>
        /// Retourne le joueur perdant (Psycko) — le dernier avec des cartes si IsGameOver().
        /// </summary>
        public Player GetLoser()
        {
            if (!IsGameOver())
                return null;

            return players.FirstOrDefault(p => !p.HasWon);
        }
    }
}