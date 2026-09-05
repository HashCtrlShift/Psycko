using System;
using System.Collections.Generic;
using System.Linq;
using Psycko;
using Psycko.Core;

namespace Psycko.Console
{
    /// <summary>
    /// Mode interactif : nombre de joueurs humains configurable (0-4) et leur position à la table.
    /// Le reste des sièges est comblé par des RandomBot.
    /// Toute la partie est déterministe si une seed racine est fournie (GameSeed).
    /// </summary>
    public class ConsoleGameRunner
    {
        private readonly GameState _gameState;
        private readonly List<Player> _players;
        private readonly HashSet<string> _humanPlayerIds;
        private readonly Dictionary<string, IPlayerAgent> _agents;
        private readonly GameLogger _logger;
        private readonly int _rootSeed;

        public ConsoleGameRunner(int rootSeed, int totalPlayers, IReadOnlyList<int> humanSeatIndices, string outputDirectory)
        {
            if (totalPlayers < 2 || totalPlayers > 4)
                throw new ArgumentOutOfRangeException(nameof(totalPlayers), "Doit être entre 2 et 4.");

            _rootSeed = rootSeed;
            _players = new List<Player>();
            _humanPlayerIds = new HashSet<string>();
            _agents = new Dictionary<string, IPlayerAgent>();

            int deckSeed = GameSeed.DeriveDeckSeed(rootSeed);
            Deck deck = new Deck(deckSeed);

            for (int seat = 0; seat < totalPlayers; seat++)
            {
                bool isHuman = humanSeatIndices.Contains(seat);
                string id = $"P{seat}";
                string name = isHuman ? $"Toi(P{seat})" : $"Bot{seat}";

                Player player = new Player(id, name);
                _players.Add(player);

                if (isHuman)
                {
                    _humanPlayerIds.Add(id);
                }
                else
                {
                    int botSeed = GameSeed.DerivePlayerSeed(rootSeed, seat);
                    _agents[id] = new RandomBot(botSeed);
                }
            }

            // 1. Distribution AVANT de déterminer le premier joueur
            //    (9 cartes/joueur : 3 face-down, 3 face-up, 3 main)
            foreach (Player p in _players)
            {
                for (int i = 0; i < 3; i++) p.AddCardFaceDown(deck.Draw());
                for (int i = 0; i < 3; i++) p.AddCardFaceUp(deck.Draw());
                for (int i = 0; i < 3; i++) p.AddCardToHand(deck.Draw());
            }

            // 2. Échange pré-jeu bots (les humains swappent via prompt séparé si voulu)
            PreGameSwap.AutoSwapForAllBots(_players.Where(p => !_humanPlayerIds.Contains(p.Id)), _rootSeed);

            // 3. Déterminer le premier joueur AVANT de construire GameState
            Player firstPlayer = PreGameSwap.DetermineFirstPlayer(_players);
            var turnManager = new TurnManager(_players, firstPlayer);

            // 4. Construire GameState avec ce TurnManager déjà positionné
            Pile pile = new Pile();
            _gameState = GameState.CreateWithTurnManager(_players, pile, deck, turnManager);

            _logger = new GameLogger(partieId: 1, seed: rootSeed, outputDirectory, echoToConsole: true);
        }

        public void Run()
        {
            System.Console.WriteLine($"=== Nouvelle partie interactive — Seed racine: {_rootSeed} ===");
            System.Console.WriteLine($"{_gameState.TurnManager.CurrentTurn.CurrentPlayer.Name} commence la partie.");

            while (_gameState.TurnManager.GetActivePlayerCount() > 1)
            {
                Player player = _gameState.TurnManager.CurrentTurn.CurrentPlayer;
                GamePhase phaseAvant = _gameState.CurrentPhase;
                int pileAvant = _gameState.Pile.Count;
                int numCoup = _logger.NextCoup();

                // ✅ CORRECTION #4 : Récupérer les cartes jouables AVANT d'appeler ChooseCard
                Card chosenCard;
                if (_humanPlayerIds.Contains(player.Id))
                {
                    chosenCard = PromptHumanCard(player);
                }
                else
                {
                    // Bot : récupérer legal cards PUIS appeler ChooseCard avec 3 arguments
                    List<Card> legalCards = RandomBot.GetLegalCards(player, _gameState);
                    chosenCard = _agents[player.Id].ChooseCard(player, _gameState, legalCards);
                }

                bool success = _gameState.PlayCard(player, chosenCard);

                if (!success)
                {
                    System.Console.WriteLine($"[Invalide] {player.Name} a tenté {chosenCard} — refusé.");
                    continue;
                }

                _logger.LogAction(
                    numCoup,
                    player.Name,
                    "Joue",
                    chosenCard.ToString(),
                    phaseAvant.ToString(),
                    _gameState.CurrentPhase.ToString(),
                    pileAvant,
                    _gameState.Pile.Count);
            }

            string psycko = _gameState.TurnManager.GetLoser()?.Name ?? "Inconnu";
            System.Console.WriteLine($"=== Partie terminée — Psycko : {psycko} ===");
            _logger.LogEndOfGame(psycko, _logger.NextCoup() - 1);
            _logger.Dispose();
        }

        private Card PromptHumanCard(Player player)
        {
            System.Console.WriteLine($"\n--- Tour de {player.Name} ---");
            System.Console.WriteLine($"Pile (sommet): {(_gameState.Pile.Count > 0 ? _gameState.Pile.Top().ToString() : "vide")}");
            System.Console.WriteLine($"Ta main: {string.Join(", ", player.Hand.Select((c, i) => $"[{i}]{c}"))}");

            while (true)
            {
                System.Console.Write("Choisis l'index de la carte à jouer: ");
                string input = System.Console.ReadLine();

                if (int.TryParse(input, out int index) && index >= 0 && index < player.Hand.Count)
                {
                    return player.Hand[index];
                }

                System.Console.WriteLine("Entrée invalide, réessaie.");
            }
        }
    }
}