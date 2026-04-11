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
 IPOIRepository? poiRepository = null)
    {
      try
            {
        _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
      _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
                _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
                _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
       _poiRepository = poiRepository;

       // ✅ Safe event subscription
    _audioManager.AudioStarted += OnAudioStarted;
           _audioManager.AudioCompleted += OnAudioCompleted;
           _settingsService.PreferredLanguageChanged += OnPreferredLanguageChanged;

                LanguageOptions = new List<LanguageOption>
         {
           new("vi", "Tiếng Việt", false),
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
  StatusMessage = $"Language changed: {code}";
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
      public ICommand StopAudioCommand { get; }        public ICommand ToggleAudioCommand { get; }        public ICommand OpenMapCommand { get; }
        public ICommand ShareCommand { get; }
        public ICommand GoBackCommand { get; }

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
        /// ✅ Now checks both offline AND downloaded packs
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

       // ✅ Check if this is an online language that requires downloading
       var isOnlineLanguage = IsOnlineLanguage(normalized);
  
      // ✅ Check BOTH offline AND cached data
                var hasOfflineData = HasOfflineDataForLanguage(SelectedPOI, normalized);
     var hasCachedData = await HasDataForLanguageAsync(SelectedPOI, normalized);
 var hasData = hasOfflineData || hasCachedData;

          // If online language but no data at all → show warning
       if (isOnlineLanguage && !hasData)
                {
          MainThread.BeginInvokeOnMainThread(() =>
      {
         NarrationPreviewText = $"[⬇️ Cần tải gói '{GetLanguageName(normalized)}' trong Settings trước khi sử dụng]";
     StatusMessage = $"⚠️ Vui lòng tải gói '{GetLanguageName(normalized)}' trong Settings để dùng";
      });
           return;
                }

             // ✅ For offline/cached languages: resolve text (will use HybridTranslationService priority system)
       var text = await _translationService.ResolveNarrationTextAsync(SelectedPOI, language, preferTtsScript: true);

        MainThread.BeginInvokeOnMainThread(() =>
     {
      NarrationPreviewText = string.IsNullOrWhiteSpace(text) ? (SelectedPOI.TtsScript ?? SelectedPOI.DescriptionText) : text;
  StatusMessage = $"Sẵn sàng phát... ({normalized.ToUpper()})";
        });
   }
    catch (Exception ex)
   {
        System.Diagnostics.Debug.WriteLine($"[POIDetailViewModel] Refresh narration preview error: {ex.Message}");
        MainThread.BeginInvokeOnMainThread(() =>
{
           NarrationPreviewText = SelectedPOI?.TtsScript ?? SelectedPOI?.DescriptionText ?? string.Empty;
            StatusMessage = "❌ Lỗi khi tải dữ liệu";
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

    // ✅ Check if this is an online language
                var isOnlineLanguage = IsOnlineLanguage(normalized);
  
      // ✅ Check BOTH offline AND cached data
             var hasOfflineData = HasOfflineDataForLanguage(SelectedPOI, normalized);
     var hasCachedData = await HasDataForLanguageAsync(SelectedPOI, normalized);
                var hasData = hasOfflineData || hasCachedData;  // ✅ Check both!

 if (isOnlineLanguage && !hasData)
           {
           StatusMessage = $"❌ Vui lòng tải gói '{GetLanguageName(normalized)}' trong Settings trước";
           return;
          }

   // ✅ Can play - data is available (offline or cached)
      _audioManager.AddToQueue(SelectedPOI);
         StatusMessage = "Đang phát âm thanh...";
  }
        catch (Exception ex)
     {
  StatusMessage = $"❌ Lỗi: {ex.Message}";
   }
        }

        private void StopAudio()
        {
  _audioManager.StopCurrent();
    IsPlaying = false;
    StatusMessage = "⏹️ Đã dừng";
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
    StatusMessage = "❌ Không tạo được link bản đồ";
    return;
 }

if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
     {
            StatusMessage = "❌ Link bản đồ không hợp lệ";
      return;
        }

       await Launcher.OpenAsync(uri);
    }
    catch (Exception ex)
            {
                StatusMessage = $"❌ Lỗi: {ex.Message}";
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
       StatusMessage = $"❌ Lỗi chia sẻ: {ex.Message}";
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
       await navigation.PopAsync();
                    return;
                }

       await shell.GoToAsync("//home");
    }
          catch (Exception ex)
       {
      StatusMessage = $"❌ Lỗi: {ex.Message}";
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

         // ✅ Force refresh data (not just update picker) and description
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
          StatusMessage = poi is null ? "Đang phát..." : $"Đang phát: {poi.Name}";
        });
     }

    private void OnAudioCompleted(object? sender, POI? poi)
        {
       MainThread.BeginInvokeOnMainThread(() =>
    {
         IsPlaying = false;
         StatusMessage = "Hoàn tất";
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

   protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


