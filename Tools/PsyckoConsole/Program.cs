using System;
using System.Collections.Generic;
using System.Linq;

namespace Psycko.Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            System.Console.WriteLine("=== Psycko Console ===");
            System.Console.WriteLine("1. Mode interactif (vs bots)");
            System.Console.WriteLine("2. Mode simulation de masse");
            System.Console.WriteLine("3. Replay depuis une seed connue");
            System.Console.Write("Choix: ");

            string choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    RunInteractive();
                    break;
                case "2":
                    RunSimulation();
                    break;
                case "3":
                    RunReplay();
                    break;
                default:
                    System.Console.WriteLine("Choix invalide.");
                    break;
            }
        }

        private static void RunInteractive()
        {
            System.Console.Write("Nombre de joueurs (2-4): ");
            if (!int.TryParse(System.Console.ReadLine(), out int totalPlayers) || totalPlayers < 2 || totalPlayers > 4)
                totalPlayers = 4;

            System.Console.Write("Nombre de joueurs humains (0-4): ");
            if (!int.TryParse(System.Console.ReadLine(), out int humanCount) || humanCount < 0 || humanCount > 4)
                humanCount = 1;

            List<int> humanSeats = new List<int>();
            for (int i = 0; i < humanCount; i++)
            {
                System.Console.Write($"Position (siège 0-{totalPlayers - 1}) du joueur humain #{i + 1}: ");
                if (int.TryParse(System.Console.ReadLine(), out int seat) && seat >= 0 && seat < totalPlayers)
                {
                    humanSeats.Add(seat);
                }
                else
                {
                    humanSeats.Add(i % totalPlayers);
                }
            }

            System.Console.Write("Seed racine (laisser vide pour aléatoire): ");
            string seedInput = System.Console.ReadLine();
            int rootSeed = string.IsNullOrWhiteSpace(seedInput)
                ? new Random().Next(1, int.MaxValue)
                : int.Parse(seedInput);

            try
            {
                var runner = new ConsoleGameRunner(rootSeed, totalPlayers, humanSeats, "Logs/Interactif");
                runner.Run();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERREUR] {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        private static void RunSimulation()
        {
            System.Console.Write("Nombre de parties à simuler: ");
            if (!int.TryParse(System.Console.ReadLine(), out int numberOfGames) || numberOfGames < 1)
                numberOfGames = 1000;

            System.Console.Write("Seed de session (laisser vide pour aléatoire): ");
            string seedInput = System.Console.ReadLine();
            int sessionSeed = string.IsNullOrWhiteSpace(seedInput)
                ? new Random().Next(1, int.MaxValue)
                : int.Parse(seedInput);

            try
            {
                var runner = new ConsoleSimulationRunner(numberOfGames, sessionSeed, "Logs/Simulation");
                runner.Run();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERREUR] {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        private static void RunReplay()
        {
            System.Console.Write("Seed racine de la partie à rejouer: ");
            if (!int.TryParse(System.Console.ReadLine(), out int rootSeed))
                rootSeed = 12345;

            System.Console.WriteLine("Replay 100% bots (déterministe) — affichage compact:");

            try
            {
                var runner = new ConsoleGameRunner(rootSeed, 4, new List<int>(), "Logs/Replay");
                runner.Run();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERREUR] {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }
}