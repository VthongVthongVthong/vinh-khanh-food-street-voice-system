using System.Globalization;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters
{
    /// <summary>
    /// Converter to get "View Details" text localized with arrow
    /// </summary>
  public class ViewDetailsTextConverter : IValueConverter
  {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var resourceManager = LocalizationResourceManager.Instance;
     return resourceManager.GetString("Tour_ViewDetails") ?? "Xem chi ti?t";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
        throw new NotImplementedException();
        }
    }
}
