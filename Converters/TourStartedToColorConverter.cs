using System.Globalization;

namespace VinhKhanhstreetfoods.Converters;

/// <summary>
/// Converts tour started state to button background color
/// Started = Red (stop/danger color)
/// Not started = Orange (brand color)
/// </summary>
public class TourStartedToColorConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
     if (value is bool isTourStarted)
        {
            // Return red when tour is started, orange when not started
            return isTourStarted 
            ? Color.FromArgb("#DC3545") // Red (stop/end color)
    : Color.FromArgb("#FF8C00"); // Orange (brand color)
        }
        return Color.FromArgb("#FF8C00");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
