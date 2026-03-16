using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public sealed record LanguageOption(string CultureCode, string DisplayName);

        private LanguageOption? _selectedAppLanguage;
        private LanguageOption? _selectedNarrationLanguage;
        private bool _enableAudio;
        private bool _enableAutoNarration;
        private int _cooldownMinutes;
        private int _triggerRadiusMeters;
        private string _statusMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel()
        {
            LoadSettingsCommand = new Command(LoadSettings);
            SaveSettingsCommand = new Command(SaveSettings);
            ResetSettingsCommand = new Command(ResetSettings);

            LanguageOptions = new List<LanguageOption>
            {
                new("vi-VN", "Tiếng Việt"),
                new("en-US", "English"),
                new("zh-CN", "中文 (简体)"),
                new("ko-KR", "한국어")
            };

            LoadSettings();
        }

        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        // App/UI language
        public LanguageOption? SelectedAppLanguage
        {
            get => _selectedAppLanguage;
            set
            {
                if (Equals(_selectedAppLanguage, value))
                    return;

                _selectedAppLanguage = value;
                OnPropertyChanged();
            }
        }

        // Narration language (TTS)
        public LanguageOption? SelectedNarrationLanguage
        {
            get => _selectedNarrationLanguage;
            set
            {
                if (Equals(_selectedNarrationLanguage, value))
                    return;

                _selectedNarrationLanguage = value;
                OnPropertyChanged();
            }
        }

        public bool EnableAudio
        {
            get => _enableAudio;
            set { _enableAudio = value; OnPropertyChanged(); }
        }

        public bool EnableAutoNarration
        {
            get => _enableAutoNarration;
            set { _enableAutoNarration = value; OnPropertyChanged(); }
        }

        public int CooldownMinutes
        {
            get => _cooldownMinutes;
            set { _cooldownMinutes = value; OnPropertyChanged(); }
        }

        public int TriggerRadiusMeters
        {
            get => _triggerRadiusMeters;
            set { _triggerRadiusMeters = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoadSettingsCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        private void LoadSettings()
        {
            var appLangCode = Preferences.Get("appLanguage", "vi-VN");
            var narrationLangCode = Preferences.Get("narrationLanguage", "vi-VN");

            SelectedAppLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == appLangCode) ?? LanguageOptions[0];
            SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == narrationLangCode) ?? LanguageOptions[0];

            EnableAudio = Preferences.Get("enableAudio", true);
            EnableAutoNarration = Preferences.Get("enableAutoNarration", true);
            CooldownMinutes = Preferences.Get("cooldownMinutes", 5);
            TriggerRadiusMeters = Preferences.Get("triggerRadiusMeters", 20);

            StatusMessage = "Cài đặt đã tải";
        }

        private void SaveSettings()
        {
            var appCode = SelectedAppLanguage?.CultureCode ?? "vi-VN";
            var narrationCode = SelectedNarrationLanguage?.CultureCode ?? "vi-VN";

            Preferences.Set("appLanguage", appCode);
            Preferences.Set("narrationLanguage", narrationCode);

            Preferences.Set("enableAudio", EnableAudio);
            Preferences.Set("enableAutoNarration", EnableAutoNarration);
            Preferences.Set("cooldownMinutes", CooldownMinutes);
            Preferences.Set("triggerRadiusMeters", TriggerRadiusMeters);

            // Apply UI culture for current session.
            // Note: full app localization requires resource-based strings; this sets CurrentCulture/CurrentUICulture.
            try
            {
                var culture = CultureInfo.GetCultureInfo(appCode);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
            catch
            {
                // ignore invalid culture codes
            }

            StatusMessage = "Cài đặt đã lưu";
        }

        private void ResetSettings()
        {
            SelectedAppLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == "vi-VN") ?? LanguageOptions[0];
            SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == "vi-VN") ?? LanguageOptions[0];

            EnableAudio = true;
            EnableAutoNarration = true;
            CooldownMinutes = 5;
            TriggerRadiusMeters = 20;

            SaveSettings();
            StatusMessage = "Đã đặt lại cài đặt mặc định";
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
