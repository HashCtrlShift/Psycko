using System;
using Psycko;
using Psycko.Core;
using Psycko.Bots;

namespace Psycko.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("=== Psycko Console ===");
            System.Console.WriteLine("Mode interactif uniquement (vs bots)");
            System.Console.WriteLine();

            System.Console.Write("Nombre de joueurs (2-4): ");
            if (!int.TryParse(System.Console.ReadLine(), out int playerCount) || playerCount < 2 || playerCount > 4)
            {
                System.Console.WriteLine("Entrée invalide.");
                return;
            }

            System.Console.Write("Nombre de joueurs humains (0-N): ");
            if (!int.TryParse(System.Console.ReadLine(), out int humanCount) || humanCount < 0 || humanCount > playerCount)
            {
                System.Console.WriteLine("Entrée invalide.");
                return;
            }

            var humanSeats = new List<int>();
            for (int i = 0; i < humanCount; i++)
            {
                System.Console.Write($"Position (siège 0-{playerCount - 1}) du joueur humain #{i + 1}: ");
                if (!int.TryParse(System.Console.ReadLine(), out int seat) || seat < 0 || seat >= playerCount)
                {
                    System.Console.WriteLine("Entrée invalide.");
                    return;
                }
                humanSeats.Add(seat);
            }

            System.Console.Write("Seed racine (laisser vide pour aléatoire): ");
            string seedInput = System.Console.ReadLine() ?? "";
            int rootSeed = string.IsNullOrWhiteSpace(seedInput) ? new Random().Next() : int.Parse(seedInput);

            System.Console.WriteLine();
            System.Console.WriteLine($"=== Nouvelle partie interactive — Seed racine: {rootSeed} ===");

            // ✅ À faire : créer ConsoleGameRunner simplifié ou appeler GameState directement
            // Pour l'instant, on peut faire un test simple :
            var players = new List<Player>();
            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new Player($"Player{i}", i < humanCount ? $"Human{i}" : $"Bot{i}"));
            }

            var gameState = new GameState(players, new Pile(), new Deck(rootSeed));
            System.Console.WriteLine($"Partie créée. Phase: {gameState.CurrentPhase}, {gameState.Players.Count} joueurs.");
            System.Console.WriteLine("TODO: intégrer gameplay interactif ici");
        }
    }
}