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
        private readonly LocalizationService _localizationService;
        private readonly LocalizationResourceManager _localizationResourceManager;

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
        private bool _isApplyingLanguage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel(SettingsService settingsService, ITranslationService translationService, IPOIRepository? poiRepository = null)
        {
            _settingsService = settingsService;
            _translationService = translationService;
            _poiRepository = poiRepository;
            _localizationService = LocalizationService.Instance;
            _localizationResourceManager = LocalizationResourceManager.Instance;

            ResetSettingsCommand = new Command(ResetSettings);
            DownloadLanguagePackCommand = new Command(async () => await DownloadLanguagePackAsync(), () => !IsDownloadingLanguagePack);

            LanguageOptions = new List<LanguageOption>
        {
            new("vi", "Tiếng Việt"),
            new("en", "English"),
            new("zh", "中文 (简体)"),
            new("ja", "日本語"),
            new("ko", "한국어"),
            new("fr", "Français"),
            new("ru", "Русский")
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
                {
                    _ = ApplyLanguageAsync(value?.CultureCode ?? "vi");
                }
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
                    // ? ANR FIX: Defer cache clearing to background thread
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
   "All languages use data from database (offline-first).";

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ResetSettingsCommand { get; }
        public ICommand DownloadLanguagePackCommand { get; }

        private void LoadSettings()
        {
            _isInitializing = true;
            try
            {
                // ? FIX #2: Check stored UI language preference
                var storedAppLangCode = Preferences.Get("appLanguage", _localizationService.CurrentLanguage);
                
              var narrationLangCode = _settingsService.PreferredLanguage;
                var appLangCode = _localizationService.CurrentLanguage;

      SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == narrationLangCode)
   ?? LanguageOptions[0];

                // ? Use stored preference if available, fallback to current
       SelectedAppLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == storedAppLangCode)
            ?? LanguageOptions.FirstOrDefault(x => x.CultureCode == appLangCode)
       ?? SelectedNarrationLanguage;

          EnableAudio = Preferences.Get("enableAudio", true);
     EnableAutoNarration = Preferences.Get("enableAutoNarration", true);
           CooldownMinutes = Preferences.Get("cooldownMinutes", 5);
        TriggerRadiusMeters = Preferences.Get("triggerRadiusMeters", 20);

      // ? ANR FIX: Defer string loading to background thread to avoid blocking UI
       _ = Task.Run(() =>
                {
       MainThread.BeginInvokeOnMainThread(() =>
           {
               StatusMessage = LocalizationService.GetString("Settings_Status_Loaded");
 });
      });
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

  // ? ANR FIX: Defer string loading to avoid blocking
       if (silent)
          {
       StatusMessage = LocalizationService.GetString("Settings_Status_Applied").Replace("{0}", narrationCode);
    }
  else
 {
       StatusMessage = LocalizationService.GetString("Settings_Status_Saved");
        }
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
      StatusMessage = LocalizationService.GetString("Settings_Status_Reset");
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
                StatusMessage = LocalizationService.GetString("Settings_Status_SelectLanguage");
                return;
            }

            if (!IsSelectedNarrationLanguageOnlineOnly)
            {
                StatusMessage = LocalizationService.GetString("Settings_Status_OfflineLanguage");
                return;
            }

            // Removed Connectivity.Current.NetworkAccess check for Android emulator/device compatibility.
            // If offline, the HttpClient will simply fail gracefully.
            
            IsDownloadingLanguagePack = true;
            DownloadProgress = 0;
            DownloadDetails = string.Empty;
            StatusMessage = LocalizationService.GetString("Settings_Status_Downloading").Replace("{0}", languageCode);

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

                        StatusMessage = LocalizationService.GetString("Settings_Status_Downloaded").Replace("{0}", languageCode).Replace("{1}", entries.ToString());
                        DownloadProgress = 1.0;
                        DownloadDetails = LocalizationService.GetString("Settings_Download_Details");
                    }
                    else
                    {
                        StatusMessage = LocalizationService.GetString("Settings_Status_Downloaded").Replace("{0}", languageCode).Replace("{1}", entries.ToString());
                        DownloadProgress = 0.5;
                        DownloadDetails = "Vui lòng ki?m tra l?i";
                    }
                }
                else
                {
                    StatusMessage = LocalizationService.GetString("Settings_Status_Downloaded").Replace("{0}", languageCode).Replace("{1}", "0");
                    DownloadProgress = 0;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"? L?i t?i gói: {ex.Message}";
                DownloadProgress = 0;
                DownloadDetails = string.Empty;
                Debug.WriteLine($"[SettingsViewModel] Download error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsDownloadingLanguagePack = false;
            }
        }

        public bool IsApplyingLanguage
        {
            get => _isApplyingLanguage;
            set
            {
                if (_isApplyingLanguage == value)
                    return;

                _isApplyingLanguage = value;
                OnPropertyChanged();
            }
        }

        private async Task ApplyLanguageAsync(string langCode)
        {
            try
            {
                IsApplyingLanguage = true;
                StatusMessage = LocalizationService.GetString("Common_Loading");

                // ? ANR FIX: Run on background thread without blocking
     await Task.Run(() =>
     {
       _localizationService.CurrentLanguage = langCode;
 // ? Load on background thread - fully async, non-blocking
 _localizationResourceManager.LoadResourcesForLanguage(langCode);
    });

                // ? FIX #2: Save the UI language preference
                Preferences.Set("appLanguage", langCode);

                SaveSettings(silent: true);

           await MainThread.InvokeOnMainThreadAsync(() =>
        {
    OnPropertyChanged(nameof(SelectedAppLanguage));
    StatusMessage = LocalizationService.GetString("Settings_Status_Applied").Replace("{0}", langCode);
    });
            }
    catch (Exception ex)
        {
       StatusMessage = $"Language switch error: {ex.Message}";
            }
    finally
            {
IsApplyingLanguage = false;
}
        }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
