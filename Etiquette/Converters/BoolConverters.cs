using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Etiquette.Converters
{
    // Convertit true/false en icône (Cadenas ouvert/fermé)
    public class BoolToLockIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (bool)value ? "\uE785" : "\uE72E"; // E785 = Unlock, E72E = Lock

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    // Convertit true/false en texte du bouton
    public class BoolToActionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (bool)value ? "Désactiver le partage" : "Activer le partage";

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    // Convertit true/false en couleur d'InfoBar
    public class BoolToSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (bool)value ? InfoBarSeverity.Success : InfoBarSeverity.Informational;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}