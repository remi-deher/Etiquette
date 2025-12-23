using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etiquette.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Etiquette.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        // Liste finale affichée dans la ListView
        public ObservableCollection<LogEntry> DisplayedLogs { get; } = new();

        // Liste des fichiers (Session + Archives)
        public ObservableCollection<string> FileList { get; } = new();

        // --- SÉLECTION DU FICHIER ---

        private string _selectedFile = "Session actuelle";
        public string SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    _ = LoadSelectedLogs();
                }
            }
        }

        // --- FILTRES ---

        private bool _showInfo = true;
        public bool ShowInfo { get => _showInfo; set { SetProperty(ref _showInfo, value); FilterLocalList(); } }

        private bool _showWarning = true;
        public bool ShowWarning { get => _showWarning; set { SetProperty(ref _showWarning, value); FilterLocalList(); } }

        private bool _showError = true;
        public bool ShowError { get => _showError; set { SetProperty(ref _showError, value); FilterLocalList(); } }

        private bool _showSuccess = true;
        public bool ShowSuccess { get => _showSuccess; set { SetProperty(ref _showSuccess, value); FilterLocalList(); } }

        // Tampon contenant tous les logs chargés (avant filtrage)
        private List<LogEntry> _allLoadedLogs = new();

        public LogsViewModel()
        {
            // 1. Remplir la liste des sources
            FileList.Add("Session actuelle");
            var files = LoggerService.Current.GetAvailableLogFiles();
            foreach (var f in files) FileList.Add(f);

            // 2. S'abonner aux nouveaux logs (Live)
            LoggerService.Current.LiveLogs.CollectionChanged += (s, e) =>
            {
                if (SelectedFile == "Session actuelle")
                {
                    // Mise à jour simplifiée : on recharge tout depuis la source Live
                    _allLoadedLogs = LoggerService.Current.LiveLogs.ToList();
                    FilterLocalList();
                }
            };

            // 3. Chargement initial
            _ = LoadSelectedLogs();
        }

        private async Task LoadSelectedLogs()
        {
            if (SelectedFile == "Session actuelle")
            {
                _allLoadedLogs = LoggerService.Current.LiveLogs.ToList();
            }
            else
            {
                // Lecture asynchrone du fichier
                _allLoadedLogs = await LoggerService.Current.ReadLogFileAsync(SelectedFile);
            }
            FilterLocalList();
        }

        private void FilterLocalList()
        {
            // Application des filtres booléens
            var query = _allLoadedLogs.Where(l =>
                (l.Level == LogLevel.Info && ShowInfo) ||
                (l.Level == LogLevel.Warning && ShowWarning) ||
                (l.Level == LogLevel.Error && ShowError) ||
                (l.Level == LogLevel.Success && ShowSuccess)
            );

            // Mise à jour de l'UI
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                DisplayedLogs.Clear();
                foreach (var log in query)
                {
                    DisplayedLogs.Add(log);
                }
            });
        }

        [RelayCommand]
        public void ClearLogs()
        {
            // On ne vide que la mémoire vive, pas les fichiers d'historique (sécurité)
            if (SelectedFile == "Session actuelle")
            {
                LoggerService.Current.LiveLogs.Clear();
            }
        }
    }
}