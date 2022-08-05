using Microsoft.UI.Xaml.Data;
using System;

namespace MiitsuColorController.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string[] message = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString((string)parameter).Split(",");
            return (bool)value ? message[0] : message[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}