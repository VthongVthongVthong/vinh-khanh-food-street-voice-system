using System.Globalization;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters
{
    /// <summary>
    /// Converts POI to its localized description based on current UI language (not narration language)
    /// Used in Tour Detail page to show description matching user's current UI language
    /// </summary>
    public class POIDescriptionLocalizedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not POI poi)
 return string.Empty;

    // Get description in app language (UI language, not narration language)
  var appLanguage = LocalizationResourceManager.Instance.CurrentLanguage;
     return poi.GetDescriptionByLanguage(appLanguage);
        }

   public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
      throw new NotImplementedException();
        }
    }
}
