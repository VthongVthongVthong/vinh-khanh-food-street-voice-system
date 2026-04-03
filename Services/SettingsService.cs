namespace VinhKhanhstreetfoods.Services;

public class SettingsService
{
    private const string PreferredLanguageKey = "preferredLanguage";
    private const string DefaultLanguage = "vi";

    public event EventHandler<string>? PreferredLanguageChanged;

    public string PreferredLanguage
    {
        get => Normalize(Preferences.Get(PreferredLanguageKey, DefaultLanguage));
        set
        {
            var normalized = Normalize(value);
            var current = Normalize(Preferences.Get(PreferredLanguageKey, DefaultLanguage));

            if (string.Equals(current, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            Preferences.Set(PreferredLanguageKey, normalized);
            PreferredLanguageChanged?.Invoke(this, normalized);
        }
    }

    private static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return DefaultLanguage;

        var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();
        var dash = normalized.IndexOf('-');
        normalized = dash > 0 ? normalized[..dash] : normalized;

        return normalized switch
        {
            "vn" or "vi" => "vi",
            "en" => "en",
            "zh" => "zh",
            "ja" => "ja",
            "ko" => "ko",
            "fr" => "fr",
            "ru" => "ru",
            _ => DefaultLanguage
        };
    }
}
