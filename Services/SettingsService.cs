namespace VinhKhanhstreetfoods.Services;

public class SettingsService
{
    private const string PreferredLanguageKey = "preferredLanguage";
    private const string DefaultLanguage = "vi";

    public string PreferredLanguage
    {
        get => Preferences.Get(PreferredLanguageKey, DefaultLanguage);
        set => Preferences.Set(PreferredLanguageKey, Normalize(value));
    }

    private static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return DefaultLanguage;
        return code.Trim();
    }
}
