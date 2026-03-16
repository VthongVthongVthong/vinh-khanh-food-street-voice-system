using System.Globalization;

namespace VinhKhanhstreetfoods.Converters;

public class IsPlayingColorConverter : IValueConverter
{
    public Color PlayingColor { get; set; } = Color.FromArgb("#283845");
    public Color IdleColor { get; set; } = Color.FromArgb("#C9A36B");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool isPlaying && isPlaying ? PlayingColor : IdleColor;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}