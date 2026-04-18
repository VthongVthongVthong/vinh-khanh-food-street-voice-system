using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly IPOIRepository _poiRepository;
        private readonly MapService _mapService;
        private readonly LocationService _locationService;
        private readonly AudioManager _audioManager;
        private readonly SettingsService _settingsService;
        private readonly MapHeatmapService _heatmapService;

        private ObservableCollection<POI> _allPOIs;
        private POI _selectedPOI;
        private double _userLatitude;
        private double _userLongitude;
        private bool _isTracking;
        private bool _isNavigating;
        private string _statusMessage;
        private int _isSyncingFromAdmin;

        // Heatmap Properties
        private int _selectedHour = 16;
        private Dictionary<int, double> _hotScores = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel(
            IPOIRepository poiRepository,
            MapService mapService,
            LocationService locationService,
            AudioManager audioManager,
            SettingsService settingsService,
            MapHeatmapService heatmapService)
        {
            _poiRepository = poiRepository ?? throw new ArgumentNullException(nameof(poiRepository));
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _heatmapService = heatmapService ?? throw new ArgumentNullException(nameof(heatmapService));

            AllPOIs = new ObservableCollection<POI>();
            StatusMessage = "T?i b?n d?...";
            IsTracking = _locationService.IsTracking;

            OpenMapCommand = new Command(async () => await OpenMap());
            RefreshCommand = new Command(async () => await RefreshPOIs());
            OpenDetailCommand = new Command<POI>(async (poi) => await OpenDetailAsync(poi));
            PlayAudioCommand = new Command<POI>((poi) => PlayAudio(poi));

            // Subscribe to events with null checks
            if (_locationService != null)
            {
                _locationService.LocationUpdated += OnLocationUpdated;
                _locationService.TrackingStateChanged += OnTrackingStateChanged;
            }

            // Subscribe to narration language changes
            if (_settingsService != null)
            {
                _settingsService.PreferredLanguageChanged += OnPreferredLanguageChanged;
            }

            SetDefaultTimeSlider();
        }

        private void SetDefaultTimeSlider()
        {
            int currentHour = DateTime.Now.Hour; // Trả về giá trị từ 0 đến 23

            // Nếu người dùng mở app trong khoảng từ 16h đến 23h59
            if (currentHour >= 16 && currentHour <= 23)
            {
                SelectedHour = currentHour;
            }
            // Trường hợp đặc biệt: Nếu là đúng 12h đêm (0h sáng), gán bằng mốc 24 trên Slider
            else if (currentHour == 0)
            {
                SelectedHour = 24; 
            }
            // Nếu nằm ngoài vùng này (từ 1h sáng đến 15h chiều)
            else
            {
                SelectedHour = 16;
            }
        }

        private void OnPreferredLanguageChanged(object? sender, string language)
        {
            // Trigger refresh when narration language changes
            OnPropertyChanged(nameof(Map_NarrationLanguage));
        }

        public async Task EnsurePOIsLoadedAsync()
        {
            if (AllPOIs.Count > 0)
                return;

            await LoadPOIs();
        }

        private void OnTrackingStateChanged(object sender, bool isTracking)
        {
            try
            {
                IsTracking = isTracking;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error in OnTrackingStateChanged: {ex.Message}");
            }
        }

        private void OnLocationUpdated(object sender, Location location)
        {
            try
            {
                if (location == null)
                    return;

                UserLatitude = location.Latitude;
                UserLongitude = location.Longitude;
                StatusMessage = $"V? tr�: {location.Latitude:F4}, {location.Longitude:F4}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error in OnLocationUpdated: {ex.Message}");
            }
        }

        public ObservableCollection<POI> AllPOIs
        {
            get => _allPOIs;
            set { _allPOIs = value; OnPropertyChanged(); }
        }

        public POI SelectedPOI
        {
            get => _selectedPOI;
            set { _selectedPOI = value; OnPropertyChanged(); }
        }

        public double UserLatitude
        {
            get => _userLatitude;
            set { _userLatitude = value; OnPropertyChanged(); }
        }

        public double UserLongitude
        {
            get => _userLongitude;
            set { _userLongitude = value; OnPropertyChanged(); }
        }

        public bool IsTracking
        {
            get => _isTracking;
            set { _isTracking = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int SelectedHour
        {
            get => _selectedHour;
            set 
            { 
                if (_selectedHour != value)
                {
                    _selectedHour = value; 
                    OnPropertyChanged(); 
                    _ = RefreshHotScoresAsync();
                }
            }
        }

        public Dictionary<int, double> HotScores
        {
            get => _hotScores;
            private set
            {
                _hotScores = value;
                OnPropertyChanged();
            }
        }

        public async Task RefreshHotScoresAsync()
        {
            var day = DateTime.Now.DayOfWeek;
            int h = SelectedHour == 24 ? 0 : SelectedHour;
            var scores = await _heatmapService.GetHotScoresAsync(day, h);
            
            // To run on MainThread so UI gets updated bindings if necessary
            MainThread.BeginInvokeOnMainThread(() => {
                HotScores = scores;
            });
        }

        public ICommand OpenMapCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenDetailCommand { get; }
        public ICommand PlayAudioCommand { get; }

        // Localization String Properties
        public string Map_Listen => LocalizationResourceManager.Instance.GetString("Map_Listen") ?? "Nghe";
        public string Map_ViewDetails => LocalizationResourceManager.Instance.GetString("Map_ViewDetails") ?? "Xem chi tiết";
        public string Map_NarrationLanguage => GetNarrationLanguageName();
        public string Map_Locations => LocalizationResourceManager.Instance.GetString("Map_Locations") ?? "địa điểm";
        public string Map_Stats_Explored => LocalizationResourceManager.Instance.GetString("Map_Stats_Explored") ?? "Khám phá";
        public string Map_Stats_Listened => LocalizationResourceManager.Instance.GetString("Map_Stats_Listened") ?? "Lắng nghe";

        private string GetNarrationLanguageName()
        {
            var langCode = _settingsService.PreferredLanguage;
            return langCode switch
            {
                "vi" => "Tiếng Việt",
                "en" => "English",
                "zh" => "中文",
                "ja" => "日本語",
                "ko" => "한국어",
                "fr" => "Français",
                "ru" => "Русский",
                _ => "Tiếng Việt"
            };
        }

        private async Task LoadPOIs()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MapViewModel] Starting POI load...");
                
                await RefreshHotScoresAsync();

                if (_poiRepository == null)
                {
                    StatusMessage = "Repository not initialized";
                    return;
                }

                var pois = await _poiRepository.GetActivePOIsAsync();

                if (pois == null || pois.Count == 0)
                {
                    StatusMessage = "No POIs loaded";
                    System.Diagnostics.Debug.WriteLine("[MapViewModel] No POIs found in database");
                    return;
                }

                // Load banner & avatar images
                try
                {
                    var bannerDict = await _poiRepository.GetAllBannerImagesAsync();
                    var avatarDict = await _poiRepository.GetAllAvatarImagesAsync();
                    foreach (var poi in pois)
                    {
                        if (bannerDict.TryGetValue(poi.Id, out var bannerUrl) && !string.IsNullOrWhiteSpace(bannerUrl))
                        {
                            poi.BannerImageUrl = bannerUrl;
                        }
                        if (avatarDict.TryGetValue(poi.Id, out var avatarUrl) && !string.IsNullOrWhiteSpace(avatarUrl))
                        {
                            poi.AvatarImageUrl = avatarUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error loading images: {ex.Message}");
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPOIs = new ObservableCollection<POI>(pois);
                    StatusMessage = $"�� t?i {pois.Count} d?a di?m";
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Loaded {pois.Count} POIs successfully");
                });

                _ = TrySyncFromAdminInBackgroundAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error loading POIs: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = $"L?i t?i d? li?u: {ex.Message}";
                });
            }
        }

        private async Task TrySyncFromAdminInBackgroundAsync()
        {
            if (Interlocked.Exchange(ref _isSyncingFromAdmin, 1) == 1)
                return;

            try
            {
                var updatedCount = await _poiRepository.SyncPOIsFromAdminAsync();
                if (updatedCount <= 0)
                    return;

                var refreshed = await _poiRepository.GetActivePOIsAsync();

                try
                {
                    var bannerDict = await _poiRepository.GetAllBannerImagesAsync();
                    var avatarDict = await _poiRepository.GetAllAvatarImagesAsync();
                    foreach (var poi in refreshed)
                    {
                        if (bannerDict.TryGetValue(poi.Id, out var bannerUrl) && !string.IsNullOrWhiteSpace(bannerUrl))
                            poi.BannerImageUrl = bannerUrl;
                        if (avatarDict.TryGetValue(poi.Id, out var avatarUrl) && !string.IsNullOrWhiteSpace(avatarUrl))
                            poi.AvatarImageUrl = avatarUrl;
                    }
                }
                catch { }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPOIs = new ObservableCollection<POI>(refreshed);
                    StatusMessage = $"�� d?ng b? {updatedCount} d?a di?m t? m�y ch?";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Admin sync skipped/error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isSyncingFromAdmin, 0);
            }
        }

        private async Task RefreshPOIs()
        {
            try
            {
                StatusMessage = "Đang làm mới...";
                await _poiRepository.SyncPOIsFromAdminAsync(force: true);
                await LoadPOIs();
                StatusMessage = "Làm mới hoàn tất.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error refreshing POIs: {ex.Message}");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        private async Task OpenMap()
        {
            try
            {
                if (SelectedPOI == null)
                {
                    StatusMessage = "Ch?n m?t d?a di?m tru?c";
                    return;
                }

                var mapUrl = _mapService.GetMapUrl(SelectedPOI.Latitude, SelectedPOI.Longitude);
                if (string.IsNullOrWhiteSpace(mapUrl))
                {
                    StatusMessage = "Kh�ng th? t?o link b?n d?";
                    return;
                }

                var uri = new Uri(mapUrl);
                await Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error opening map: {ex.Message}");
                StatusMessage = $"L?i: {ex.Message}";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error in OnPropertyChanged({propertyName}): {ex.Message}");
            }
        }

        private async Task OpenDetailAsync(POI poi)
        {
            if (poi == null || _isNavigating) return;
            
            try
            {
                _isNavigating = true;
                await Shell.Current.GoToAsync($"detail?poiId={poi.Id}", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Navigation error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void PlayAudio(POI poi)
        {
            if (poi == null) return;

            try
            {
                _audioManager.AddToQueue(poi);
                StatusMessage = "?ang ph?t ?m thanh...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Play audio error: {ex.Message}");
                StatusMessage = $"? L?i: {ex.Message}";
            }
        }

        public void RefreshLocalizationStrings()
        {
     OnPropertyChanged("Map_Listen");
   OnPropertyChanged("Map_ViewDetails");
    OnPropertyChanged("Map_NarrationLanguage");
       OnPropertyChanged("Map_Locations");
   OnPropertyChanged("Map_Stats_Explored");
  OnPropertyChanged("Map_Stats_Listened");
  }
    }
}
