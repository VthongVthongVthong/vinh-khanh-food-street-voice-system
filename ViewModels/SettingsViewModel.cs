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
    public string DisplayLabel => IsOnlineOnly ? $"{DisplayName} (online)" : DisplayName;
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
      new("vi", "Ti?ng Vi?t"),
            new("en", "English"),
    new("zh", "?? (Chinese Simplified)"),
  new("ja", "??? (Japanese)", true),
    new("ko", "??? (Korean)", true),
         new("fr", "Français (French)", true),
              new("ru", "??????? (Russian)", true)
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

        public bool IsSelectedNarrationLanguageOnlineOnly => SelectedNarrationLanguage?.IsOnlineOnly ?? false;

        public string SelectedNarrationLanguageInfo =>
        IsSelectedNarrationLanguageOnlineOnly
  ? "Ngôn ng? này c?n m?ng ho?c t?i tr??c gói ngôn ng?."
             : "Ngôn ng? này h? tr? offline s?n.";

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

     StatusMessage = "Settings loaded";
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

    StatusMessage = silent ? $"?ã áp d?ng ngay ({narrationCode})" : "Settings saved ?";
        }

        private void Cancel()
    {
 LoadSettings();
      StatusMessage = "?ã h?y thay ??i";
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
      StatusMessage = "?ã reset v? m?c ??nh";
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
        StatusMessage = "Vui lòng ch?n ngôn ng?";
          return;
     }

            if (!IsSelectedNarrationLanguageOnlineOnly)
            {
     StatusMessage = "Ngôn ng? này ?ã có offline s?n";
              return;
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
      StatusMessage = "Không có m?ng ?? t?i gói ngôn ng?";
return;
          }

IsDownloadingLanguagePack = true;
            StatusMessage = $"?ang t?i gói '{languageCode}'...";

   try
         {
  var entries = await _translationService.DownloadLanguagePackAsync(languageCode);
     StatusMessage = entries > 0
         ? $"T?i xong gói '{languageCode}' ({entries} m?c) ?"
                : $"Không t?i ???c gói '{languageCode}'";
       }
 catch (Exception ex)
            {
   StatusMessage = $"L?i t?i gói: {ex.Message}";
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
