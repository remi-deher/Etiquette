using Etiquette.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Etiquette.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel = new SettingsViewModel();
        }
    }
}