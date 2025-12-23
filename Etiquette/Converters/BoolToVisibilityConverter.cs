using Microsoft.UI.Xaml; // Important pour 'Visibility'
using Microsoft.UI.Xaml.Data;
using System;

namespace Etiquette.Converters
{
    // ... Vos autres convertisseurs (BoolToLockIconConverter, etc.) sont ici ...

    // AJOUTEZ CELUI-CI :
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}