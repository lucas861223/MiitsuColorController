using Microsoft.UI.Xaml.Data;
using System;

namespace MiitsuColorController.Converters
{
    public class IntToStringArrayIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString((string)parameter).Split(",")[(int)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}