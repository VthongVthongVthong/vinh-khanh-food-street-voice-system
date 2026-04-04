using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Diagnostics;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public sealed record LanguageOption(string CultureCode, string DisplayName, bool IsOnlineOnly = false)
        {
            public string DisplayLabel => DisplayName;
        }

        private readonly SettingsService _settingsService;
        private readonly ITranslationService _translationService;
        private readonly IPOIRepository? _poiRepository;

        private LanguageOption? _selectedNarrationLanguage;
        private LanguageOption? _selectedAppLanguage;
        private bool _enableAudio;
        private bool _enableAutoNarration;
        private int _cooldownMinutes;
        private int _triggerRadiusMeters;
        private bool _isDownloadingLanguagePack;
        private string _statusMessage = string.Empty;
        private bool _isInitializing;
        private double _downloadProgress;
        private string _downloadDetails = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel(SettingsService settingsService, ITranslationService translationService, IPOIRepository? poiRepository = null)
        {
            _settingsService = settingsService;
            _translationService = translationService;
            _poiRepository = poiRepository;

            SaveSettingsCommand = new Command(SaveSettings);
            ResetSettingsCommand = new Command(ResetSettings);
            CancelCommand = new Command(Cancel);
            DownloadLanguagePackCommand = new Command(async () => await DownloadLanguagePackAsync(), () => !IsDownloadingLanguagePack);

            LanguageOptions = new List<LanguageOption>
            {
                new("vi", "Ti\u1EBFng Vi\u1EC7t"),
                new("en", "English"),
                new("zh", "\u4E2D\u6587 (Chinese Simplified)"),
                new("ja", "\u65E5\u672C\u8A9E (Japanese)", false),
                new("ko", "\uD55C\uAD6D\uC5B4 (Korean)", false),
                new("fr", "Fran\u00E7ais (French)", false),
                new("ru", "\u0420\u0443\u0441\u0441\u043A\u0438\u0439 (Russian)", false)
            };

            LoadSettings();
        }

        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        public LanguageOption? SelectedAppLanguage
        {
            get => _selectedAppLanguage;
            set
            {
                if (Equals(_selectedAppLanguage, value))
                    return;

                _selectedAppLanguage = value;
                OnPropertyChanged();

                if (!_isInitializing)
                    SaveSettings(silent: true);
            }
        }

        public LanguageOption? SelectedNarrationLanguage
        {
            get => _selectedNarrationLanguage;
            set
            {
                if (Equals(_selectedNarrationLanguage, value))
                    return;

                _selectedNarrationLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelectedNarrationLanguageOnlineOnly));
                OnPropertyChanged(nameof(SelectedNarrationLanguageInfo));

                if (!_isInitializing)
                {
                    _selectedAppLanguage = value;
                    OnPropertyChanged(nameof(SelectedAppLanguage));

                    // ? CLEAR CACHE when language changes (run async off UI thread)
                    _ = Task.Run(async () => await ClearTranslationCacheAsync());

                    SaveSettings(silent: true);
                }
            }
        }

        public bool EnableAudio
        {
            get => _enableAudio;
            set
            {
                if (_enableAudio == value)
                    return;

                _enableAudio = value;
                OnPropertyChanged();
                if (!_isInitializing)
                    SaveSettings(silent: true);
            }
        }

        public bool EnableAutoNarration
        {
            get => _enableAutoNarration;
            set
            {
                if (_enableAutoNarration == value)
                    return;

                _enableAutoNarration = value;
                OnPropertyChanged();
                if (!_isInitializing)
                    SaveSettings(silent: true);
            }
        }

        public int CooldownMinutes
        {
            get => _cooldownMinutes;
            set
            {
                if (_cooldownMinutes == value)
                    return;

                _cooldownMinutes = value;
                OnPropertyChanged();
                if (!_isInitializing)
                    SaveSettings(silent: true);
            }
        }

        public int TriggerRadiusMeters
        {
            get => _triggerRadiusMeters;
            set
            {
                if (_triggerRadiusMeters == value)
                    return;

                _triggerRadiusMeters = value;
                OnPropertyChanged();
                if (!_isInitializing)
                    SaveSettings(silent: true);
            }
        }

        public bool IsDownloadingLanguagePack
        {
            get => _isDownloadingLanguagePack;
            set
            {
                if (_isDownloadingLanguagePack == value)
                    return;

                _isDownloadingLanguagePack = value;
                OnPropertyChanged();
                (DownloadLanguagePackCommand as Command)?.ChangeCanExecute();
            }
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                if (Math.Abs(_downloadProgress - value) < 0.01)
                    return;

                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public string DownloadDetails
        {
            get => _downloadDetails;
            set
            {
                if (_downloadDetails == value)
                    return;

                _downloadDetails = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelectedNarrationLanguageOnlineOnly => SelectedNarrationLanguage?.IsOnlineOnly ?? false;

        public string SelectedNarrationLanguageInfo =>
            "T\u1EA5t c\u1EA3 ng\u00F4n ng\u1EEF hi\u1EC7n d\u00F9ng d\u1EEF li\u1EC7u t\u1EEB c\u01A1 s\u1EDF d\u1EEF li\u1EC7u (offline-first).";

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DownloadLanguagePackCommand { get; }

        private void LoadSettings()
        {
            _isInitializing = true;
            try
            {
                var narrationLangCode = _settingsService.PreferredLanguage;

                SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == narrationLangCode)
                    ?? LanguageOptions[0];

                SelectedAppLanguage = SelectedNarrationLanguage;

                EnableAudio = Preferences.Get("enableAudio", true);
                EnableAutoNarration = Preferences.Get("enableAutoNarration", true);
                CooldownMinutes = Preferences.Get("cooldownMinutes", 5);
                TriggerRadiusMeters = Preferences.Get("triggerRadiusMeters", 20);

                StatusMessage = "\u2705 C\u00E0i \u0111\u1EB7t \u0111\u00E3 t\u1EA3i";
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void SaveSettings()
        {
            SaveSettings(silent: false);
        }

        private void SaveSettings(bool silent)
        {
            var narrationCode = SelectedNarrationLanguage?.CultureCode ?? "vi";
            _settingsService.PreferredLanguage = narrationCode;

            Preferences.Set("enableAudio", EnableAudio);
            Preferences.Set("enableAutoNarration", EnableAutoNarration);
            Preferences.Set("cooldownMinutes", CooldownMinutes);
            Preferences.Set("triggerRadiusMeters", TriggerRadiusMeters);

            StatusMessage = silent ? $"\u2705 \u0110\u00E3 \u00E1p d\u1EE5ng ({narrationCode})" : "\u2705 C\u00E0i \u0111\u1EB7t \u0111\u00E3 l\u01B0u";
        }

        private void Cancel()
        {
            LoadSettings();
            StatusMessage = "\u274C \u0110\u00E3 h\u1EE7y thay \u0111\u1ED5i";
        }

        private void ResetSettings()
        {
            SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == "vi") ?? LanguageOptions[0];
            SelectedAppLanguage = SelectedNarrationLanguage;
            EnableAudio = true;
            EnableAutoNarration = true;
            CooldownMinutes = 5;
            TriggerRadiusMeters = 20;

            SaveSettings();
            StatusMessage = "\uD83D\uDD04 \u0110\u00E3 reset v\u1EC1 m\u1EB7c \u0111\u1ECBnh";
        }

        /// <summary>
        /// Clear translation cache when language changes (run async to avoid blocking UI).
        /// </summary>
        private async Task ClearTranslationCacheAsync()
        {
            try
            {
                if (_poiRepository == null)
                    return;

                await _poiRepository.ClearCachedTranslationsAsync();
                Debug.WriteLine("[SettingsViewModel] ? Translation cache cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsViewModel] Error clearing cache: {ex.Message}");
            }
        }

        private async Task DownloadLanguagePackAsync()
        {
            var languageCode = SelectedNarrationLanguage?.CultureCode;
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                StatusMessage = "\u26A0\uFE0F Vui l\u00F2ng ch\u1ECDn ng\u00F4n ng\u1EEF";
                return;
            }

            if (!IsSelectedNarrationLanguageOnlineOnly)
            {
                StatusMessage = "\u2139\uFE0F Ngôn ng? này s? d?ng d? li?u t? c? s? d? li?u, không c?n t?i gói";
                return;
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                StatusMessage = "\u274C Kh\u00F4ng c\u00F3 m\u1EA1ng \u0111\u1EC3 t\u1EA3i g\u00F3i ng\u00F4n ng\u1EEF";
                return;
            }

            IsDownloadingLanguagePack = true;
            DownloadProgress = 0;
            DownloadDetails = string.Empty;
            StatusMessage = $"\u23F3 \u0110ang t\u1EA3i g\u00F3i '{languageCode}'...";

            try
            {
                // ? Create progress reporter
                var progress = new Progress<LanguagePackProgress>(report =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DownloadProgress = report.ProgressPercentage / 100.0;
                        DownloadDetails = $"{report.CurrentCount}/{report.TotalCount} POI \u2022 {report.CurrentPOIName}";
                    });
                });

                var entries = await _translationService.DownloadLanguagePackAsync(languageCode, progress);

                if (entries > 0)
                {
                    // ? Verify downloaded pack is in DB
                    var hasDownloaded = _poiRepository != null
                        ? await _poiRepository.HasDownloadedLanguagePackAsync(languageCode)
                        : false;

                    if (hasDownloaded)
                    {
                        // ? Invalidate memory cache so fresh data loads from DB
                        // Note: TranslationService doesn't use memory caching

                        StatusMessage = $"\u2705 T\u1EA3i xong g\u00F3i '{languageCode}' ({entries} m\u1EE5c)";
                        DownloadProgress = 1.0;
                        DownloadDetails = "\u0110\u00E3 l\u01B0u v\u00E0o c\u01A1 s\u1EDF d\u1EEF li\u1EC7u";
                    }
                    else
                    {
                        StatusMessage = $"?? T?i xong nh?ng l?u DB th?t b?i ({entries} m?c)";
                        DownloadProgress = 0.5;
                        DownloadDetails = "Vui l\u00F2ng ki\u1EC3m tra l\u1EA1i";
                    }
                }
                else
                {
                    StatusMessage = $"\u274C Kh\u00F4ng t\u1EA3i \u0111\u01B0\u1EE3c g\u00F3i '{languageCode}'";
                    DownloadProgress = 0;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"\u274C L\u1ED7i t\u1EA3i g\u00F3i: {ex.Message}";
                DownloadProgress = 0;
                DownloadDetails = string.Empty;
                Debug.WriteLine($"[SettingsViewModel] Download error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsDownloadingLanguagePack = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
