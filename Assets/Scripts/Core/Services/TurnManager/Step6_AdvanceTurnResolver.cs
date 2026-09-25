using System;
using System.Collections.Generic;
using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Sous-temps 6 de l'Ordre Strict défini dans CLAUDE.md :
    /// avance le joueur actif après la résolution des effets de pile (Step5).
    ///
    /// Les joueurs dont la phase est Finished sont exclus du comptage :
    /// ils ne peuvent jamais être une cible d'avancement, ni consommer un cran
    /// lorsque skipNext est actif (skipNext saute UN joueur actif, jamais un
    /// Finished qui ne "compte" pas dans le cercle).
    ///
    /// L'ancrage de départ est recherché via state.ActivePlayerIndex, qui pointe
    /// dans la liste BRUTE state.Players (non filtrée) : le joueur courant peut
    /// lui-même venir de passer Finished pendant le tour qui s'achève, et donc
    /// ne plus apparaître dans la liste des actifs — l'ancrage doit malgré tout
    /// rester valide pour amorcer le parcours.
    ///
    /// Ne mute jamais l'état directement (GameState est immuable) : le nouvel
    /// état est produit via GameState.WithActivePlayerIndex(...).
    ///
    /// La détection de fin de partie (0 ou 1 joueur actif restant) ne relève
    /// PAS de cette étape : Step6 se contente de ne pas boucler indéfiniment
    /// dans ce cas et retourne l'état inchangé. GameOrchestrator est seul
    /// responsable de détecter et traiter la fin de partie.
    /// </summary>
    public static class Step6_AdvanceTurnResolver
    {
        public static TurnResult Resolve(GameState state, bool skipNext, bool replay)
        {
            if (replay)
            {
                // Le joueur courant rejoue : aucun avancement, drapeaux consommés.
                return new TurnResult(state, skipNext: false, replay: false);
            }

            // Liste brute des index de joueurs actifs (Finished exclus).
            List<int> activePlayerIndexes = new List<int>();
            for (int index = 0; index < state.Players.Count; index++)
            {
                if (state.Players[index].CurrentPhase != DefPhase.Finished)
                {
                    activePlayerIndexes.Add(index);
                }
            }

            // Cas défensif : fin de partie imminente ou déjà atteinte.
            // Step6 ne détecte pas la fin de partie, il évite seulement de boucler.
            if (activePlayerIndexes.Count <= 1)
            {
                return new TurnResult(state, skipNext: false, replay: false);
            }

            // Ancrage sur la liste BRUTE (state.Players), pas sur la liste filtrée :
            // le joueur courant peut avoir terminé ce tour-ci et ne plus être actif.
            int anchorIndex = state.ActivePlayerIndex;
            if (anchorIndex < 0 || anchorIndex >= state.Players.Count)
            {
                return new TurnResult(state, skipNext: false, replay: false);
            }

            // 1 cran si simple avancement, 2 crans si skipNext (un joueur actif sauté).
            int requiredAdvances = skipNext ? 2 : 1;
            int currentIndex = anchorIndex;
            int advances = 0;

            while (advances < requiredAdvances)
            {
                currentIndex = GetNextIndex(currentIndex, state.Players.Count, state.Direction);

                // Un joueur Finished rencontré sur le trajet est ignoré silencieusement :
                // il ne consomme jamais un cran, qu'il s'agisse de l'avancement normal
                // ou du cran supplémentaire de skipNext.
                if (state.Players[currentIndex].CurrentPhase != DefPhase.Finished)
                {
                    advances++;
                }
            }

            GameState nextState = state.WithActivePlayerIndex(currentIndex);
            return new TurnResult(nextState, skipNext: false, replay: false);
        }

        /// <summary>Index brut suivant dans state.Players, selon le sens de jeu courant.</summary>
        private static int GetNextIndex(int currentIndex, int playerCount, PlayDirection direction)
        {
            if (direction == PlayDirection.Clockwise)
            {
                return (currentIndex + 1) % playerCount;
            }

            return (currentIndex - 1 + playerCount) % playerCount;
        }
    }
}