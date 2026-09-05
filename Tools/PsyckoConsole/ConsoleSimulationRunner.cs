using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Psycko;
using Psycko.Core;

namespace Psycko.Console
{
    /// <summary>
    /// Mode simulation de masse : joue N parties 100% bots (aucune interaction humaine).
    /// Chaque partie est déterministe (seed dérivée depuis une seed de session).
    /// Résumé dans simulation_recap.csv, logs détaillés dans partie_*.csv si besoin.
    /// </summary>
    public class ConsoleSimulationRunner
    {
        private readonly int _numberOfGames;
        private readonly int _sessionSeed;
        private readonly string _outputDirectory;
        private readonly SimulationSummaryLogger _summaryLogger;

        public ConsoleSimulationRunner(int numberOfGames, int sessionSeed, string outputDirectory)
        {
            if (numberOfGames < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfGames), "Au moins 1 partie.");

            _numberOfGames = numberOfGames;
            _sessionSeed = sessionSeed;
            _outputDirectory = outputDirectory;
            _summaryLogger = new SimulationSummaryLogger(outputDirectory);
        }

        public void Run()
        {
            System.Console.WriteLine($"=== Simulation de {_numberOfGames} parties — Seed session: {_sessionSeed} ===");
            System.Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();
            int completedGames = 0;

            for (int partieId = 1; partieId <= _numberOfGames; partieId++)
            {
                // Dérive une seed unique pour cette partie depuis la seed de session
                int rootSeed = unchecked(_sessionSeed * 31 + partieId);
                rootSeed = rootSeed & 0x7FFFFFFF; // Évite les seeds négatives

                var gameStopwatch = Stopwatch.StartNew();

                try
                {
                    // Joue une partie 100% bots (4 joueurs, aucun humain)
                    var runner = new ConsoleGameRunner(rootSeed, 4, new List<int>(), _outputDirectory);
                    int numberOfMoves = SimulateGame(runner);

                    gameStopwatch.Stop();

                    _summaryLogger.LogGameResult(
                        partieId,
                        rootSeed,
                        numberOfMoves,
                        runner._gameState.TurnManager.GetLoser()?.Name ?? "Inconnu",
                        gameStopwatch.ElapsedMilliseconds);

                    completedGames++;

                    if (partieId % 100 == 0 || partieId == _numberOfGames)
                    {
                        System.Console.WriteLine($"[{partieId}/{_numberOfGames}] {completedGames} parties complétées");
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ERREUR] Partie {partieId} (seed {rootSeed}): {ex.Message}");
                    System.Console.WriteLine($"  {ex.StackTrace}");
                }
            }

            stopwatch.Stop();

            System.Console.WriteLine();
            System.Console.WriteLine($"=== Simulation terminée ===");
            System.Console.WriteLine($"Parties complétées: {completedGames}/{_numberOfGames}");
            System.Console.WriteLine($"Durée totale: {stopwatch.Elapsed.TotalSeconds:F2}s");
            System.Console.WriteLine($"Logs récapitulatifs: {_summaryLogger.FilePath}");

            _summaryLogger.Dispose();
        }

        /// <summary>
        /// Simule une partie et retourne le nombre de coups joués.
        /// Gère les erreurs : si une erreur est levée, la retourne à Run() pour log.
        /// </summary>
        private int SimulateGame(ConsoleGameRunner runner)
        {
            // Accès direct aux champs privés via réflexion (hack simulation)
            // Mieux : rendre _gameState public ou passer par une interface
            var gameStateField = typeof(ConsoleGameRunner).GetField("_gameState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var agentsField = typeof(ConsoleGameRunner).GetField("_agents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            GameState gameState = (GameState)gameStateField?.GetValue(runner);
            Dictionary<string, IPlayerAgent> agents = (Dictionary<string, IPlayerAgent>)agentsField?.GetValue(runner);

            if (gameState == null || agents == null)
                throw new InvalidOperationException("Impossible d'accéder à _gameState ou _agents via réflexion.");

            int moveCount = 0;

            while (gameState.TurnManager.GetActivePlayerCount() > 1)
            {
                Player player = gameState.TurnManager.CurrentTurn.CurrentPlayer;

                if (!agents.ContainsKey(player.Id))
                    throw new InvalidOperationException($"Agent manquant pour {player.Name}");

                List<Card> legalCards = RandomBot.GetLegalCards(player, gameState);
                Card chosenCard = agents[player.Id].ChooseCard(player, gameState, legalCards);

                if (chosenCard == null)
                    throw new InvalidOperationException($"{player.Name} a retourné null (ramassage obligatoire)."); // À gérer si ramassage automatique existe

                bool success = gameState.PlayCard(player, chosenCard);

                if (!success)
                    throw new InvalidOperationException($"Pose invalide : {player.Name} a joué {chosenCard} illégalement.");

                moveCount++;

                // Sécurité : évite les boucles infinies (limite arbitraire 10k coups/partie)
                if (moveCount > 10000)
                    throw new InvalidOperationException("Partie excède 10 000 coups — boucle infinie ?");
            }

            return moveCount;
        }
    }
}