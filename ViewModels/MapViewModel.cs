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

        private ObservableCollection<POI> _allPOIs;
        private POI _selectedPOI;
        private double _userLatitude;
        private double _userLongitude;
        private bool _isTracking;
        private string _statusMessage;
        private int _isSyncingFromAdmin;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel(IPOIRepository poiRepository, MapService mapService, LocationService locationService)
        {
            _poiRepository = poiRepository ?? throw new ArgumentNullException(nameof(poiRepository));
            _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));

            AllPOIs = new ObservableCollection<POI>();
            StatusMessage = "Tải bản đồ...";
            IsTracking = _locationService.IsTracking;

            OpenMapCommand = new Command(async () => await OpenMap());
            RefreshCommand = new Command(async () => await RefreshPOIs());

            // Subscribe to events with null checks
            if (_locationService != null)
            {
                _locationService.LocationUpdated += OnLocationUpdated;
                _locationService.TrackingStateChanged += OnTrackingStateChanged;
            }
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
                StatusMessage = $"Vị trí: {location.Latitude:F4}, {location.Longitude:F4}";
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

        public ICommand OpenMapCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadPOIs()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MapViewModel] Starting POI load...");

                if (_poiRepository == null)
                {
                    StatusMessage = "Repository not initialized";
                    return;
                }

                var pois = await _poiRepository.GetAllPOIsAsync();

                if (pois == null || pois.Count == 0)
                {
                    StatusMessage = "No POIs loaded";
                    System.Diagnostics.Debug.WriteLine("[MapViewModel] No POIs found in database");
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPOIs = new ObservableCollection<POI>(pois);
                    StatusMessage = $"Đã tải {pois.Count} địa điểm";
                    System.Diagnostics.Debug.WriteLine($"[MapViewModel] Loaded {pois.Count} POIs successfully");
                });

                _ = TrySyncFromAdminInBackgroundAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error loading POIs: {ex.Message}\n{ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
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

                var refreshed = await _poiRepository.GetAllPOIsAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPOIs = new ObservableCollection<POI>(refreshed);
                    StatusMessage = $"Đã đồng bộ {updatedCount} địa điểm từ máy chủ";
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
                await _poiRepository.SyncPOIsFromAdminAsync();
                await LoadPOIs();
                StatusMessage = "Làm mới hoàn tất";
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
                    StatusMessage = "Chọn một địa điểm trước";
                    return;
                }

                var mapUrl = _mapService.GetMapUrl(SelectedPOI.Latitude, SelectedPOI.Longitude);
                if (string.IsNullOrWhiteSpace(mapUrl))
                {
                    StatusMessage = "Không thể tạo link bản đồ";
                    return;
                }

                var uri = new Uri(mapUrl);
                await Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Error opening map: {ex.Message}");
                StatusMessage = $"Lỗi: {ex.Message}";
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
    }
}
