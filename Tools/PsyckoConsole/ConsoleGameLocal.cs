#nullable enable

using System;
using Psycko.Core.Domain.Log;
using System.Diagnostics.CodeAnalysis;

namespace Psycko.Console
{
    /// <summary>Menu interactif : construit une RunConfiguration puis la transmet à GameSession.</summary>
    public static class ConsoleGameLocal
    {
        public static int Run()
        {
            System.Console.WriteLine("=== Psycko Console ===");
            System.Console.WriteLine("  [1] Simulation de parties");
            System.Console.WriteLine("  [2] Partie Humain vs 3 Bots");
            string choice = Ask("Choix", "1", x => x == "1" || x == "2");

            string logText = Ask("Logs (off / all / exceptional)", "all",
                x => ArgumentParser.TryParseLogMode(x, out _, out _));

            var args = new System.Collections.Generic.List<string>();

            if (choice == "2")
            {
                string seed = Ask("Seed (vide = aléatoire)", "",
                    x => x.Length == 0 || ArgumentParser.TryParseInt(x, "Seed", int.MinValue, int.MaxValue, out _, out _));
                bool reveal = Ask("Révéler les mains des bots (o/n)", "n", x => x == "o" || x == "n") == "o";

                args.Add("play");
                if (seed.Length > 0) args.Add(seed);
                args.Add(logText);
                if (reveal) args.Add("--reveal");
            }
            else
            {
                string games = Ask("Nombre de parties", "1000",
                    x => ArgumentParser.TryParseInt(x, "Nombre de parties", 1, int.MaxValue, out _, out _));
                string start = Ask("Seed de départ", "1",
                    x => ArgumentParser.TryParseInt(x, "Seed", int.MinValue, int.MaxValue, out _, out _));

                args.Add("sim");
                args.Add(games);
                args.Add(start);
                args.Add(logText);
            }

            if (!ArgumentParser.TryParse(args.ToArray(), out var configuration, out var error))
            {
                System.Console.WriteLine(error);
                return 2;
            }

            return Program.Execute(configuration);
        }

        private static string Ask(string label, string def, Func<string, bool> isValid)
        {
            while (true)
            {
                System.Console.Write(def.Length > 0 ? $"{label} [{def}] > " : $"{label} > ");
                string input = System.Console.ReadLine()?.Trim() ?? "";
                if (input.Length == 0) input = def;
                if (isValid(input)) return input;
                System.Console.WriteLine("Saisie invalide.");
            }
        }
    }
}