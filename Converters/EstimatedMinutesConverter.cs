using System.Globalization;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters;

/// <summary>
/// Converts EstimatedMinutes to a localized string with "minutes" label.
/// Example: 45 → "⏱️ 45 phút" (Vietnamese)
/// </summary>
public class EstimatedMinutesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int estimatedMinutes || estimatedMinutes <= 0)
            return string.Empty;

        var localizationManager = LocalizationResourceManager.Instance;
        var minutesLabel = localizationManager.GetString("Tour_EstimatedMinutes");

        return $"{estimatedMinutes} {minutesLabel}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
    throw new NotImplementedException();
    }
}
