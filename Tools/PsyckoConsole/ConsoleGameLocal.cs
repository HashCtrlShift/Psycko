#nullable enable

using System;

namespace Psycko.Console
{
    /// <summary>Menu interactif : simulation ou partie Humain vs Bots.</summary>
    public static class ConsoleGameLocal
    {
        public static int Run()
        {
            System.Console.WriteLine("=== Psycko Console ===");
            System.Console.WriteLine("  [1] Simulation de parties");
            System.Console.WriteLine("  [2] Partie Humain vs 3 Bots");
            string choice = Prompt("Choix", "1");

            string logMode = Prompt("Logs (off / all / exceptional)", "all");

            if (choice == "2")
            {
                string seed = Prompt("Seed (vide = aléatoire)", "");
                bool reveal = Prompt("Révéler les mains des bots (o/n)", "n").ToLowerInvariant() == "o";
                var args = new System.Collections.Generic.List<string> { "play" };
                if (seed.Length > 0) args.Add(seed);
                else args.Add(Environment.TickCount.ToString());
                args.Add(logMode);
                if (reveal) args.Add("--reveal");
                return Program.Main(args.ToArray());
            }

            string games = Prompt("Nombre de parties", "1000");
            string start = Prompt("Seed de départ", "1");
            return Program.Main(new[] { "sim", games, start, logMode });
        }

        private static string Prompt(string label, string def)
        {
            System.Console.Write(def.Length > 0 ? $"{label} [{def}] > " : $"{label} > ");
            string? input = System.Console.ReadLine()?.Trim();
            return string.IsNullOrEmpty(input) ? def : input!;
        }
    }
}