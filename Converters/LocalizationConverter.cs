using System.Globalization;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Converters
{
    /// <summary>
    /// Value converter for localized strings.
    /// Supports dynamic language changes via binding.
    /// </summary>
    public class LocalizationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string key)
                return string.Empty;

            return LocalizationResourceManager.Instance.GetString(key);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
