using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Résolution des effets spéciaux : Prêtre, Doublon, Valet, "2", Bombe, 7 (Don).
    /// Porte l'état transverse (Prêtre actif, dernier rang significatif) car ces effets
    /// dépendent de l'historique récent de la pile, pas seulement de son contenu actuel.
    /// Une instance par GameState.
    /// </summary>
    public class EffectResolver
    {
        /// <summary>
        /// Le Prêtre est actif : le prochain joueur à jouer une carte SIGNIFICATIVE
        /// doit poser une hauteur ≤ Prêtre (8).
        /// </summary>
        public bool IsPriestActive { get; private set; } = false;

        /// <summary>
        /// Hauteur plafond imposée par le Prêtre (int)CardRank.Priest = 8. -1 si inactif.
        /// </summary>
        public int PriestHeightBlock { get; private set; } = -1;

        /// <summary>
        /// Hauteur de la dernière carte significative posée.
        /// Utilisée pour détecter un Doublon : si la carte posée ce tour a le même rang
        /// que cette dernière, le Doublon est déclenché, puis ce tracker est mis à jour.
        /// </summary>
        public CardRank? LastSignificantRank { get; private set; } = null;

        // ------------------------------------------------------------------
        // PRÊTRE
        // ------------------------------------------------------------------

        /// <summary>
        /// Active le blocage Prêtre. Le prochain joueur qui pose une carte
        /// SIGNIFICATIVE (non-Joker de Verre) est contraint à ≤ 8.
        /// </summary>
        public void ActivatePriestBlock()
        {
            IsPriestActive = true;
            PriestHeightBlock = (int)CardRank.Priest; // 8
        }

        /// <summary>
        /// Réinitialise le blocage Prêtre (retour à la règle standard ≥ sommet).
        /// </summary>
        public void ResetPriestBlock()
        {
            IsPriestActive = false;
            PriestHeightBlock = -1;
        }

        // ------------------------------------------------------------------
        // DOUBLON
        // ------------------------------------------------------------------

        /// <summary>
        /// Doublon : la carte qui vient d'être posée a le même rang que la dernière
        /// carte significative posée avant elle (Joker de Verre transparent).
        /// Désactivé s'il ne reste que 2 joueurs actifs.
        /// Se déclenche au maximum une fois par tour (une seule paire comparée).
        /// </summary>
        public bool DetectDoublet(Card cardJustPlayed, int activePlayerCount)
        {
            if (activePlayerCount <= 2)
                return false;

            if (cardJustPlayed.IsJoker)
                return false;

            if (LastSignificantRank == null)
                return false;

            return cardJustPlayed.Rank == LastSignificantRank.Value;
        }

        /// <summary>
        /// Met à jour la dernière carte significative posée.
        /// - Joker de Verre : transparent, ne change rien
        /// - Joker Noir/Couleur : casse la chaîne Doublon => reset à null
        /// - Carte standard : met à jour avec ce rang
        /// </summary>
        public void UpdateLastSignificantRank(Card card)
        {
            if (card.IsJoker && card.JokerType == JokerType.Glass)
                return; // Transparent : ne change rien

            if (card.IsJoker && (card.JokerType == JokerType.Black || card.JokerType == JokerType.Color))
            {
                LastSignificantRank = null; // Casse la chaîne
                return;
            }

            // Carte standard
            LastSignificantRank = card.Rank;
        }

        /// <summary>
        /// Réinitialise le tracker Doublon (appelé à chaque destruction/ramassage de pile).
        /// </summary>
        public void ResetDoubletTracker()
        {
            LastSignificantRank = null;
        }

        // ------------------------------------------------------------------
        // DESTRUCTION DE PILE
        // ------------------------------------------------------------------

        /// <summary>
        /// Détruit la pile (Carré, "2", ou Bombe). La contrainte Prêtre disparaît
        /// avec la pile : nouvelle pile => hauteur libre.
        /// </summary>
        public void ResolvePileDestruction(Pile pile, DestructionReason reason)
        {
            while (!pile.IsEmpty())
            {
                pile.Pop();
            }

            ResetPriestBlock();
            ResetDoubletTracker();
        }

        // ------------------------------------------------------------------
        // VALET
        // ------------------------------------------------------------------

        /// <summary>
        /// Valet (L'Inverseur) : inverse le sens de jeu.
        /// </summary>
        public void HandleJackPlayed(TurnManager turnManager)
        {
            turnManager.ReverseDirection();
        }

        // ------------------------------------------------------------------
        // 2 (FERMETURE)
        // ------------------------------------------------------------------

        /// <summary>
        /// Fermeture spéciale du "2". La carte est déjà sur la pile.
        /// Cas normal : pile détruite, même joueur rejoue.
        /// Cas dernière carte avant transition : pile ramassée par le joueur, suivant ouvre.
        /// </summary>
        public void HandleTwoCardPlayed(
            Player player,
            Pile pile,
            TurnManager turnManager,
            PhaseTransitionResult playerState,
            System.Func<Player, bool> pickUpPile)
        {
            if (playerState == PhaseTransitionResult.NoChange)
            {
                ResolvePileDestruction(pile, DestructionReason.Two);
                turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: false);
            }
            else
            {
                pickUpPile(player);
                turnManager.HandleTwoPlayed(isLastCardBeforePhaseChange: true);
            }
        }

        // ------------------------------------------------------------------
        // 7 (DON)
        // ------------------------------------------------------------------

        /// <summary>
        /// Effet du 7 (Don). Le joueur qui a posé le 7 donne une carte de sa main.
        /// Silencieux en Phase Chance (révélation face cachée) ou si la carte n'est plus en main.
        /// </summary>
        public void HandleSevenPlayed(
            Player playerWhoPlayed,
            Player nextPlayer,
            Card cardToGift,
            GamePhase currentPhase,
            System.Action<Player> refillHand)
        {
            if (playerWhoPlayed == null || nextPlayer == null)
                return;

            if ((currentPhase == GamePhase.Travail || currentPhase == GamePhase.Talent) &&
                playerWhoPlayed.Hand.Contains(cardToGift))
            {
                playerWhoPlayed.Hand.Remove(cardToGift);
                nextPlayer.AddCardToHand(cardToGift);
                refillHand(playerWhoPlayed);
            }

            // Phase Chance ou carte absente => effet silencieux
        }
    }
}