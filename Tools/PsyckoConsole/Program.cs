#nullable enable

using System;

namespace Psycko.Console
{
    /// <summary>
    /// Point d'entrée unique. Usage : voir ArgumentParser.Usage.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0)
                return ConsoleGameLocal.Run();

            if (!ArgumentParser.TryParse(args, out var configuration, out var error))
            {
                System.Console.WriteLine(error);
                System.Console.WriteLine(ArgumentParser.Usage);
                return 2;
            }

            return Execute(configuration!);
        }

        /// <summary>Point d'exécution commun au CLI et au menu.</summary>
        public static int Execute(RunConfiguration configuration)
        {
            if (configuration.DryRun)
            {
                System.Console.WriteLine(configuration);
                return 0;
            }

            return GameSession.Run(configuration);
        }
    }
}