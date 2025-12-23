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

        public void UpdateStatusBar()
        {
            IpStatusText.Text = $"IP : {GetLocalIpAddress() ?? "N/A"}";

            bool isLocal = AppSettings.DbServer == "127.0.0.1" || AppSettings.DbServer.ToLower() == "localhost";
            DbStatusText.Text = isLocal ? "BDD : Locale" : $"BDD : Distante ({AppSettings.DbServer})";
            DbStatusDot.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);

            UpdatePrinterMenu();
        }

        private void UpdatePrinterMenu()
        {
            string currentPrinter = AppSettings.PrinterName;
            bool hasPrinter = !string.IsNullOrEmpty(currentPrinter);

            PrinterStatusText.Text = hasPrinter ? currentPrinter : "Sélectionner imprimante";
            PrinterStatusDot.Fill = new SolidColorBrush(hasPrinter ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);

            PrinterListMenu.Items.Clear();

            try
            {
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    var item = new MenuFlyoutItem { Text = printer };
                    item.Click += (s, e) =>
                    {
                        AppSettings.PrinterName = printer;
                        UpdateStatusBar();
                    };

                    if (printer == currentPrinter)
                    {
                        item.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                    }
                    PrinterListMenu.Items.Add(item);
                }
            }
            catch
            {
                PrinterListMenu.Items.Add(new MenuFlyoutItem { Text = "Liste indisponible", IsEnabled = false });
            }

            if (PrinterListMenu.Items.Count > 0)
                PrinterListMenu.Items.Add(new MenuFlyoutSeparator());

            var windowsSettingsItem = new MenuFlyoutItem
            {
                Text = "Gérer les imprimantes Windows...",
                Icon = new FontIcon { Glyph = "\uE713" }
            };

            windowsSettingsItem.Click += async (s, e) =>
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:printers"));
            };

            PrinterListMenu.Items.Add(windowsSettingsItem);
        }

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

                // NOUVEAU : Vérification du Changelog
                CheckForChangelog();
            }
        }

        // --- GESTION DU CHANGELOG ---

        private string GetAppVersion()
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async void CheckForChangelog()
        {
            string currentVersion = GetAppVersion();
            string lastVersion = AppSettings.LastRunVersion;

            // Si c'est une nouvelle version
            if (currentVersion != lastVersion)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Quoi de neuf dans la version {currentVersion} ?",
                    Content = new ChangelogPage(),
                    CloseButtonText = "Fermer",
                    XamlRoot = this.Content.XamlRoot, // Requis pour WinUI 3
                    Width = 600
                };

                await dialog.ShowAsync();

                // On met à jour la version enregistrée
                AppSettings.LastRunVersion = currentVersion;
            }
        }

        // ---------------------------

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