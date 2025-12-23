using Etiquette.Views;
using Etiquette.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace Etiquette
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Impression étiquette";
            TrySetSystemBackdrop();

            // Initialisation de la barre de statut au démarrage
            UpdateStatusBar();
        }

        /// <summary>
        /// Met à jour toutes les informations de la barre de statut (IP, BDD, Imprimante)
        /// </summary>
        public void UpdateStatusBar()
        {
            // 1. Mise à jour de l'IP
            IpStatusText.Text = $"IP : {GetLocalIpAddress() ?? "N/A"}";

            // 2. Mise à jour du statut BDD
            // On considère que c'est local si l'adresse est localhost ou 127.0.0.1
            bool isLocal = AppSettings.DbServer == "127.0.0.1" || AppSettings.DbServer.ToLower() == "localhost";
            DbStatusText.Text = isLocal ? "BDD : Locale" : $"BDD : Distante ({AppSettings.DbServer})";

            // 3. Mise à jour du menu imprimante
            UpdatePrinterMenu();
        }

        /// <summary>
        /// Génère le menu déroulant des imprimantes et met à jour le texte affiché
        /// </summary>
        private void UpdatePrinterMenu()
        {
            string currentPrinter = AppSettings.PrinterName;
            PrinterStatusText.Text = string.IsNullOrEmpty(currentPrinter) ? "Sélectionner imprimante" : currentPrinter;

            PrinterListMenu.Items.Clear();

            try
            {
                // Tente de lister les imprimantes via System.Drawing (nécessite System.Drawing.Common)
                // Si le package n'est pas installé, cela ira dans le catch.
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    var item = new MenuFlyoutItem { Text = printer };

                    // Gestion du clic pour changer l'imprimante
                    item.Click += (s, e) =>
                    {
                        AppSettings.PrinterName = printer;
                        UpdateStatusBar(); // Rafraîchir l'affichage
                    };

                    // Mettre en gras si c'est l'imprimante actuelle
                    if (printer == currentPrinter)
                    {
                        item.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                    }

                    PrinterListMenu.Items.Add(item);
                }
            }
            catch
            {
                // Fallback si on ne peut pas lister les imprimantes (ex: UWP sandboxed ou package manquant)
                PrinterListMenu.Items.Add(new MenuFlyoutItem { Text = "Liste indisponible", IsEnabled = false });
            }

            // Option pour désélectionner
            if (PrinterListMenu.Items.Count > 0)
                PrinterListMenu.Items.Add(new MenuFlyoutSeparator());

            var settingsItem = new MenuFlyoutItem { Text = "Configurer dans les paramètres..." };
            settingsItem.Click += (s, e) => ContentFrame.Navigate(typeof(SettingsPage));
            PrinterListMenu.Items.Add(settingsItem);
        }

        /// <summary>
        /// Récupère l'adresse IPv4 locale du poste
        /// </summary>
        private string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch { return null; }
        }

        private void TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop();
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings.IsFirstRun)
            {
                StartWizard();
            }
            else
            {
                if (NavView.MenuItems.Count > 0)
                {
                    NavView.SelectedItem = NavView.MenuItems[0];
                    ContentFrame.Navigate(typeof(DashboardPage));
                }
            }
        }

        public void StartWizard()
        {
            AppSettings.IsFirstRun = true;

            NavView.IsPaneVisible = false;
            NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;

            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Visibility = Visibility.Collapsed;
            }

            ContentFrame.Navigate(typeof(SetupWizardPage));
        }

        public void EndWizard()
        {
            NavView.IsPaneVisible = true;

            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Visibility = Visibility.Visible;
            }

            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardPage));

            // Mettre à jour la status bar car l'utilisateur a pu changer la config dans le Wizard
            UpdateStatusBar();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "logs":
                        ContentFrame.Navigate(typeof(LogsPage));
                        break;
                    case "help":
                        ContentFrame.Navigate(typeof(HelpPage));
                        break;
                    case "about":
                        ContentFrame.Navigate(typeof(AboutPage));
                        break;
                }
            }
        }
    }
}