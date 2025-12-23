using Microsoft.UI.Xaml; // Nécessaire pour RoutedEventArgs
using Microsoft.UI.Xaml.Controls;

namespace Etiquette.Views
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        // La méthode ajoutée pour gérer le clic
        private void ChangelogButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ChangelogPage));
        }
    }
}