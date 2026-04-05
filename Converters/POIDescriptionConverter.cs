using System.Globalization;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters
{
    /// <summary>
    /// Converts POI to its localized description based on app language
    /// </summary>
    public class POIDescriptionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
     {
         if (value is not POI poi)
        return string.Empty;

 // Get description in app language
     var appLanguage = LocalizationService.Instance.CurrentLanguage;
        return poi.GetDescriptionByLanguage(appLanguage);
     }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
{
 throw new NotImplementedException();
        }
}
}
