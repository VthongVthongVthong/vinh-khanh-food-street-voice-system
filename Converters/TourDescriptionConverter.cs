using System.Globalization;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters;

/// <summary>
/// Converts a Tour object to its localized description based on current language.
/// This converter automatically refreshes when the language changes.
/// </summary>
public class TourDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Tour tour)
      return string.Empty;

  var localizationManager = LocalizationResourceManager.Instance;
        var currentLanguage = localizationManager.CurrentLanguage;

  return currentLanguage switch
     {
       "en" => tour.DescriptionEn ?? tour.Description ?? string.Empty,
  "zh" => tour.DescriptionZh ?? tour.Description ?? string.Empty,
  "ja" => tour.DescriptionJa ?? tour.Description ?? string.Empty,
   "ko" => tour.DescriptionKo ?? tour.Description ?? string.Empty,
            "fr" => tour.DescriptionFr ?? tour.Description ?? string.Empty,
      "ru" => tour.DescriptionRu ?? tour.Description ?? string.Empty,
   _ => tour.Description ?? string.Empty
     };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
 }
}
