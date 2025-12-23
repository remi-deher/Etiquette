using Etiquette.Models;
using Etiquette.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing.Printing;

namespace Etiquette.Views
{
    public sealed partial class SetupWizardPage : Page
    {
        // Service de découverte réseau
        private NetworkDiscoveryService _discoveryService = new NetworkDiscoveryService();

        // Variables pour le Binding (x:Bind) de l'étape de découverte
        private string _discoveryStatus = "Recherche...";
        private bool _isSearching = false;
        private string _foundServerIp = null;

        public SetupWizardPage()
        {
            this.InitializeComponent();

            // Chargement initial des périphériques
            LoadPrinters();

            // Démarrage à l'étape 1 (Matériel)
            ShowStep(1);
        }

        // =========================================================
        // GESTION DU MATÉRIEL (IMPRIMANTE + SCANNER)
        // =========================================================

        private void LoadPrinters()
        {
            try
            {
                CmbPrinters.Items.Clear();

                // On récupère les imprimantes installées via System.Drawing
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    CmbPrinters.Items.Add(printer);
                }

                // Sélection intelligente : on cherche "Zebra", "Label", "Etiquette" ou "Receipt"
                if (CmbPrinters.Items.Count > 0)
                {
                    CmbPrinters.SelectedIndex = 0; // Par défaut le premier

                    foreach (var item in CmbPrinters.Items)
                    {
                        string name = item.ToString();

                        // CORRECTION ICI : Ajout du || et ajustement des parenthèses
                        if (name.Contains("Zebra", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Label", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Etiquette", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Receipt", StringComparison.OrdinalIgnoreCase))
                        {
                            CmbPrinters.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Pensez à garder le bloc catch qui fermait votre try initialement
                CmbPrinters.PlaceholderText = "Impossible de lister les imprimantes";
            }
        }

        private void OnOpenWindowsPrinters(object sender, RoutedEventArgs e)
        {
            // Ouvre le panneau de configuration Windows
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:printers"));
        }

        private void OnScanTestInput(object sender, TextChangedEventArgs e)
        {
            // Dès que la douchette "écrit" quelque chose dans la zone de texte,
            // on affiche un feedback visuel vert pour confirmer que ça marche.
            if (!string.IsNullOrEmpty(TxtScanTest.Text))
            {
                IconScanStatus.Visibility = Visibility.Visible;
                TxtScanFeedback.Visibility = Visibility.Visible;
            }
            else
            {
                IconScanStatus.Visibility = Visibility.Collapsed;
                TxtScanFeedback.Visibility = Visibility.Collapsed;
            }
        }

        // =========================================================
        // GESTION DE LA NAVIGATION (WIZARD)
        // =========================================================

        private void ShowStep(int step)
        {
            // 1. On masque tout
            Step1_Hardware.Visibility = Visibility.Collapsed;
            Step2_ModeChoice.Visibility = Visibility.Collapsed;
            Step3_RoleChoice.Visibility = Visibility.Collapsed;
            Step4_ServerConfig.Visibility = Visibility.Collapsed;
            Step4_Discovery.Visibility = Visibility.Collapsed;

            // 2. On affiche la bonne étape et on met à jour la barre de progression
            switch (step)
            {
                case 1: // Matériel
                    Step1_Hardware.Visibility = Visibility.Visible;
                    WizardProgress.Value = 15;
                    break;

                case 2: // Choix Mode (Solo vs Réseau)
                    Step2_ModeChoice.Visibility = Visibility.Visible;
                    WizardProgress.Value = 35;
                    break;

                case 3: // Choix Rôle (Serveur vs Client)
                    Step3_RoleChoice.Visibility = Visibility.Visible;
                    WizardProgress.Value = 55;
                    break;

                case 40: // Config Manuelle (Serveur)
                    Step4_ServerConfig.Visibility = Visibility.Visible;
                    WizardProgress.Value = 85;
                    // On pré-génère le SQL pour aider l'utilisateur
                    OnGenerateSql(null, null);
                    break;

                case 41: // Découverte Auto (Client)
                    Step4_Discovery.Visibility = Visibility.Visible;
                    WizardProgress.Value = 85;
                    if (PinEntryPanel != null) PinEntryPanel.Visibility = Visibility.Collapsed;
                    StartDiscovery();
                    break;
            }
        }

        // --- HANDLERS DES BOUTONS ---

        // Validation étape 1 -> Passage étape 2
        private void OnHardwareValidated(object sender, RoutedEventArgs e)
        {
            // On sauvegarde l'imprimante tout de suite
            if (CmbPrinters.SelectedItem != null)
            {
                AppSettings.PrinterName = CmbPrinters.SelectedItem.ToString();
            }
            ShowStep(2);
        }

        // Navigation Étape 2 (Mode)
        private void OnGoToStandalone(object s, RoutedEventArgs e)
        {
            // Mode Solo : On configure une DB locale par défaut (ex: localhost) et on termine.
            FinishSetup("Standalone", "127.0.0.1", "labelmaster", "root", "", "None", "MariaDB / MySQL");
        }

        private void OnGoToMultiPoste(object s, RoutedEventArgs e) => ShowStep(3);

        // Navigation Étape 3 (Rôle)
        private void OnGoToNewServer(object s, RoutedEventArgs e) => ShowStep(40); // Config manuelle
        private void OnGoToDiscovery(object s, RoutedEventArgs e) => ShowStep(41); // Recherche auto

        // Navigation Retour
        private void OnBackToStep1(object s, RoutedEventArgs e) => ShowStep(1);
        private void OnBackToStep2(object s, RoutedEventArgs e) => ShowStep(2);
        private void OnBackToStep3(object s, RoutedEventArgs e) => ShowStep(3);


        // =========================================================
        // CONFIGURATION SERVEUR & SQL
        // =========================================================

        private void OnValidateServerConfig(object s, RoutedEventArgs e)
        {
            // Validation manuelle de la config serveur
            FinishSetup(
                "MultiPoste",
                TxtServerIp.Text,
                TxtDbName.Text,
                TxtUser.Text,
                TxtPassword.Password,
                CmbSslMode.SelectedValue as string ?? "None",
                CmbDbType.SelectedItem as string ?? "MariaDB / MySQL"
            );
        }

        private void OnGenerateSql(object s, RoutedEventArgs e)
        {
            // Vérification que les contrôles sont chargés
            if (TxtDbName == null || TxtUser == null || TxtPassword == null || CmbDbType == null || TxtSqlOutput == null)
                return;

            string dbName = TxtDbName.Text;
            string user = TxtUser.Text;
            string pass = TxtPassword.Password;
            string dbType = CmbDbType.SelectedItem as string ?? "MariaDB / MySQL";

            if (string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(user))
            {
                TxtSqlOutput.Text = "-- Remplissez les champs Nom et Utilisateur pour voir le script.";
                return;
            }

            string sql = "";

            switch (dbType)
            {
                case "MariaDB / MySQL":
                    sql = $"CREATE DATABASE IF NOT EXISTS `{dbName}`;\n" +
                          $"CREATE USER IF NOT EXISTS '{user}'@'%' IDENTIFIED BY '{pass}';\n" +
                          $"GRANT ALL PRIVILEGES ON `{dbName}`.* TO '{user}'@'%';\n" +
                          $"FLUSH PRIVILEGES;";
                    break;

                case "PostgreSQL":
                    sql = $"CREATE DATABASE \"{dbName}\";\n" +
                          $"CREATE USER \"{user}\" WITH ENCRYPTED PASSWORD '{pass}';\n" +
                          $"GRANT ALL PRIVILEGES ON DATABASE \"{dbName}\" TO \"{user}\";";
                    break;

                case "Microsoft SQL Server":
                    sql = $"CREATE DATABASE [{dbName}];\nGO\n" +
                          $"CREATE LOGIN [{user}] WITH PASSWORD = '{pass}';\nGO\n" +
                          $"USE [{dbName}];\n" +
                          $"CREATE USER [{user}] FOR LOGIN [{user}];\n" +
                          $"ALTER ROLE [db_owner] ADD MEMBER [{user}];\nGO";
                    break;

                case "Oracle":
                    sql = $"CREATE USER {user} IDENTIFIED BY \"{pass}\";\n" +
                          $"GRANT CONNECT, RESOURCE TO {user};\n" +
                          $"ALTER USER {user} QUOTA UNLIMITED ON USERS;";
                    break;
            }

            TxtSqlOutput.Text = sql;
        }


        // =========================================================
        // DÉCOUVERTE RÉSEAU (DISCOVERY)
        // =========================================================

        private async void StartDiscovery()
        {
            _isSearching = true;
            _discoveryStatus = "Recherche d'un poste configuré sur le réseau...";
            Bindings.Update();

            try
            {
                // On cherche via UDP Broadcast
                string serverIp = await _discoveryService.SearchForServerAsync(timeoutMs: 4000);

                if (!string.IsNullOrEmpty(serverIp))
                {
                    _foundServerIp = serverIp;
                    _discoveryStatus = $"Serveur trouvé ({serverIp}). Tentative de connexion...";
                    Bindings.Update();
                    await TrySecurePairing(serverIp);
                }
                else
                {
                    _discoveryStatus = "Aucun serveur trouvé. Vérifiez que le serveur est allumé.";
                }
            }
            catch (Exception ex)
            {
                _discoveryStatus = $"Erreur : {ex.Message}";
            }

            _isSearching = false;
            Bindings.Update();
        }

        private void OnFetchConfig(object sender, RoutedEventArgs e)
        {
            // Bouton cliqué après avoir entré le PIN (si nécessaire)
            // On relance la logique qui inclura l'envoi du PIN si implémenté, 
            // ou on recommence simplement la découverte.
            StartDiscovery();
        }

        private async Task TrySecurePairing(string ip)
        {
            string url = $"http://{ip}:54322/pair";
            try
            {
                using (var crypto = new CryptoService())
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    // Échange de clés
                    byte[] myPublicKey = crypto.GetPublicKey();
                    var content = new ByteArrayContent(myPublicKey);
                    var response = await client.PostAsync(url, content);

                    // Si le serveur demande un PIN (Forbidden 403)
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _discoveryStatus = "Serveur trouvé. Veuillez entrer le code PIN du serveur.";
                        PinEntryPanel.Visibility = Visibility.Visible;
                        return;
                    }

                    response.EnsureSuccessStatusCode();

                    // Déchiffrement de la réponse
                    byte[] responseData = await response.Content.ReadAsByteArrayAsync();

                    using (var ms = new MemoryStream(responseData))
                    using (var reader = new BinaryReader(ms))
                    {
                        int keyLength = reader.ReadInt32();
                        byte[] serverPublicKey = reader.ReadBytes(keyLength);
                        crypto.DeriveSharedSecret(serverPublicKey);
                        byte[] encryptedConfig = reader.ReadBytes((int)(ms.Length - ms.Position));
                        string jsonConfig = crypto.DecryptData(encryptedConfig);

                        if (!string.IsNullOrEmpty(jsonConfig))
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var config = JsonSerializer.Deserialize<ConfigurationTransferModel>(jsonConfig, options);

                            if (config != null)
                            {
                                // Succès : on applique la config reçue
                                FinishSetup("MultiPoste", config.DbServer, config.DbName, config.DbUser, config.DbPassword, "None", "MariaDB / MySQL");
                                return;
                            }
                        }
                    }
                }
                _discoveryStatus = "Erreur : Échec du déchiffrement des données.";
            }
            catch (Exception ex)
            {
                _discoveryStatus = $"Erreur de connexion : {ex.Message}";
            }
        }


        // =========================================================
        // FINALISATION
        // =========================================================

        private void FinishSetup(string mode, string ip, string db, string user, string pass, string sslMode, string dbType)
        {
            WizardProgress.Value = 100;

            // Enregistrement dans les préférences globales
            AppSettings.AppMode = mode;
            if (!string.IsNullOrEmpty(ip)) AppSettings.DbServer = ip;
            if (!string.IsNullOrEmpty(db)) AppSettings.DbName = db;
            if (!string.IsNullOrEmpty(user)) AppSettings.DbUser = user;
            if (!string.IsNullOrEmpty(pass)) AppSettings.DbPassword = pass;
            AppSettings.DbSslMode = sslMode;
            AppSettings.DbType = dbType;

            // Marquer le wizard comme terminé
            AppSettings.IsFirstRun = false;

            // Fermer le wizard via la fenêtre principale
            var mainWindow = (Application.Current as App)?.m_window as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.EndWizard();
            }
        }
    }
}