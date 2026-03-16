using System.Collections.ObjectModel;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly POIRepository _poiRepository;
        private readonly MapService _mapService;
        private readonly LocationService _locationService;

        private ObservableCollection<POI> _allPOIs;
        private POI _selectedPOI;
        private double _userLatitude;
        private double _userLongitude;
        private string _statusMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MapViewModel(POIRepository poiRepository, MapService mapService, LocationService locationService)
        {
            _poiRepository = poiRepository;
            _mapService = mapService;
            _locationService = locationService;

            AllPOIs = new ObservableCollection<POI>();
            StatusMessage = "Tải bản đồ...";

            OpenMapCommand = new Command(async () => await OpenMap());
            RefreshCommand = new Command(async () => await RefreshPOIs());

            _ = LoadPOIs();
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

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand OpenMapCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadPOIs()
        {
            var pois = await _poiRepository.GetAllPOIsAsync();
            AllPOIs = new ObservableCollection<POI>(pois);
            StatusMessage = $"Đã tải {pois.Count} địa điểm";
        }

        private async Task RefreshPOIs()
        {
            StatusMessage = "Đang làm mới...";
            await LoadPOIs();
            StatusMessage = "Làm mới hoàn tất";
        }

        private async Task OpenMap()
        {
            if (SelectedPOI == null)
                return;

            try
            {
                var uri = Uri.EscapeDataString(_mapService.GetMapUrl(SelectedPOI.Latitude, SelectedPOI.Longitude));
                await Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
