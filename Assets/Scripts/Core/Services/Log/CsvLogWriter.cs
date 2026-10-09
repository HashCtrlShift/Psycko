using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Psycko.Core.Domain.Log;
using Psycko.Core.Interfaces;
using Psycko.Core.Services.Log.Format;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Écrit des GameLog en CSV (UTF-8 BOM, séparateur ';', compatible Excel).
    /// Colonnes : A-B = bilan ; puis 4 colonnes par partie
    /// (Action | Pile avant | Effets | Détail).
    /// </summary>
    internal static class CsvLogWriter
    {
        /// <summary>Colonnes Excel max (16 384) - 2 de bilan = 16 382 ; / 4 = 4 095 ; plafond retenu 4 000.</summary>
        public const int MaxGamesPerFile = 4000;

        private const int SummaryColumns = 2;
        private const int ColumnsPerGame = 4;

        /// <summary>
        /// Exporte les parties en un ou plusieurs fichiers de gamesPerFile parties maximum.
        /// results peut être null (pas de bilan) ; sinon aligné sur gameLogs.
        /// cardFormatter peut être null : CardFormatter (français) est alors utilisé.
        /// Retourne les chemins écrits.
        /// </summary>
        public static IReadOnlyList<string> WriteToFiles(
            IReadOnlyList<GameLog> gameLogs,
            IReadOnlyList<GameResult> results,
            string directory,
            int gamesPerFile = MaxGamesPerFile,
            ICardFormatter cardFormatter = null)
        {
            if (gameLogs == null) throw new ArgumentNullException(nameof(gameLogs));
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (gamesPerFile < 1 || gamesPerFile > MaxGamesPerFile)
                throw new ArgumentOutOfRangeException(nameof(gamesPerFile));
            if (results != null && results.Count != gameLogs.Count)
                throw new ArgumentException("results doit être null ou aligné sur gameLogs.", nameof(results));

            var formatter = new CsvLogFormatter(cardFormatter ?? new CardFormatter());

            Directory.CreateDirectory(directory);

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var paths = new List<string>();

            for (int offset = 0, part = 1; offset < gameLogs.Count; offset += gamesPerFile, part++)
            {
                int count = Math.Min(gamesPerFile, gameLogs.Count - offset);
                int firstSeed = gameLogs[offset].Seed;
                int lastSeed = gameLogs[offset + count - 1].Seed;

                string fileName = string.Format(CultureInfo.InvariantCulture,
                    "GameLogs_{0}_{1}_{2}_{3}.csv", firstSeed, lastSeed, stamp, part);
                string path = Path.Combine(directory, fileName);

                using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
                {
                    WriteFile(writer, formatter, gameLogs, results, offset, count);
                }

                paths.Add(path);
            }

            return paths.AsReadOnly();
        }

        private static void WriteFile(TextWriter writer, CsvLogFormatter formatter,
            IReadOnlyList<GameLog> logs, IReadOnlyList<GameResult> results,
            int offset, int count)
        {
            int width = SummaryColumns + ColumnsPerGame * count;

            // Colonnes de bilan : A = stats agrégées, B = détail par partie.
            var colA = BuildSummaryA(results, offset, count);
            var colB = BuildSummaryB(results, logs, offset, count);

            // Ligne d'en-têtes
            var header = new string[width];
            header[0] = "Bilan (étape 5)";
            header[1] = "Bilan (T41)";
            for (int i = 0; i < count; i++)
            {
                int c = SummaryColumns + i * ColumnsPerGame;
                header[c] = "Action (seed " + logs[offset + i].Seed.ToString(CultureInfo.InvariantCulture) + ")";
                header[c + 1] = "Pile avant";
                header[c + 2] = "Effets";
                header[c + 3] = "Détail";
            }
            writer.WriteLine(Csv(header));

            int maxEntries = Enumerable.Range(offset, count)
                .Select(i => logs[i].Entries.Count)
                .DefaultIfEmpty(0)
                .Max();
            int rows = Math.Max(maxEntries, Math.Max(colA.Count, colB.Count));

            for (int row = 0; row < rows; row++)
            {
                var cells = new string[width];
                cells[0] = row < colA.Count ? colA[row] : string.Empty;
                cells[1] = row < colB.Count ? colB[row] : string.Empty;

                for (int i = 0; i < count; i++)
                {
                    var entries = logs[offset + i].Entries;
                    if (row >= entries.Count) continue;

                    var entry = entries[row];
                    int c = SummaryColumns + i * ColumnsPerGame;
                    cells[c] = formatter.Format(entry);
                    cells[c + 1] = formatter.FormatCards(entry.PileBefore);
                    cells[c + 2] = formatter.FormatEffects(entry.Effects);
                    cells[c + 3] = entry.Detail ?? string.Empty;
                }

                writer.WriteLine(Csv(cells));
            }
        }

        /// <summary>Colonne A : agrégats du fichier (parties, erreurs, coups moy/méd/min/max).</summary>
        private static List<string> BuildSummaryA(IReadOnlyList<GameResult> results, int offset, int count)
        {
            var lines = new List<string> { "Parties : " + count };
            if (results == null) return lines;

            var slice = Enumerable.Range(offset, count)
                .Select(i => results[i])
                .Where(r => r != null)
                .ToList();
            lines.Add("Parties OK : " + slice.Count(r => !r.IsError));
            lines.Add("Parties en erreur : " + slice.Count(r => r.IsError));

            var moves = slice.Where(r => !r.IsError)
                .Select(r => (double)r.TotalMoves)
                .OrderBy(x => x)
                .ToList();
            if (moves.Count > 0)
            {
                lines.Add("Coups moyenne : " + F(moves.Average()));
                lines.Add("Coups médiane : " + F(Median(moves)));
                lines.Add("Coups min : " + F(moves.First()));
                lines.Add("Coups max : " + F(moves.Last()));
            }
            return lines;
        }

        /// <summary>Colonne B (emplacement T41) : une ligne par partie avec ses compteurs.</summary>
        private static List<string> BuildSummaryB(IReadOnlyList<GameResult> results,
            IReadOnlyList<GameLog> logs, int offset, int count)
        {
            var lines = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string line = "seed " + logs[offset + i].Seed.ToString(CultureInfo.InvariantCulture);
                var r = results?[offset + i];
                if (r != null)
                {
                    line += string.Format(CultureInfo.InvariantCulture,
                        " : coups={0}, plays={1}, classement={2}, ramassages={3}{4}",
                        r.TotalMoves, r.TotalPlays,
                        string.Join(",", r.RankingBySeat),
                        string.Join(",", r.PickupsBySeat),
                        r.IsError ? " [ERREUR]" : string.Empty);
                }
                lines.Add(line);
            }
            return lines;
        }

        private static double Median(IList<double> sorted)
            => sorted.Count % 2 == 1
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;

        private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Csv(IEnumerable<string> values)
            => string.Join(";", values.Select(v =>
            {
                v = v ?? string.Empty;
                return v.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0
                    ? "\"" + v.Replace("\"", "\"\"") + "\""
                    : v;
            }));
    }
}