using Etiquette.Models;
using Etiquette.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Etiquette.Views
{
    public sealed partial class SetupWizardPage : Page
    {
        private NetworkDiscoveryService _discoveryService = new NetworkDiscoveryService();
        private string _discoveryStatus = "Recherche...";
        private bool _isSearching = false;
        private string _foundServerIp = null;

        public SetupWizardPage()
        {
            this.InitializeComponent();
            ShowStep(1);
        }

        private void ShowStep(int step)
        {
            Step1_ModeChoice.Visibility = Visibility.Collapsed;
            Step2_RoleChoice.Visibility = Visibility.Collapsed;
            Step3_ServerConfig.Visibility = Visibility.Collapsed;
            Step3_Discovery.Visibility = Visibility.Collapsed;

            switch (step)
            {
                case 1: Step1_ModeChoice.Visibility = Visibility.Visible; WizardProgress.Value = 10; break;
                case 2: Step2_RoleChoice.Visibility = Visibility.Visible; WizardProgress.Value = 30; break;
                case 30:
                    Step3_ServerConfig.Visibility = Visibility.Visible;
                    WizardProgress.Value = 60;
                    // On appelle la génération manuellement une fois que tout est affiché
                    OnGenerateSql(null, null);
                    break;
                case 31:
                    Step3_Discovery.Visibility = Visibility.Visible;
                    WizardProgress.Value = 60;
                    if (PinEntryPanel != null) PinEntryPanel.Visibility = Visibility.Collapsed;
                    StartDiscovery();
                    break;
            }
        }

        // --- HANDLERS NAVIGATION ---
        private void OnGoToStandalone(object s, RoutedEventArgs e) => FinishSetup("Standalone", "127.0.0.1", "labelmaster", "root", "", "None", "MariaDB / MySQL");

        private void OnGoToMultiPoste(object s, RoutedEventArgs e) => ShowStep(2);
        private void OnGoToNewServer(object s, RoutedEventArgs e) => ShowStep(30);
        private void OnGoToDiscovery(object s, RoutedEventArgs e) => ShowStep(31);
        private void OnBackToStep1(object s, RoutedEventArgs e) => ShowStep(1);
        private void OnBackToStep2(object s, RoutedEventArgs e) => ShowStep(2);

        // --- VALIDATION SERVEUR ---
        private void OnValidateServerConfig(object s, RoutedEventArgs e)
        {
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

        // --- GÉNÉRATION SQL (CORRIGÉE) ---
        private void OnGenerateSql(object s, RoutedEventArgs e)
        {
            // IMPORTANT : Cette ligne empêche le crash au démarrage.
            // On vérifie que TOUS les contrôles utilisés existent avant de continuer.
            if (TxtDbName == null || TxtUser == null || TxtPassword == null || CmbDbType == null || TxtSqlOutput == null)
                return;

            string dbName = TxtDbName.Text;
            string user = TxtUser.Text;
            string pass = TxtPassword.Password;
            string dbType = CmbDbType.SelectedItem as string ?? "MariaDB / MySQL";

            if (string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(user))
            {
                TxtSqlOutput.Text = "-- Remplissez les champs Nom et Utilisateur pour générer le script.";
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

        // --- DISCOVERY & PAIRING ---
        private async void StartDiscovery()
        {
            _isSearching = true;
            _discoveryStatus = "Recherche d'un serveur sur le réseau...";
            Bindings.Update();

            try
            {
                string serverIp = await _discoveryService.SearchForServerAsync(timeoutMs: 4000);

                if (!string.IsNullOrEmpty(serverIp))
                {
                    _foundServerIp = serverIp;
                    _discoveryStatus = $"Serveur trouvé ({serverIp}). Tentative de connexion sécurisée...";
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

        private async Task TrySecurePairing(string ip)
        {
            string url = $"http://{ip}:54322/pair";
            try
            {
                using (var crypto = new CryptoService())
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    byte[] myPublicKey = crypto.GetPublicKey();
                    var content = new ByteArrayContent(myPublicKey);
                    var response = await client.PostAsync(url, content);

                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _discoveryStatus = "Serveur trouvé, mais mode 'Appairage' inactif.";
                        return;
                    }
                    response.EnsureSuccessStatusCode();
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
                                FinishSetup("MultiPoste", config.DbServer, config.DbName, config.DbUser, config.DbPassword, "None", "MariaDB / MySQL");
                                return;
                            }
                        }
                    }
                }
                _discoveryStatus = "Erreur : Échec du déchiffrement.";
            }
            catch (Exception ex)
            {
                _discoveryStatus = $"Erreur de connexion : {ex.Message}";
            }
        }

        // --- FIN SETUP ---
        private void FinishSetup(string mode, string ip, string db, string user, string pass, string sslMode, string dbType)
        {
            WizardProgress.Value = 100;
            AppSettings.AppMode = mode;
            if (!string.IsNullOrEmpty(ip)) AppSettings.DbServer = ip;
            if (!string.IsNullOrEmpty(db)) AppSettings.DbName = db;
            if (!string.IsNullOrEmpty(user)) AppSettings.DbUser = user;
            if (!string.IsNullOrEmpty(pass)) AppSettings.DbPassword = pass;
            AppSettings.DbSslMode = sslMode;
            AppSettings.DbType = dbType;
            AppSettings.IsFirstRun = false;

            var mainWindow = (Application.Current as App)?.m_window as MainWindow;
            if (mainWindow != null) mainWindow.EndWizard();
        }

        private void OnFetchConfig(object sender, RoutedEventArgs e) => StartDiscovery();
    }
}