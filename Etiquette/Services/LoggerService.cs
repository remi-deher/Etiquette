using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Etiquette.Services
{
    public enum LogLevel { Info, Warning, Error, Success }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
        public string? StackTrace { get; set; }
    }

    public class LoggerService
    {
        // Singleton
        public static LoggerService Current { get; } = new LoggerService();

        // Logs de la session en cours (en mémoire RAM)
        public ObservableCollection<LogEntry> LiveLogs { get; } = new();

        private readonly string _logFolder;

        private LoggerService()
        {
            // Dossier : %LocalAppData%\Etiquette\Logs
            _logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Etiquette", "Logs");
            Directory.CreateDirectory(_logFolder);
        }

        // --- ÉCRITURE ---

        public void Log(string message, LogLevel level = LogLevel.Info, Exception? ex = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                StackTrace = ex?.ToString()
            };

            // 1. Mise à jour de l'UI (Thread Principal)
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                LiveLogs.Insert(0, entry);
                // On garde seulement les 500 derniers logs en mémoire vive pour ne pas surcharger
                if (LiveLogs.Count > 500) LiveLogs.RemoveAt(LiveLogs.Count - 1);
            });

            // 2. Écriture Fichier (Tâche de fond)
            Task.Run(() => WriteToFile(entry));
        }

        private void WriteToFile(LogEntry entry)
        {
            try
            {
                // Nom du fichier : logs_2023-10-27.txt
                string fileName = $"logs_{DateTime.Now:yyyy-MM-dd}.txt";
                string fullPath = Path.Combine(_logFolder, fileName);

                // Format simple à parser : ISO_DATE|LEVEL|MESSAGE|STACK
                // On remplace les sauts de ligne dans le stacktrace pour tenir sur une ligne (facilite la lecture)
                string safeStack = entry.StackTrace?.Replace(Environment.NewLine, "  ") ?? "";
                string line = $"{entry.Timestamp:O}|{entry.Level}|{entry.Message}|{safeStack}";

                File.AppendAllText(fullPath, line + Environment.NewLine);
            }
            catch { /* Ignorer les erreurs d'écriture de log pour ne pas crasher l'app */ }
        }

        // --- LECTURE (HISTORIQUE) ---

        public List<string> GetAvailableLogFiles()
        {
            if (!Directory.Exists(_logFolder)) return new List<string>();

            // Retourne les noms de fichiers triés par date décroissante (plus récents en premier)
            return Directory.GetFiles(_logFolder, "logs_*.txt")
                            .Select(Path.GetFileName)
                            .OrderByDescending(x => x)
                            .ToList();
        }

        public async Task<List<LogEntry>> ReadLogFileAsync(string fileName)
        {
            var results = new List<LogEntry>();
            string fullPath = Path.Combine(_logFolder, fileName);

            if (!File.Exists(fullPath)) return results;

            try
            {
                var lines = await File.ReadAllLinesAsync(fullPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        if (DateTime.TryParse(parts[0], out DateTime date) &&
                            Enum.TryParse(parts[1], out LogLevel level))
                        {
                            results.Add(new LogEntry
                            {
                                Timestamp = date,
                                Level = level,
                                Message = parts[2],
                                StackTrace = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null
                            });
                        }
                    }
                }
                // On inverse pour afficher les logs les plus récents en haut de la liste
                results.Reverse();
            }
            catch (Exception) { /* Fichier corrompu ou illisible */ }

            return results;
        }
    }
}