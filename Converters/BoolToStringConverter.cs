using Microsoft.UI.Xaml.Data;
using System;

namespace MiitsuColorController.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? ((string)parameter).Split(",")[0] : ((string)parameter).Split(",")[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}