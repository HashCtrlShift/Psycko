using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core;

namespace Psycko
{
    /// <summary>
    /// Gère la phase d'échange pré-jeu (~1 min, durée pilotée par le serveur/réseau, hors Core) :
    /// swap 1-pour-1 entre une carte en main et une carte face-up, répétable à volonté.
    /// Les Face-Down ne sont jamais concernées.
    /// </summary>
    public static class PreGameSwap
    {
        #region Determine First Player Utilities
        /// <summary>
        /// Ordre de priorité des couleurs pour la détermination du premier joueur,
        /// INDÉPENDANT de l'enum CardSuit (dont les valeurs numériques ne correspondent pas
        /// à l'ordre de jeu voulu). Ordre voulu : ♣ → ♦ → ♥ → ♠.
        /// </summary>
        private static readonly Dictionary<CardSuit, int> SuitPriority = new Dictionary<CardSuit, int>
        {
            { CardSuit.Clubs, 0 },
            { CardSuit.Diamonds, 1 },
            { CardSuit.Hearts, 2 },
            { CardSuit.Spades, 3 }
        };

        /// <summary>
        /// Échange une carte de la main contre une carte face-up (1-pour-1).
        /// Retourne false si l'une des deux cartes n'est pas trouvée à l'emplacement attendu.
        /// </summary>
        public static bool SwapHandWithFaceUp(Player player, Card handCard, Card faceUpCard)
        {
            if (player == null)
                return false;

            if (!player.Hand.Contains(handCard))
                return false;

            if (!player.FaceUp.Contains(faceUpCard))
                return false;

            player.RemoveCardFromHand(handCard);
            player.RemoveCardFaceUp(faceUpCard);

            player.AddCardFaceUp(handCard);
            player.AddCardToHand(faceUpCard);

            return true;
        }

        /// <summary>
        /// Marque un joueur comme prêt (fin de ses échanges).
        /// </summary>
        public static void MarkReady(Player player)
        {
            if (player == null)
                return;

            player.IsReady = true;
        }

        /// <summary>
        /// Retourne true si tous les joueurs sont prêts (déclenche un démarrage anticipé).
        /// </summary>
        public static bool AreAllPlayersReady(IEnumerable<Player> players)
        {
            if (players == null)
                return false;

            foreach (Player player in players)
            {
                if (!player.IsReady)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Détermine le joueur qui ouvre la partie : celui possédant la carte la plus faible
        /// EN MAIN (jamais face-up/face-down), selon l'ordre 3♣→3♦→3♥→3♠→4♣→...→As♠.
        /// Les Jokers ne sont jamais la carte la plus faible (ils sont exclus de cette recherche,
        /// car ils ne portent pas de rang standard comparable).
        /// Remonte automatiquement aux rangs supérieurs si aucun rang inférieur n'est trouvé en main.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Si aucun joueur n'a de carte standard en main (cas théoriquement impossible avec 3 cartes/main).
        /// </exception>
        public static Player DetermineFirstPlayer(IEnumerable<Player> players)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            Player bestPlayer = null;
            int bestRank = int.MaxValue;
            int bestSuitPriority = int.MaxValue;

            foreach (Player player in players)
            {
                foreach (Card card in player.Hand)
                {
                    if (card.IsJoker)
                        continue; // Jokers exclus de la comparaison de rang standard

                    int rank = (int)card.Rank;
                    int suitPriority = SuitPriority[card.Suit];

                    bool isBetter =
                        rank < bestRank ||
                        (rank == bestRank && suitPriority < bestSuitPriority);

                    if (isBetter)
                    {
                        bestRank = rank;
                        bestSuitPriority = suitPriority;
                        bestPlayer = player;
                    }
                }
            }

            if (bestPlayer == null)
                throw new InvalidOperationException(
                    "Aucune carte standard trouvée en main parmi les joueurs — impossible de déterminer le premier joueur.");

            return bestPlayer;
        }

        #endregion

        #region Bots AutoSwap Utilities
        
        private static readonly Random _random = new Random();

        /// <summary>
        /// Effectue un swap aléatoire pour un bot durant la phase d'échange pré-jeu.
        /// Simule un comportement "simple" : le bot décide aléatoirement s'il swap (0 à 3 fois),
        /// puis choisit aléatoirement une carte main + une carte face-up à échanger à chaque fois.
        /// Utilise une seed dédiée (via GameSeed.DerivePlayerSeed) pour rester reproductible en simulation.
        /// </summary>
        public static void AutoSwapForBot(Player bot, int seed = -1)
        {
            if (bot == null)
                return;

            Random rng = seed >= 0 ? new Random(seed) : _random;

            // Le bot décide aléatoirement combien de swaps il tente (0 à 3)
            int swapCount = rng.Next(0, 4);

            for (int i = 0; i < swapCount; i++)
            {
                if (bot.Hand.Count == 0 || bot.FaceUp.Count == 0)
                    break;

                Card handCard = bot.Hand[rng.Next(bot.Hand.Count)];
                Card faceUpCard = bot.FaceUp[rng.Next(bot.FaceUp.Count)];

                SwapHandWithFaceUp(bot, handCard, faceUpCard);
            }

            MarkReady(bot);
        }

        /// <summary>
        /// Applique AutoSwapForBot à une collection de bots, en dérivant une seed par bot
        /// depuis une seed racine de partie (reproductibilité totale en simulation de masse).
        /// L'ordre d'itération de players DOIT correspondre à l'ordre des seats (0,1,2...)
        /// utilisé ailleurs (Deck, RandomBot) pour que la dérivation de seed soit cohérente.
        /// </summary>
        public static void AutoSwapForAllBots(IEnumerable<Player> bots, int rootSeed = -1)
        {
            if (bots == null)
                return;

            int seatIndex = 0;
            foreach (Player bot in bots)
            {
                int botSeed = rootSeed >= 0
                    ? Psycko.Core.GameSeed.DerivePlayerSeed(rootSeed, seatIndex)
                    : -1;

                AutoSwapForBot(bot, botSeed);
                seatIndex++;
            }
        }

        #endregion
    }
}
