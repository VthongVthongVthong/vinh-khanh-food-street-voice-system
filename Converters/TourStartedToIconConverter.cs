using System.Globalization;

namespace VinhKhanhstreetfoods.Converters;

/// <summary>
/// Converts tour started state to button icon
/// Started = stop_square icon
/// Not started = play icon
/// </summary>
public class TourStartedToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTourStarted)
  {
        // Return stop icon when tour is started, play icon when not started
        return isTourStarted 
         ? "stop_svgrepo_com.svg" // Stop/square icon
: "play_1003_svgrepo_com.png"; // Play/triangle icon
  }
   return "play_1003_svgrepo_com.png";
  }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
