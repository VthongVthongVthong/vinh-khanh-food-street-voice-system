using System.Globalization;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods.Converters
{
 /// <summary>
    /// Converts boolean to opacity value (disabled items = 0.6, enabled = 1.0)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
   if (value is bool boolValue)
       return boolValue ? 0.6 : 1.0;
     return 1.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
=> throw new NotImplementedException();
 }
}
