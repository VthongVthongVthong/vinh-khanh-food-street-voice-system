using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels;

[QueryProperty(nameof(TourId), "tourId")]
public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly ITourRepository _tourRepository;
    private readonly IPOIRepository _poiRepository;
    private readonly LocalizationResourceManager _localizationManager;
    private readonly SettingsService _settingsService;
    private readonly AudioManager _audioManager;
    private readonly POICacheService _poiCache;
    private Tour? _tour;
    private List<POI> _tourPois = new();
    private bool _isLoading;
    private int _tourId;
    private string _statusMessage = string.Empty;
    private bool _isNavigating;
    private bool _avatarsLoaded = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TourDetailViewModel(
        ITourRepository tourRepository,
        IPOIRepository poiRepository,
        LocalizationResourceManager localizationManager,
        SettingsService settingsService,
        AudioManager audioManager)
    {
        _tourRepository = tourRepository;
        _poiRepository = poiRepository;
        _localizationManager = localizationManager;
        _settingsService = settingsService;
        _audioManager = audioManager;
        _poiCache = POICacheService.Instance;

        SelectPoiCommand = new Command<POI>(async (poi) => await SelectPoiAsync(poi));
        StartTourCommand = new Command(async () => await StartTourAsync());
        GoBackCommand = new Command(async () => await GoBackAsync());
    }

    public int TourId
    {
        get => _tourId;
        set
        {
            if (_tourId == value) return;
            _tourId = value;
            _ = LoadTourAsync(value);
        }
    }

    public Tour? Tour
    {
        get => _tour;
        set
        {
            if (_tour == value) return;
            _tour = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QRCodeContent));
        }
    }

    public string QRCodeContent
    {
        get
        {
            if (_tour == null) return string.Empty;
            return $"vinhkhanh://tour?id={_tour.Id}&action=play";
        }
    }


    public List<POI> TourPois
    {
        get => _tourPois;
        set
        {
            if (_tourPois == value) return;
            _tourPois = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand SelectPoiCommand { get; }
    public ICommand StartTourCommand { get; }
    public ICommand GoBackCommand { get; }

    /// <summary>
    /// Get localized description for a tour based on current language
    /// </summary>
    public string GetTourDescription(Tour tour)
    {
        if (tour == null)
            return string.Empty;

        var currentLanguage = _localizationManager.CurrentLanguage;
        return currentLanguage switch
        {
            "en" => tour.DescriptionEn ?? tour.Description ?? string.Empty,
            "zh" => tour.DescriptionZh ?? tour.Description ?? string.Empty,
            "ja" => tour.DescriptionJa ?? tour.Description ?? string.Empty,
            "ko" => tour.DescriptionKo ?? tour.Description ?? string.Empty,
            "fr" => tour.DescriptionFr ?? tour.Description ?? string.Empty,
            "ru" => tour.DescriptionRu ?? tour.Description ?? string.Empty,
            _ => tour.Description ?? string.Empty
        };
    }

    private async Task LoadTourAsync(int tourId)
    {
        try
        {
            IsLoading = true;
            _avatarsLoaded = false;
            StatusMessage = "?ang t?i thông tin l? trình...";

            // Load tour details
            Tour = await _tourRepository.GetTourByIdAsync(tourId);
            if (Tour == null)
            {
                StatusMessage = "Không tìm th?y l? trình";
                return;
            }

            // Load POIs for this tour
            var tourPoisMapping = await _tourRepository.GetTourPOIsAsync(tourId);
            var poiIds = tourPoisMapping.Select(tp => tp.POIId).ToList();

            if (poiIds.Count == 0)
            {
                StatusMessage = "L? trình không có ?i?m d?ng";
                return;
            }

            // ? TRY CACHE FIRST: Check if POIs are already cached from HomePage
            var cachedPOIs = _poiCache.GetAllCachedPOIs();
            if (cachedPOIs.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] ?? Using cached POIs: {cachedPOIs.Count} available");
                TourPois = cachedPOIs
                    .Where(p => poiIds.Contains(p.Id))
                    .OrderBy(p => tourPoisMapping.FirstOrDefault(tp => tp.POIId == p.Id)?.SortOrder ?? 0)
                    .ToList();

                if (TourPois.Count > 0)
                {
                    StatusMessage = $"?ã t?i {TourPois.Count} ?i?m d?ng t? b? nh? ??m";
                    System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] ? Loaded from cache: {TourPois.Count} POIs");
                    
                    // ? Clear message after short delay to let UI render
                    _ = Task.Delay(1500).ContinueWith(_ => StatusMessage = string.Empty);
                    return;
                }
            }

            // ? FALLBACK: If cache empty or doesn't have all POIs, fetch from repository
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] ?? Cache miss or incomplete, fetching from repository");
            var allPois = await _poiRepository.GetAllPOIsAsync();
            TourPois = allPois
                .Where(p => poiIds.Contains(p.Id))
                .OrderBy(p => tourPoisMapping.FirstOrDefault(tp => tp.POIId == p.Id)?.SortOrder ?? 0)
                .ToList();

            // ? Update cache with fetched POIs so next tour loads faster
            _poiCache.UpdateCache(allPois);

            StatusMessage = $"?ã t?i {TourPois.Count} ?i?m d?ng";
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Loaded tour '{Tour.Name}' with {TourPois.Count} POIs");
            
            // ? Clear message after short delay to let UI render
            _ = Task.Delay(1500).ContinueWith(_ => StatusMessage = string.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"L?i t?i d? li?u: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SelectPoiAsync(POI poi)
    {
        if (poi == null || _isNavigating) return;

        try
        {
            _isNavigating = true;
            // ? Clear status message before navigating to POI detail
            StatusMessage = string.Empty;
            
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Navigating to POI detail: {poi.Id}");
            await Shell.Current.GoToAsync($"///detail?poiId={poi.Id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Navigation error: {ex.Message}");
            StatusMessage = $"L?i: {ex.Message}";
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async Task StartTourAsync()
    {
        if (Tour == null || TourPois.Count == 0)
        {
            StatusMessage = "Không th? b?t ??u l? trình";
            return;
        }

        try
        {
            // Get current narration language from settings
            var narrationLanguage = _settingsService.PreferredLanguage ?? "vi";
            
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Starting tour with {TourPois.Count} POIs, language: {narrationLanguage}");

            // Pre-resolve translations for all POIs in background
            _ = _audioManager.PreResolveTranslationsAsync(TourPois, narrationLanguage);

            // Add all POIs to queue in order
            foreach (var poi in TourPois)
            {
                _audioManager.AddToQueue(poi);
            }

            StatusMessage = $"?ã thêm {TourPois.Count} ?i?m d?ng vào hàng ch? phát";
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Added {TourPois.Count} POIs to audio queue");
        }
        catch (Exception ex)
        {
            StatusMessage = $"L?i kh?i ??ng l? trình: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Start tour error: {ex.Message}");
        }
    }

    public async Task LoadTourPoiAvatarsAsync()
    {
      // Only load avatars once per tour
      if (_avatarsLoaded || TourPois == null || TourPois.Count == 0)
            return;

        try
      {
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Loading avatars for {TourPois.Count} POIs");

      // ? Check if avatars are already loaded from cache
         var alreadyLoaded = TourPois.Count(p => !string.IsNullOrWhiteSpace(p.AvatarImageUrl));
    if (alreadyLoaded == TourPois.Count)
           {
  _avatarsLoaded = true;
    System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] ? All avatars already loaded from cache, skipping reload");
     return;
}

   // Load all avatar images at once in parallel for faster loading
var avatarDict = await _poiRepository.GetAllAvatarImagesAsync();

      var loadedCount = 0;
          var fallbackCount = 0;

   // Process POIs in parallel to speed up assignment
   await Task.Run(() =>
        {
           Parallel.ForEach(TourPois, poi =>
 {
        // Skip if already has avatar
     if (!string.IsNullOrWhiteSpace(poi.AvatarImageUrl))
 {
         Interlocked.Increment(ref loadedCount);
         return;
       }

           // Priority 1: Avatar image
          if (avatarDict.TryGetValue(poi.Id, out var avatarUrl) && !string.IsNullOrWhiteSpace(avatarUrl))
             {
    poi.AvatarImageUrl = avatarUrl;
          Interlocked.Increment(ref loadedCount);
     return;
     }

         // Priority 2: Fallback from POI.ImageUrls first item
         var firstImage = poi.ImageUrlList?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (!string.IsNullOrWhiteSpace(firstImage))
        {
       poi.AvatarImageUrl = firstImage;
 Interlocked.Increment(ref fallbackCount);
          }
         });
   });

      // Refresh UI once with all images loaded
      await MainThread.InvokeOnMainThreadAsync(() =>
     {
  var current = TourPois.ToList();
        TourPois = new List<POI>(current);
         _avatarsLoaded = true;
      System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Avatar load complete: {loadedCount} from avatar table, {fallbackCount} from image list");
   });
  }
       catch (Exception ex)
       {
         System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Error loading avatars: {ex.Message}");
        }
   }

    private async Task GoBackAsync()
    {
        try
        {
            // ? Clear status message before navigating back
 StatusMessage = string.Empty;
            
 System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Going back to tour list");
      await Shell.Current.GoToAsync("///tour");
        }
        catch (Exception ex)
  {
            System.Diagnostics.Debug.WriteLine($"[TourDetailViewModel] Go back error: {ex.Message}");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
