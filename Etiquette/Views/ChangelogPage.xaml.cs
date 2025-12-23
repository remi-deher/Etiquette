using System;
using Microsoft.UI.Xaml.Controls; // INDISPENSABLE pour que 'Page' soit reconnu
using Microsoft.UI.Xaml;

namespace Etiquette.Views
{
    // Le nom ici doit être "ChangelogPage", PAS "AboutPage"
    public sealed partial class ChangelogPage : Page
    {
        public ChangelogPage()
        {
            // Cette méthode n'existe que si x:Class dans le XAML correspond à ce nom de classe
            this.InitializeComponent();
        }
    }
}