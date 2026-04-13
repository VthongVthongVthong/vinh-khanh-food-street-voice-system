using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class POIDetailViewModel : INotifyPropertyChanged, IDisposable
    {
        public sealed record LanguageOption(string CultureCode, string DisplayName, bool IsOnlineOnly = false)
        {
            public string DisplayLabel => DisplayName;
 }

        private readonly AudioManager _audioManager;
        private readonly MapService _mapService;
   private readonly SettingsService _settingsService;
      private readonly ITranslationService _translationService;
      private readonly IPOIRepository? _poiRepository;
        private readonly LocalizationResourceManager _localizationManager;

        private POI? _selectedPOI;
        private string _statusMessage = string.Empty;
        private bool _isPlaying;
        private bool _disposed;
        private LanguageOption? _selectedNarrationLanguage;
        private string _narrationPreviewText = string.Empty;
        private string _currentDescriptionText = string.Empty;

 public event PropertyChangedEventHandler? PropertyChanged;

        public POIDetailViewModel(
     AudioManager audioManager,
  MapService mapService,
 SettingsService settingsService,
            ITranslationService translationService,
        LocalizationResourceManager localizationManager,
            IPOIRepository? poiRepository = null)
    {
            try
  {
     _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
  _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
  _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
   _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
     _localizationManager = localizationManager ?? throw new ArgumentNullException(nameof(localizationManager));
         _poiRepository = poiRepository;

       // Safe event subscription
         _audioManager.AudioStarted += OnAudioStarted;
                _audioManager.AudioCompleted += OnAudioCompleted;
       _settingsService.PreferredLanguageChanged += OnPreferredLanguageChanged;

       LanguageOptions = new List<LanguageOption>
       {
    new("vi", _localizationManager.GetString("Home_LanguageBadge") ?? "Tiếng Việt", false),
      new("en", "English", false),
     new("zh", "中文", false),
        new("ja", "日本語", false),
       new("ko", "한국어", false),
         new("fr", "Français", false),
       new("ru", "Русский", false)
    };

       SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == _settingsService.PreferredLanguage)
              ?? LanguageOptions[0];

 PlayAudioCommand = new Command(() => PlayAudio());
      StopAudioCommand = new Command(StopAudio);
     ToggleAudioCommand = new Command(() =>
    {
    if (IsPlaying) StopAudio();
        else PlayAudio();
});
     GoBackCommand = new Command(async () => await GoBack());
            OpenMapCommand = new Command(async () =>
            {
                if (SelectedPOI != null)
                {
                    await Shell.Current.GoToAsync($"///map?poiId={SelectedPOI.Id}");
                }
            });

            ShareCommand = new Command(async () =>
            {
                if (SelectedPOI != null)
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = SelectedPOI.Name,
                        Text = $"Check out {SelectedPOI.Name}!\n{SelectedPOI.Address}",
                        Uri = $"app://poi?id={SelectedPOI.Id}"
                    });
                }
            });

         System.Diagnostics.Debug.WriteLine("[POIDetailViewModel] Initialized successfully");
            }
    catch (Exception ex)
         {
       System.Diagnostics.Debug.WriteLine($"[POIDetailViewModel] Constructor error: {ex.Message}\n{ex.StackTrace}");
      throw;
        }
        }

        public POI? SelectedPOI
     {
         get => _selectedPOI;
         set
            {
          if (Equals(_selectedPOI, value))
    return;

             _selectedPOI = value;
   OnPropertyChanged();
       OnPropertyChanged(nameof(CurrentDescriptionText));
         OnPropertyChanged(nameof(CurrentTtsScriptText));
                OnPropertyChanged(nameof(QRCodeContent));

   _ = RefreshNarrationPreviewAsync();
     }
        }

        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        public string CurrentDescriptionText
        {
      get
 {
      if (_selectedPOI is null)
    return string.Empty;

        // Sync with app language, not just selected narration language
    var language = _settingsService.PreferredLanguage;
   return _selectedPOI.GetDescriptionByLanguage(language);
            }
  }

        public string CurrentTtsScriptText
        {
get
            {
        if (_selectedPOI is null)
         return string.Empty;

 // Sync with app language
              var language = _settingsService.PreferredLanguage;
    return _selectedPOI.GetTtsScriptByLanguage(language);
   }
        }

        public string QRCodeContent
      {
 get
            {
     if (_selectedPOI == null) return string.Empty;
                return $"vinhkhanh://poi?id={_selectedPOI.Id}&action=play";
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
       OnPropertyChanged(nameof(CurrentDescriptionText));
      OnPropertyChanged(nameof(CurrentTtsScriptText));
    OnPropertyChanged(nameof(QRCodeContent));

          var code = value?.CultureCode ?? "vi";
    _settingsService.PreferredLanguage = code;
          _audioManager.StopCurrent();
         IsPlaying = false;

    _ = RefreshNarrationPreviewAsync();

  MainThread.BeginInvokeOnMainThread(() =>
   {
       var languageChangedMsg = _localizationManager.GetString("POI_NarrationLanguage") ?? "Language";
                    StatusMessage = $"{languageChangedMsg}: {code}";
             });
  }
    }

        public string NarrationPreviewText
        {
        get => _narrationPreviewText;
            set
  {
     if (_narrationPreviewText == value)
        return;

    _narrationPreviewText = value;
    OnPropertyChanged();
       }
    }

      public string StatusMessage
        {
  get => _statusMessage;
    set
            {
      if (_statusMessage == value)
                return;

     _statusMessage = value;
OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
         set
          {
         if (_isPlaying == value)
           return;

 _isPlaying = value;
    OnPropertyChanged();
  }
        }

  public ICommand PlayAudioCommand { get; }
        public ICommand StopAudioCommand { get; }
  public ICommand ToggleAudioCommand { get; }
 public ICommand OpenMapCommand { get; }
    public ICommand ShareCommand { get; }
    public ICommand GoBackCommand { get; }

        // Localization String Properties
        public string POI_Title => _localizationManager.GetString("POI_Title") ?? "Chi tiết nhà hàng";
  public string POI_Images => _localizationManager.GetString("POI_Images") ?? "📷 Hình Ảnh";
  public string POI_Description => _localizationManager.GetString("POI_Description") ?? "Mô tả";
        public string POI_ViewOnMap => _localizationManager.GetString("POI_ViewOnMap") ?? "Xem trên bản đồ";
        public string POI_AudioSection => _localizationManager.GetString("POI_AudioSection") ?? "🎧 Âm Thanh Hướng Dẫn";
 public string POI_NarrationLanguage => _localizationManager.GetString("POI_NarrationLanguage") ?? "Ngôn ngữ thuyết minh";
        public string POI_SelectNarrationLanguage => _localizationManager.GetString("POI_SelectNarrationLanguage") ?? "Chọn ngôn ngữ";
        public string POI_QRCodeTitle => _localizationManager.GetString("POI_QRCodeTitle") ?? "Mã QR Thuyết Minh";
        public string POI_PlayAudio => _localizationManager.GetString("POI_PlayAudio") ?? "Phát Âm Thanh";
        public string POI_StopAudio => _localizationManager.GetString("POI_StopAudio") ?? "Dừng Âm Thanh";
        public string POI_ShareButton => _localizationManager.GetString("POI_ShareButton") ?? "📤 Chia Sẻ";

        /// <summary>
        /// Check if POI has data for this language from DB columns/cache.
        /// </summary>
    private async Task<bool> HasDataForLanguageAsync(POI poi, string languageCode)
        {
            var normalized = NormalizeLang(languageCode);

   // Check offline columns first (instant)
            if (HasOfflineDataForLanguage(poi, normalized))
   return true;

     // For online languages, check DB cache (downloaded packs)
            if (_poiRepository != null)
          {
                try
        {
 var cached = await _poiRepository.GetCachedTranslationAsync(
      poi.Id, normalized, isTtsScript: false);
            return !string.IsNullOrWhiteSpace(cached);
           }
                catch
         {
return false;
      }
        }

    return false;
        }

        /// <summary>
   /// Refresh narration preview - check if language data is available
        /// Now checks both offline AND downloaded packs
      /// </summary>
   private async Task RefreshNarrationPreviewAsync()
        {
      if (SelectedPOI is null)
            {
         NarrationPreviewText = string.Empty;
   return;
  }

            try
            {
     var language = SelectedNarrationLanguage?.CultureCode ?? _settingsService.PreferredLanguage;
     var normalized = NormalizeLang(language);

        // Check if this is an online language that requires downloading
       var isOnlineLanguage = IsOnlineLanguage(normalized);

         // Check BOTH offline AND cached data
    var hasOfflineData = HasOfflineDataForLanguage(SelectedPOI, normalized);
      var hasCachedData = await HasDataForLanguageAsync(SelectedPOI, normalized);
                var hasData = hasOfflineData || hasCachedData;

    // If online language but no data at all - show warning
   if (isOnlineLanguage && !hasData)
        {
              MainThread.BeginInvokeOnMainThread(() =>
 {
           var langName = GetLanguageName(normalized);
    var offlineMsg = _localizationManager.GetString("Settings_Status_OfflineLanguage") 
        ?? $"Vui lòng tải gói '{langName}' trong Settings để dùng";
    NarrationPreviewText = $"[{offlineMsg}]";
          StatusMessage = offlineMsg;
              });
return;
     }

                // For offline/cached languages: resolve text (will use HybridTranslationService priority system)
            var text = await _translationService.ResolveNarrationTextAsync(SelectedPOI, language, preferTtsScript: true);

   MainThread.BeginInvokeOnMainThread(() =>
       {
    var readyMsg = _localizationManager.GetString("POI_Ready") ?? "Sẵn sàng phát";
      NarrationPreviewText = string.IsNullOrWhiteSpace(text) ? (SelectedPOI.TtsScript ?? SelectedPOI.DescriptionText) : text;
    StatusMessage = $"{readyMsg}... ({normalized.ToUpper()})";
     });
   }
            catch (Exception ex)
   {
    System.Diagnostics.Debug.WriteLine($"[POIDetailViewModel] Refresh narration preview error: {ex.Message}");
     MainThread.BeginInvokeOnMainThread(() =>
       {
      var errorMsg = _localizationManager.GetString("Common_Error") ?? "Lỗi khi tải dữ liệu";
             NarrationPreviewText = SelectedPOI?.TtsScript ?? SelectedPOI?.DescriptionText ?? string.Empty;
    StatusMessage = errorMsg;
         });
 }
        }

        /// <summary>
      /// Play audio - check if language data is available first
     /// </summary>
        private async void PlayAudio()
        {
   if (SelectedPOI is null)
             return;

            try
    {
         var language = SelectedNarrationLanguage?.CultureCode ?? _settingsService.PreferredLanguage;
           var normalized = NormalizeLang(language);

   // Check if this is an online language
    var isOnlineLanguage = IsOnlineLanguage(normalized);

      // Check BOTH offline AND cached data
        var hasOfflineData = HasOfflineDataForLanguage(SelectedPOI, normalized);
      var hasCachedData = await HasDataForLanguageAsync(SelectedPOI, normalized);
var hasData = hasOfflineData || hasCachedData;

     if (isOnlineLanguage && !hasData)
      {
      var langName = GetLanguageName(normalized);
        var offlineMsg = _localizationManager.GetString("Settings_Status_OfflineLanguage") 
      ?? $"Vui lòng tải gói '{langName}' trong Settings";
                    StatusMessage = offlineMsg;
         return;
  }

     // Can play - data is available (offline or cached)
        _audioManager.AddToQueue(SelectedPOI);
           var playingMsg = _localizationManager.GetString("POI_Playing") ?? "Đang phát âm thanh...";
   StatusMessage = playingMsg;
            }
            catch (Exception ex)
  {
                var errorMsg = _localizationManager.GetString("Common_Error") ?? "Lỗi";
  StatusMessage = $"{errorMsg}: {ex.Message}";
    }
        }

     private void StopAudio()
        {
    _audioManager.StopCurrent();
            IsPlaying = false;
            var stoppedMsg = _localizationManager.GetString("POI_StopAudio") ?? "Đã dừng";
        StatusMessage = stoppedMsg;
        }

     private async Task OpenMap()
        {
  if (SelectedPOI is null)
       return;

        try
       {
 var url = _mapService.GetMapUrl(SelectedPOI.Latitude, SelectedPOI.Longitude);

        if (string.IsNullOrWhiteSpace(url))
      {
                var mapErrorMsg = _localizationManager.GetString("POI_ViewOnMap") 
                  ?? "Không tạo được link bản đồ";
  StatusMessage = mapErrorMsg;
       return;
 }

              if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
 {
    var invalidLinkMsg = _localizationManager.GetString("Common_Error") 
    ?? "Link bản đồ không hợp lệ";
    StatusMessage = invalidLinkMsg;
return;
          }

     await Launcher.OpenAsync(uri);
        }
catch (Exception ex)
   {
       var errorMsg = _localizationManager.GetString("Common_Error") ?? "Lỗi";
            StatusMessage = $"{errorMsg}: {ex.Message}";
            }
        }

        private async Task SharePOI()
        {
 if (SelectedPOI is null)
      return;

         try
    {
         await Share.RequestAsync(new ShareTextRequest
                {
      Title = SelectedPOI.Name,
      Text = $"{SelectedPOI.Name}: {SelectedPOI.DescriptionText}",
 Uri = SelectedPOI.MapLink
          });
     }
    catch (Exception ex)
            {
              var shareErrorMsg = _localizationManager.GetString("POI_Share") 
   ?? "Lỗi chia sẻ";
  StatusMessage = $"{shareErrorMsg}: {ex.Message}";
         }
    }

     private async Task GoBack()
      {
         try
    {
              var shell = Shell.Current;
      if (shell is null)
             return;

       var navigation = shell.Navigation;

   if (navigation.ModalStack.Count > 0)
            {
           await navigation.PopModalAsync();
     return;
                }

         if (navigation.NavigationStack.Count > 1)
         {
           await shell.GoToAsync("..");
        return;
        }

           await shell.GoToAsync("//home");
    }
     catch (Exception ex)
   {
        var errorMsg = _localizationManager.GetString("Common_Error") ?? "Lỗi";
          StatusMessage = $"{errorMsg}: {ex.Message}";
       }
    }

        private void OnPreferredLanguageChanged(object? sender, string language)
        {
      var selected = LanguageOptions.FirstOrDefault(x => x.CultureCode == language) ?? LanguageOptions[0];
            if (!Equals(SelectedNarrationLanguage, selected))
    {
       _selectedNarrationLanguage = selected;
OnPropertyChanged(nameof(SelectedNarrationLanguage));
            }

            // Force refresh data (not just update picker) and description
  OnPropertyChanged(nameof(CurrentDescriptionText));
OnPropertyChanged(nameof(CurrentTtsScriptText));
            OnPropertyChanged(nameof(QRCodeContent));
            _ = RefreshNarrationPreviewAsync();
        }

        private void OnAudioStarted(object? sender, POI? poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
   {
      IsPlaying = true;
        var playingMsg = _localizationManager.GetString("POI_Playing") ?? "Đang phát...";
                StatusMessage = poi is null ? playingMsg : $"{playingMsg}: {poi.Name}";
    });
        }

      private void OnAudioCompleted(object? sender, POI? poi)
 {
          MainThread.BeginInvokeOnMainThread(() =>
      {
           IsPlaying = false;
  var completedMsg = _localizationManager.GetString("Home_Status_AudioCompleted") 
 ?? "Hoàn tất";
                StatusMessage = completedMsg;
            });
        }

        /// <summary>
        /// Check if language is online (requires downloading)
    /// </summary>
        private static bool IsOnlineLanguage(string languageCode)
        {
    return false;
  }

      /// <summary>
      /// Check if POI has offline data for this language
   /// </summary>
        private static bool HasOfflineDataForLanguage(POI poi, string languageCode)
        {
        if (poi == null)
                return false;

      return languageCode switch
          {
"en" => !string.IsNullOrEmpty(poi.DescriptionEn) || !string.IsNullOrEmpty(poi.TtsScriptEn),
           "zh" => !string.IsNullOrEmpty(poi.DescriptionZh) || !string.IsNullOrEmpty(poi.TtsScriptZh),
                "ja" => !string.IsNullOrEmpty(poi.DescriptionJa) || !string.IsNullOrEmpty(poi.TtsScriptJa),
              "ko" => !string.IsNullOrEmpty(poi.DescriptionKo) || !string.IsNullOrEmpty(poi.TtsScriptKo),
     "fr" => !string.IsNullOrEmpty(poi.DescriptionFr) || !string.IsNullOrEmpty(poi.TtsScriptFr),
        "ru" => !string.IsNullOrEmpty(poi.DescriptionRu) || !string.IsNullOrEmpty(poi.TtsScriptRu),
         _ => true
 };
        }

     /// <summary>
        /// Get display name for language code
      /// </summary>
        private static string GetLanguageName(string languageCode)
  {
        return languageCode switch
 {
         "en" => "English",
     "zh" => "中文",
                "ja" => "日本語",
      "ko" => "한국어",
       "fr" => "Français",
 "ru" => "Русский",
  "vi" => "Tiếng Việt",
    _ => languageCode
  };
        }

        /// <summary>
        /// Normalize language code: "en-US" → "en", "vi-VN" → "vi"
        /// </summary>
 private static string NormalizeLang(string? code)
        {
      if (string.IsNullOrWhiteSpace(code)) return "vi";
            var trimmed = code.Trim().ToLowerInvariant();
            var dashIndex = trimmed.IndexOf('-');
return dashIndex > 0 ? trimmed[..dashIndex] : trimmed;
        }

        public void Dispose()
        {
            if (_disposed)
             return;

            _disposed = true;
  _audioManager.AudioStarted -= OnAudioStarted;
            _audioManager.AudioCompleted -= OnAudioCompleted;
 _settingsService.PreferredLanguageChanged -= OnPreferredLanguageChanged;
        }

        public void RefreshLocalizationStrings()
        {
     OnPropertyChanged("POI_Title");
 OnPropertyChanged("POI_Images");
      OnPropertyChanged("POI_Description");
         OnPropertyChanged("POI_AudioSection");
 OnPropertyChanged("POI_NarrationLanguage");
   OnPropertyChanged("POI_SelectNarrationLanguage");
  OnPropertyChanged("POI_QRCodeTitle");
      OnPropertyChanged("POI_PlayAudio");
   OnPropertyChanged("POI_StopAudio");
   OnPropertyChanged("POI_ShareButton");
        }

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
      {
   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


