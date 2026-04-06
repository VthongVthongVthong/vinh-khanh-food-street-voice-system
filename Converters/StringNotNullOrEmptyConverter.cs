using System.Globalization;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods.Converters
{
    public class StringNotNullOrEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
      if (value is string str)
            return !string.IsNullOrWhiteSpace(str);
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
