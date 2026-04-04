using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly LocationService _locationService;
        private readonly GeofenceEngine _geofenceEngine;
        private readonly IPOIRepository _poiRepository;
        private readonly AudioManager _audioManager;

        private readonly List<POI> _allPOIs = new();
        private ObservableCollection<POI> _nearbyPOIs;
        private string _statusMessage;
        private bool _isLocationServiceRunning;
        private double _userLatitude;
        private double _userLongitude;
        private POI? _selectedPOI;
        private bool _isLoading;
        private string _searchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public HomeViewModel(
            LocationService locationService,
            GeofenceEngine geofenceEngine,
            IPOIRepository poiRepository,
            AudioManager audioManager)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _geofenceEngine = geofenceEngine ?? throw new ArgumentNullException(nameof(geofenceEngine));
            _poiRepository = poiRepository ?? throw new ArgumentNullException(nameof(poiRepository));
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));

            NearbyPOIs = new ObservableCollection<POI>();
            StatusMessage = "Ứng dụng sẵn sàng. Nhấn START để bắt đầu theo dõi vị trí.";
            IsLoading = false;

            StartLocationServiceCommand = new Command(async () => await StartLocationService());
            StopLocationServiceCommand = new Command(async () => await StopLocationService());
            OpenDetailCommand = new Command<POI>(async poi => await OpenDetailAsync(poi));

            _locationService.LocationUpdated += OnLocationUpdated;
            _geofenceEngine.POITriggered += OnPOITriggered;
            _audioManager.AudioStarted += OnAudioStarted;
            _audioManager.AudioCompleted += OnAudioCompleted;
        }

        public ObservableCollection<POI> NearbyPOIs
        {
            get => _nearbyPOIs;
            set { _nearbyPOIs = value; OnPropertyChanged(); }
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
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLocationServiceRunning
        {
            get => _isLocationServiceRunning;
            set { _isLocationServiceRunning = value; OnPropertyChanged(); }
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

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public ICommand StartLocationServiceCommand { get; }
        public ICommand StopLocationServiceCommand { get; }
        public ICommand OpenDetailCommand { get; }

        public async Task EnsureInitialDataLoadedAsync()
        {
            if (_allPOIs.Count > 0 || IsLoading)
                return;

            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải dữ liệu...";

                if (_poiRepository == null)
                {
                    StatusMessage = "Lỗi: Repository không được khởi tạo";
                    IsLoading = false;
                    return;
                }

                var allPOIs = await _poiRepository.GetActivePOIsAsync();
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Loaded {allPOIs?.Count ?? 0} active POIs from database");

                if (allPOIs == null || allPOIs.Count == 0)
                {
                    StatusMessage = "Không có điểm của lãi nào. Kiểm tra dữ liệu cơ sở dữ liệu.";
                    IsLoading = false;
                    return;
                }

                _allPOIs.Clear();
                _allPOIs.AddRange(allPOIs);
                ApplyFilter();
                StatusMessage = $"Đã tải {allPOIs.Count} điểm của lãi. Nhấn START để bắt đầu.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Error loading initial data: {ex.Message}\n{ex.StackTrace}");
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            if (_allPOIs.Count == 0)
                return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allPOIs
                : _allPOIs.Where(p =>
                        (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.DescriptionText) && p.DescriptionText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.Address) && p.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                NearbyPOIs.Clear();
                foreach (var poi in filtered)
                {
                    NearbyPOIs.Add(poi);
                }
            });
        }

        private async Task OpenDetailAsync(POI? poi)
        {
            if (poi is null)
                return;

            try
            {
                await Shell.Current.GoToAsync($"//home/detail?poiId={poi.Id}", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Navigation error: {ex}");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                SelectedPOI = null;
            }
        }

        private async Task StartLocationService()
        {
            var permission = await _locationService.CheckAndRequestLocationPermission();
            if (permission != PermissionStatus.Granted)
            {
                StatusMessage = "Quyền truy cập vị trí bị từ chối";
                return;
            }

            await _locationService.StartListening();
            IsLocationServiceRunning = true;
            StatusMessage = "Đang theo dõi vị trí...";
        }

        private async Task StopLocationService()
        {
            await _locationService.StopListening();
            IsLocationServiceRunning = false;
            StatusMessage = "Dừng theo dõi vị trí";
        }

        private void OnLocationUpdated(object sender, Location location)
        {
            UserLatitude = location.Latitude;
            UserLongitude = location.Longitude;
            StatusMessage = $"Vị trí: {location.Latitude:F5}, {location.Longitude:F5}";

            _ = _geofenceEngine.CheckPOIs(location);
        }

        private void OnPOITriggered(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Phát hành: {poi.Name}";

                if (!NearbyPOIs.Contains(poi))
                    NearbyPOIs.Add(poi);

                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] POI triggered and added: {poi.Name}");
            });
        }

        private void OnAudioStarted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() => { StatusMessage = $"Đang phát: {poi.Name}"; });
        }

        private void OnAudioCompleted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() => { StatusMessage = "Hoàn tất phát âm thanh"; });
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
