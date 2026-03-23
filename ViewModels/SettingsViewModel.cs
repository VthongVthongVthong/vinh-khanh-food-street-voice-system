using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public sealed record LanguageOption(string CultureCode, string DisplayName);

        private readonly SettingsService _settingsService;

        private LanguageOption? _selectedNarrationLanguage;
        private bool _enableAudio;
        private bool _enableAutoNarration;
        private int _cooldownMinutes;
        private int _triggerRadiusMeters;
        private string _statusMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            SaveSettingsCommand = new Command(SaveSettings);

            LanguageOptions = new List<LanguageOption>
            {
                new("vi", "Tiếng Việt"),
                new("en", "English"),
                new("zh", "中文 (Chinese Simplified)"),
                new("ko", "한국어 (Korean)")
            };

            LoadSettings();
        }

        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        /// <summary>
        /// Narration language (for TTS translation)
        /// </summary>
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

        public ICommand SaveSettingsCommand { get; }

        private void LoadSettings()
        {
            var narrationLangCode = _settingsService.PreferredLanguage;

            SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == narrationLangCode)
                ?? LanguageOptions[0];

            EnableAudio = Preferences.Get("enableAudio", true);
            EnableAutoNarration = Preferences.Get("enableAutoNarration", true);
            CooldownMinutes = Preferences.Get("cooldownMinutes", 5);
            TriggerRadiusMeters = Preferences.Get("triggerRadiusMeters", 20);

            StatusMessage = "Settings loaded";
        }

        private void SaveSettings()
        {
            var narrationCode = SelectedNarrationLanguage?.CultureCode ?? "vi";
            _settingsService.PreferredLanguage = narrationCode;

            Preferences.Set("enableAudio", EnableAudio);
            Preferences.Set("enableAutoNarration", EnableAutoNarration);
            Preferences.Set("cooldownMinutes", CooldownMinutes);
            Preferences.Set("triggerRadiusMeters", TriggerRadiusMeters);

            StatusMessage = "Settings saved ✓";
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
