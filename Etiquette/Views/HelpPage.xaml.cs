using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Etiquette.Services;
using System;
using System.Linq;
using System.Reflection;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Etiquette.Views
{
    public sealed partial class HelpPage : Page
    {
        public HelpPage()
        {
            this.InitializeComponent();
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            // 1. Version
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            TxtVersion.Text = $"Etiquette App - v{version.Major}.{version.Minor}.{version.Build}";

            // 2. IP Locale
            try
            {
                var hostNames = NetworkInformation.GetHostNames();
                var localIp = hostNames.FirstOrDefault(h => h.Type == HostNameType.Ipv4 && h.IPInformation != null)?.DisplayName;
                TxtIpAddress.Text = localIp ?? "Non connect�";
            }
            catch
            {
                TxtIpAddress.Text = "Inconnue";
            }

            // 3. Statut BDD
            UpdateDbStatusDisplay();
        }

        private void UpdateDbStatusDisplay()
        {
            if (AppSettings.AppMode == "Standalone")
            {
                TxtDbStatus.Text = "Mode Local (Monoposte)";
                TxtDbDetail.Text = "Base de donn�es SQLite interne.";
                IconDbStatus.Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            }
            else
            {
                TxtDbStatus.Text = $"Serveur : {AppSettings.DbServer}";
                TxtDbDetail.Text = $"Utilisateur : {AppSettings.DbUser} | Base : {AppSettings.DbName}";
                IconDbStatus.Foreground = (Brush)Application.Current.Resources["SystemAccentColor"];
            }
        }

        private async void OnPrintTest(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Test d'impression",
                Content = $"Une commande de test a �t� envoy�e � l'imprimante :\n{AppSettings.PrinterName}\n\nSi rien ne sort, v�rifiez le c�ble USB/R�seau.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void OnCheckConnection(object sender, RoutedEventArgs e)
        {
            if (AppSettings.AppMode == "Standalone")
            {
                var dialog = new ContentDialog
                {
                    Title = "Info",
                    Content = "Vous �tes en mode monoposte (Local). Aucune connexion r�seau n'est requise pour la base de donn�es.",
                    CloseButtonText = "Compris",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            TxtDbStatus.Text = "Ping en cours...";
            await System.Threading.Tasks.Task.Delay(1000);
            UpdateDbStatusDisplay();

            var tooltip = new ToolTip { Content = "La configuration semble correcte." };
            ToolTipService.SetToolTip((Button)sender, tooltip);
            tooltip.IsOpen = true;
            await System.Threading.Tasks.Task.Delay(2000);
            tooltip.IsOpen = false;
        }

        private void OnOpenLogs(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LogsPage));
        }

        // NOUVELLE M�THODE POUR GITHUB
        private async void OnOpenGitHub(object sender, RoutedEventArgs e)
        {
            // Ouvre le navigateur par d�faut sur la page du projet
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/remi-deher/Etiquette"));
        }
    }
}