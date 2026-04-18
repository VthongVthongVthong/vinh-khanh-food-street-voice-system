using System.Globalization;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Converters;

/// <summary>
/// Converts a Tour object to its cover image URL.
/// Falls back to a default image if no cover image is available.
/// </summary>
public class TourCoverImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Tour tour)
      return string.Empty;

        return tour.CoverImageUrl ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
