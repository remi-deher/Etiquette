using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etiquette.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace Etiquette.ViewModels
{
    public class AppModeItem
    {
        public string Key { get; set; }
        public string Label { get; set; }
    }

    public partial class SettingsViewModel : ObservableObject
    {
        // --- PROPRIÉTÉS EXISTANTES ---
        private string _dbServer = "";
        public string DbServer { get => _dbServer; set => SetProperty(ref _dbServer, value); }

        private string _dbName = "";
        public string DbName { get => _dbName; set => SetProperty(ref _dbName, value); }

        private string _dbUser = "";
        public string DbUser { get => _dbUser; set => SetProperty(ref _dbUser, value); }

        private string _dbPassword = "";
        public string DbPassword { get => _dbPassword; set => SetProperty(ref _dbPassword, value); }

        private string _selectedSslMode = "None";
        public string SelectedSslMode { get => _selectedSslMode; set => SetProperty(ref _selectedSslMode, value); }

        private string _selectedPrinter = "";
        public string SelectedPrinter { get => _selectedPrinter; set => SetProperty(ref _selectedPrinter, value); }

        // --- GESTION DU MODE APPLICATIF ---
        private string _selectedAppMode = "MultiPoste";
        public string SelectedAppMode
        {
            get => _selectedAppMode;
            set
            {
                if (SetProperty(ref _selectedAppMode, value))
                {
                    OnPropertyChanged(nameof(IsPairingAllowed));
                    OnPropertyChanged(nameof(IsDbConfigVisible)); // Met à jour la visibilité de l'assistant

                    if (!IsPairingAllowed && IsPairingActive)
                    {
                        TogglePairing();
                    }
                }
            }
        }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // --- GESTION APPAIRAGE ---
        public bool IsPairingAllowed => SelectedAppMode != "Standalone";

        // Nouvelle propriété pour la visibilité de l'assistant BDD (Caché en Monoposte)
        public bool IsDbConfigVisible => SelectedAppMode != "Standalone";

        private int _selectedDurationIndex = 1;
        public int SelectedDurationIndex { get => _selectedDurationIndex; set => SetProperty(ref _selectedDurationIndex, value); }

        private bool _isPairingActive = false;
        public bool IsPairingActive { get => _isPairingActive; set => SetProperty(ref _isPairingActive, value); }

        private string _pairingStatusText = "Le partage est désactivé.";
        public string PairingStatusText { get => _pairingStatusText; set => SetProperty(ref _pairingStatusText, value); }

        // --- NOUVEAU : GESTION ASSISTANT BDD MULTI-MOTEUR ---

        private string _selectedDbType = "MariaDB / MySQL";
        public string SelectedDbType { get => _selectedDbType; set => SetProperty(ref _selectedDbType, value); }

        private string _sqlOutput = "";
        public string SqlOutput { get => _sqlOutput; set => SetProperty(ref _sqlOutput, value); }

        public List<string> DbTypes { get; } = new() { "MariaDB / MySQL", "PostgreSQL", "Microsoft SQL Server", "Oracle" };

        // --- LISTES ---
        public List<string> SslModes { get; } = new() { "None", "Preferred", "Required", "VerifyCA", "VerifyFull" };
        public ObservableCollection<string> InstalledPrinters { get; } = new();

        public ObservableCollection<AppModeItem> AppModes { get; } = new()
        {
            new AppModeItem { Key = "Standalone", Label = "Monoposte (Local)" },
            new AppModeItem { Key = "MultiPoste", Label = "Multi poste (Réseau)" },
            new AppModeItem { Key = "Scanette",   Label = "Avec Scanette (Hub)" }
        };

        public SettingsViewModel()
        {
            DbServer = AppSettings.DbServer;
            DbName = AppSettings.DbName;
            DbUser = AppSettings.DbUser;
            DbPassword = AppSettings.DbPassword;
            SelectedSslMode = string.IsNullOrEmpty(AppSettings.DbSslMode) ? "None" : AppSettings.DbSslMode;
            SelectedAppMode = AppSettings.AppMode;

            LoadPrinters(AppSettings.PrinterName);
            CheckServerStatus();
        }

        private void CheckServerStatus()
        {
            if (App.HttpServer != null && App.HttpServer.IsPairingActive)
            {
                IsPairingActive = true;
                PairingStatusText = "Le partage est déjà actif.";
            }
        }

        private void LoadPrinters(string savedPrinter)
        {
            InstalledPrinters.Clear();
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters) InstalledPrinters.Add(printer);
                if (!string.IsNullOrEmpty(savedPrinter) && InstalledPrinters.Contains(savedPrinter)) SelectedPrinter = savedPrinter;
                else if (InstalledPrinters.Count > 0) SelectedPrinter = InstalledPrinters[0];
                else SelectedPrinter = "Aucune imprimante";
            }
            catch { SelectedPrinter = "Erreur lecture"; }
        }

        [RelayCommand]
        public void SaveSettings()
        {
            AppSettings.DbServer = DbServer;
            AppSettings.DbName = DbName;
            AppSettings.DbUser = DbUser;
            AppSettings.DbPassword = DbPassword;
            AppSettings.DbSslMode = SelectedSslMode;
            AppSettings.PrinterName = SelectedPrinter;
            AppSettings.AppMode = SelectedAppMode;
            StatusMessage = "Paramètres enregistrés !";
        }

        [RelayCommand]
        public void RestartWizard()
        {
            var mainWindow = (Microsoft.UI.Xaml.Application.Current as App)?.m_window as MainWindow;
            mainWindow?.StartWizard();
        }

        [RelayCommand]
        public void TogglePairing()
        {
            var server = App.HttpServer;
            if (server == null) return;

            if (IsPairingActive)
            {
                server.StopPairing();
                PairingStatusText = "Le partage est désactivé.";
                IsPairingActive = false;
            }
            else
            {
                int minutes = 5;
                switch (SelectedDurationIndex) { case 0: minutes = 5; break; case 1: minutes = 10; break; case 2: minutes = 15; break; case 3: minutes = 30; break; }

                server.StartPairing(minutes);
                PairingStatusText = $"Partage actif pour {minutes} minutes.";
                IsPairingActive = true;

                server.PairingStatusChanged += (s, active) =>
                {
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        IsPairingActive = active;
                        if (!active) PairingStatusText = "Délai écoulé, partage désactivé.";
                    });
                };
            }
        }

        // --- LOGIQUE GÉNÉRATION SQL ---
        [RelayCommand]
        public void GenerateSql()
        {
            if (string.IsNullOrWhiteSpace(DbName) || string.IsNullOrWhiteSpace(DbUser) || string.IsNullOrWhiteSpace(DbPassword))
            {
                SqlOutput = "-- Veuillez remplir les champs Nom BDD, Utilisateur et Mot de passe ci-dessus.";
                return;
            }

            string sql = "";

            switch (SelectedDbType)
            {
                case "MariaDB / MySQL":
                    sql = $"-- Copiez ceci dans votre console MySQL/MariaDB :\n" +
                          $"CREATE DATABASE IF NOT EXISTS `{DbName}`;\n" +
                          $"CREATE USER IF NOT EXISTS '{DbUser}'@'%' IDENTIFIED BY '{DbPassword}';\n" +
                          $"GRANT ALL PRIVILEGES ON `{DbName}`.* TO '{DbUser}'@'%';\n" +
                          $"FLUSH PRIVILEGES;";
                    break;

                case "PostgreSQL":
                    sql = $"-- Exécutez ceci sous psql ou pgAdmin :\n" +
                          $"CREATE DATABASE \"{DbName}\";\n" +
                          $"CREATE USER \"{DbUser}\" WITH ENCRYPTED PASSWORD '{DbPassword}';\n" +
                          $"GRANT ALL PRIVILEGES ON DATABASE \"{DbName}\" TO \"{DbUser}\";";
                    break;

                case "Microsoft SQL Server":
                    sql = $"-- Script T-SQL pour SQL Server :\n" +
                          $"CREATE DATABASE [{DbName}];\n" +
                          $"GO\n" +
                          $"CREATE LOGIN [{DbUser}] WITH PASSWORD = '{DbPassword}';\n" +
                          $"GO\n" +
                          $"USE [{DbName}];\n" +
                          $"CREATE USER [{DbUser}] FOR LOGIN [{DbUser}];\n" +
                          $"ALTER ROLE [db_owner] ADD MEMBER [{DbUser}];\n" +
                          $"GO";
                    break;

                case "Oracle":
                    sql = $"-- Script PL/SQL pour Oracle (à adapter selon version 12c/19c/21c) :\n" +
                          $"-- Note : Assurez-vous d'avoir les droits suffisants.\n\n" +
                          $"CREATE USER {DbUser} IDENTIFIED BY \"{DbPassword}\";\n" +
                          $"GRANT CONNECT, RESOURCE TO {DbUser};\n" +
                          $"-- Vous devrez peut-être accorder des quotas :\n" +
                          $"ALTER USER {DbUser} QUOTA UNLIMITED ON USERS;";
                    break;

                default:
                    sql = "-- Sélectionnez un type de base de données.";
                    break;
            }

            SqlOutput = sql;
        }
    }
}