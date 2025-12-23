using Etiquette.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Etiquette.Converters
{
    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Error => new SolidColorBrush(Colors.Red),
                    LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                    LogLevel.Success => new SolidColorBrush(Colors.LimeGreen),
                    LogLevel.Info => new SolidColorBrush(Colors.CornflowerBlue),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}