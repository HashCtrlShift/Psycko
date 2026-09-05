using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Psycko.Console
{
    /// <summary>
    /// Génère les logs coup par coup d'une partie, exportés en CSV au fur et à mesure (append),
    /// et affiche en parallèle sur stdout si demandé (utile en mode interactif).
    /// </summary>
    public class GameLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly bool _echoToConsole;
        private int _coupCounter;

        public int PartieId { get; }
        public int Seed { get; }
        public string FilePath { get; }

        private const string Header =
            "PartieId;Seed;NumCoup;Joueur;Action;Detail;PhaseAvant;PhaseApres;TaillePileAvant;TaillePileApres";

        public GameLogger(int partieId, int seed, string outputDirectory, bool echoToConsole)
        {
            PartieId = partieId;
            Seed = seed;
            _echoToConsole = echoToConsole;
            _coupCounter = 0;

            Directory.CreateDirectory(outputDirectory);
            FilePath = Path.Combine(outputDirectory, $"partie_{partieId}_seed_{seed}.csv");

            bool isNewFile = !File.Exists(FilePath);
            _writer = new StreamWriter(FilePath, append: true, Encoding.UTF8) { AutoFlush = true };

            if (isNewFile)
            {
                _writer.WriteLine(Header);
            }
        }

        /// <summary>
        /// Incrémente et retourne le prochain numéro de coup (partagé entre "Joue" et "Effet" du même coup).
        /// </summary>
        public int NextCoup()
        {
            _coupCounter++;
            return _coupCounter;
        }

        /// <summary>
        /// Log une ligne d'action (coup ou effet dérivé) dans le CSV, et en stdout si activé.
        /// </summary>
        public void LogAction(
            int numCoup,
            string joueur,
            string action,
            string detail,
            string phaseAvant,
            string phaseApres,
            int taillePileAvant,
            int taillePileApres)
        {
            string line = string.Join(";",
                PartieId,
                Seed,
                numCoup,
                Escape(joueur),
                Escape(action),
                Escape(detail),
                phaseAvant,
                phaseApres,
                taillePileAvant,
                taillePileApres);

            _writer.WriteLine(line);

            if (_echoToConsole)
            {
                System.Console.WriteLine($"{joueur} {action} {detail}".Trim());
            }
        }

        /// <summary>
        /// Log une ligne de résumé de fin de partie (vainqueur/perdant, nombre total de coups).
        /// </summary>
        public void LogEndOfGame(string psycko, int totalCoups)
        {
            LogAction(totalCoups + 1, "Système", "FinDePartie",
                $"Psycko={psycko};TotalCoups={totalCoups}",
                "-", "-", 0, 0);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(";") || value.Contains("\""))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        public void Dispose()
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
    }

    /// <summary>
    /// Logger agrégé pour le mode simulation de masse : un fichier récapitulatif (1 ligne par partie),
    /// distinct des logs détaillés par partie (optionnels, activables pour les parties "intéressantes").
    /// </summary>
    public class SimulationSummaryLogger : IDisposable
    {
        private readonly StreamWriter _writer;

        public string FilePath { get; }

        private const string Header = "PartieId;Seed;NombreDeCoups;Psycko;DureeMs";

        public SimulationSummaryLogger(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            FilePath = Path.Combine(outputDirectory, "simulation_recap.csv");

            bool isNewFile = !File.Exists(FilePath);
            _writer = new StreamWriter(FilePath, append: true, Encoding.UTF8) { AutoFlush = true };

            if (isNewFile)
            {
                _writer.WriteLine(Header);
            }
        }

        public void LogGameResult(int partieId, int seed, int nombreDeCoups, string psycko, long dureeMs)
        {
            _writer.WriteLine(string.Join(";", partieId, seed, nombreDeCoups, psycko, dureeMs));
        }

        public void Dispose()
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
    }
}