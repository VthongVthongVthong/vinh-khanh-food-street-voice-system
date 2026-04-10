using System.Globalization;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods.Converters
{
    /// <summary>
    /// Converts null/empty string to boolean (inverse of IsNullOrEmptyConverter)
    /// </summary>
    public class IsNotNullOrEmptyConverter : IValueConverter
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
