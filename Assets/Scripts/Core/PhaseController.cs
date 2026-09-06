using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Résultat d'une révélation de carte FaceDown en Phase Chance.
    /// </summary>
    public enum ChanceRevealOutcome
    {
        Passed,      // la carte révélée était jouable, posée sur la pile
        PickedUp     // la carte révélée ne passait pas, joueur ramasse toute la pile
    }

    /// <summary>
    /// Gère les transitions de phase (Travail → Talent → Chance → Victoire)
    /// et la mécanique de révélation FaceDown. Logique de jeu pure — ne doit
    /// jamais être dupliquée côté Présentation/Console.
    /// </summary>
    public static class PhaseController
    {
        /// <summary>
        /// Détecte l'état du joueur sans modifier le joueur ni l'état de la partie.
        /// </summary>
        public static PhaseTransitionResult CheckPlayerState(Player player, Deck deck)
        {
            int handCount = player.Hand.Count;
            int faceUpCount = player.FaceUp.Count;
            int faceDownCount = player.FaceDown.Count;
            bool deckIsEmpty = deck.Count == 0;

            if (handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount == 0)
                return PhaseTransitionResult.Won;

            // Bascule Talent -> Chance : main vide, pioche vide, plus de face-up, il reste du face-down
            if (player.CurrentPhase == GamePhase.Talent &&
                handCount == 0 && deckIsEmpty && faceUpCount == 0 && faceDownCount > 0)
                return PhaseTransitionResult.TransitionedToChance;

            // Bascule Travail -> Talent : main vide, pioche vide, il reste du face-up
            if (player.CurrentPhase == GamePhase.Travail &&
                handCount == 0 && deckIsEmpty && faceUpCount > 0)
                return PhaseTransitionResult.TransitionedToTalent;

            return PhaseTransitionResult.NoChange;
        }

        /// <summary>
        /// Applique la fusion FaceUp → Main (Travail → Talent) si les conditions sont réunies.
        /// </summary>
        /// <returns>true si la fusion a eu lieu.</returns>
        public static bool TryMergeFaceUpIntoHand(Player player, Deck deck)
        {
            if (player.CurrentPhase == GamePhase.Travail &&
                player.Hand.Count == 0 &&
                deck.Count == 0 &&
                player.FaceUp.Count > 0)
            {
                player.Hand.AddRange(player.FaceUp);
                player.FaceUp.Clear();
                player.CurrentPhase = GamePhase.Talent;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Bascule Talent → Chance (changement d'étiquette seul, pas de fusion en bloc).
        /// </summary>
        /// <returns>true si la bascule a eu lieu.</returns>
        public static bool TryTransitionTalentToChance(Player player, Deck deck)
        {
            if (player.CurrentPhase == GamePhase.Talent &&
                player.Hand.Count == 0 &&
                deck.Count == 0 &&
                player.FaceUp.Count == 0 &&
                player.FaceDown.Count > 0)
            {
                player.CurrentPhase = GamePhase.Chance;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Révèle UNE carte FaceDown (Phase Chance, main vide). Pose la carte sur la pile
        /// si elle passe, sinon fait ramasser toute la pile au joueur.
        /// Le tour du joueur est TOUJOURS terminé après un appel réussi (pas de rejeu).
        /// </summary>
        /// <returns>null si aucune révélation n'était due (conditions non réunies).</returns>
        public static ChanceRevealOutcome? TryRevealFaceDown(
            Player player,
            GameState gameState)
        {
            if (player.CurrentPhase != GamePhase.Chance ||
                player.Hand.Count != 0 ||
                player.FaceDown.Count == 0)
                return null;

            Card revealed = player.FaceDown[0];
            player.FaceDown.RemoveAt(0);

            if (gameState.IsPlayable(revealed))
            {
                gameState.Pile.Add(revealed);
                gameState.UpdateLastSignificantRank(revealed);
                gameState.TurnManager.HandlePlayerTransitionedOrWon(player);
                return ChanceRevealOutcome.Passed;
            }
            else
            {
                gameState.Pile.Add(revealed);
                gameState.PickUpPile(player);
                gameState.TurnManager.HandlePlayerPickedUp();
                return ChanceRevealOutcome.PickedUp;
            }
        }

        /// <summary>
        /// Détecte et applique la victoire (tous les étages épuisés en Phase Chance).
        /// </summary>
        /// <returns>true si le joueur vient de gagner.</returns>
        public static bool TryDeclareVictory(Player player, GameState gameState)
        {
            if (player.CurrentPhase == GamePhase.Chance &&
                player.Hand.Count == 0 &&
                player.FaceUp.Count == 0 &&
                player.FaceDown.Count == 0 &&
                !player.HasWon)
            {
                player.HasWon = true;
                gameState.TurnManager.HandlePlayerTransitionedOrWon(player);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Applique toutes les transitions applicables pour ce joueur, dans l'ordre correct :
        /// Victoire (priorité absolue) → Travail→Talent → Talent→Chance → Révélation Chance.
        /// </summary>
        /// <returns>true si le tour du joueur est déjà consommé (Run() doit faire "continue").</returns>
        public static bool ApplyAll(Player player, GameState gameState)
        {
            if (TryDeclareVictory(player, gameState))
                return true;

            TryMergeFaceUpIntoHand(player, gameState.Deck);
            TryTransitionTalentToChance(player, gameState.Deck);

            ChanceRevealOutcome? revealOutcome = TryRevealFaceDown(player, gameState);
            if (revealOutcome != null)
                return true;

            return false;
        }
    }
}