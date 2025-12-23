using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Etiquette.Converters
{
    public partial class StringVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string text && string.IsNullOrEmpty(text))
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}