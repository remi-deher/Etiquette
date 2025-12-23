using Etiquette.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Etiquette.Views
{
    public sealed partial class LogsPage : Page
    {
        public LogsViewModel ViewModel { get; }

        public LogsPage()
        {
            this.InitializeComponent();
            ViewModel = new LogsViewModel();
        }
    }
}