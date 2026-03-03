using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VinhKhanhFoodGuide.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private string _selectedLanguage = "en";
    private bool _isTtsEnabled = true;
    private int _updateIntervalSeconds = 5;

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => SetProperty(ref _selectedLanguage, value);
    }

    public bool IsTtsEnabled
    {
        get => _isTtsEnabled;
        set => SetProperty(ref _isTtsEnabled, value);
    }

    public int UpdateIntervalSeconds
    {
        get => _updateIntervalSeconds;
        set => SetProperty(ref _updateIntervalSeconds, value);
    }

    public List<string> AvailableLanguages { get; } = new() { "en", "vi", "fr", "zh" };
    public List<int> UpdateIntervals { get; } = new() { 2, 5, 10, 15 };

    public SettingsViewModel()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        // Load from preferences
        SelectedLanguage = Preferences.Default.Get("app_language", "en");
        IsTtsEnabled = Preferences.Default.Get("tts_enabled", true);
        UpdateIntervalSeconds = Preferences.Default.Get("update_interval", 5);
    }

    public void SaveSettings()
    {
        Preferences.Default.Set("app_language", SelectedLanguage);
        Preferences.Default.Set("tts_enabled", IsTtsEnabled);
        Preferences.Default.Set("update_interval", UpdateIntervalSeconds);
    }
}
