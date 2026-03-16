using System.Collections.ObjectModel;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using System.ComponentModel;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly LocationService _locationService;
        private readonly GeofenceEngine _geofenceEngine;
        private readonly IPOIRepository _poiRepository;
        private readonly AudioManager _audioManager;

        private ObservableCollection<POI> _nearbyPOIs;
        private string _statusMessage;
        private bool _isLocationServiceRunning;
        private double _userLatitude;
        private double _userLongitude;

        public event PropertyChangedEventHandler PropertyChanged;

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

            StartLocationServiceCommand = new Command(async () => await StartLocationService());
            StopLocationServiceCommand = new Command(async () => await StopLocationService());

            // Subscribe to events safely
            try
            {
                _locationService.LocationUpdated += OnLocationUpdated;
                _geofenceEngine.POITriggered += OnPOITriggered;
                _audioManager.AudioStarted += OnAudioStarted;
                _audioManager.AudioCompleted += OnAudioCompleted;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khởi tạo: {ex.Message}";
            }
        }

        public ObservableCollection<POI> NearbyPOIs
        {
            get => _nearbyPOIs;
            set { _nearbyPOIs = value; OnPropertyChanged(); }
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

        public ICommand StartLocationServiceCommand { get; }
        public ICommand StopLocationServiceCommand { get; }

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

            // Check nearby POIs
            _ = _geofenceEngine.CheckPOIs(location);
        }

        private void OnPOITriggered(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Phát hành: {poi.Name}";
                
                if (!NearbyPOIs.Contains(poi))
                    NearbyPOIs.Add(poi);
            });
        }

        private void OnAudioStarted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Đang phát: {poi.Name}";
            });
        }

        private void OnAudioCompleted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = "Hoàn tất phát âm thanh";
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
