using Etiquette.Views;
using Etiquette.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Etiquette
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Impression étiquette";
            TrySetSystemBackdrop();
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
            // Vérification du premier lancement
            if (AppSettings.IsFirstRun)
            {
                StartWizard();
            }
            else
            {
                // Lancement normal
                if (NavView.MenuItems.Count > 0)
                {
                    NavView.SelectedItem = NavView.MenuItems[0];
                    ContentFrame.Navigate(typeof(DashboardPage));
                }
            }
        }

        /// <summary>
        /// Lance le Wizard en masquant la navigation standard
        /// </summary>
        public void StartWizard()
        {
            AppSettings.IsFirstRun = true;

            // Mode Wizard : On cache le menu de navigation pour l'immersion
            NavView.IsPaneVisible = false;
            NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;

            // Masquer le bouton Settings natif pendant le wizard
            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Visibility = Visibility.Collapsed;
            }

            ContentFrame.Navigate(typeof(SetupWizardPage));
        }

        public void EndWizard()
        {
            // On réactive le menu
            NavView.IsPaneVisible = true;

            // Réafficher le bouton Settings natif
            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Visibility = Visibility.Visible;
            }

            // On navigue vers le dashboard
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardPage));
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