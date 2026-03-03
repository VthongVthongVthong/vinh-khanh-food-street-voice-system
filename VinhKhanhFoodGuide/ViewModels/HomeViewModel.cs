using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VinhKhanhFoodGuide.Models;
using VinhKhanhFoodGuide.Services;
using VinhKhanhFoodGuide.Data;

namespace VinhKhanhFoodGuide.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class HomeViewModel : BaseViewModel
{
    private readonly ILocationService _locationService;
    private readonly IGeofenceEngine _geofenceEngine;
    private readonly IAudioManager _audioManager;
    private readonly IPoiRepository _repository;

    private string _statusMessage = "Ready";
    private LocationData _currentLocation;
    private POI _selectedPoi;
    private bool _isTracking = false;
    private string _nearestPoiName = "None";
    private double _nearestPoiDistance = 0;
    private bool _isAudioPlaying = false;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public LocationData CurrentLocation
    {
        get => _currentLocation;
        set => SetProperty(ref _currentLocation, value);
    }

    public POI SelectedPoi
    {
        get => _selectedPoi;
        set => SetProperty(ref _selectedPoi, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetProperty(ref _isTracking, value);
    }

    public string NearestPoiName
    {
        get => _nearestPoiName;
        set => SetProperty(ref _nearestPoiName, value);
    }

    public double NearestPoiDistance
    {
        get => _nearestPoiDistance;
        set => SetProperty(ref _nearestPoiDistance, value);
    }

    public bool IsAudioPlaying
    {
        get => _isAudioPlaying;
        set => SetProperty(ref _isAudioPlaying, value);
    }

    public ObservableCollection<POI> AllPois { get; } = new();

    public HomeViewModel(ILocationService locationService, IGeofenceEngine geofenceEngine, 
        IAudioManager audioManager, IPoiRepository repository)
    {
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _audioManager = audioManager;
        _repository = repository;

        _locationService.LocationChanged += (s, location) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentLocation = location;
                UpdateNearestPoi(location);
                _geofenceEngine.UpdateLocation(location);
            });
        };

        _geofenceEngine.GeofenceTriggered += async (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                SelectedPoi = await _repository.GetPoiByIdAsync(e.PoiId);
                StatusMessage = $"Arrived at {e.PoiName}! Distance: {e.Distance:F1}m";
                await PlayPoiAudioAsync(e.PoiId);
            });
        };
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _geofenceEngine.LoadPoisAsync();
            var pois = await _repository.GetAllPoisAsync();
            foreach (var poi in pois)
            {
                AllPois.Add(poi);
            }
            StatusMessage = "Initialized. Tap 'Start Tracking' to begin.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task StartTrackingAsync()
    {
        try
        {
            await _locationService.StartTrackingAsync();
            IsTracking = true;
            StatusMessage = "Tracking location...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public async Task StopTrackingAsync()
    {
        try
        {
            await _locationService.StopTrackingAsync();
            IsTracking = false;
            StatusMessage = "Tracking stopped";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void UpdateNearestPoi(LocationData location)
    {
        if (AllPois.Count == 0) return;

        var distances = AllPois.Select(poi => new
        {
            Poi = poi,
            Distance = GeofenceEngine.CalculateDistance(
                location.Latitude,
                location.Longitude,
                poi.Latitude,
                poi.Longitude
            )
        }).OrderBy(x => x.Distance).FirstOrDefault();

        if (distances != null)
        {
            NearestPoiName = distances.Poi.Name;
            NearestPoiDistance = distances.Distance;
        }
    }

    private async Task PlayPoiAudioAsync(int poiId)
    {
        try
        {
            var content = await _repository.GetPoiContentByLanguageAsync(poiId, "en");
            if (content != null)
            {
                IsAudioPlaying = true;
                _geofenceEngine.SetAudioPlayingState(true);

                if (!string.IsNullOrEmpty(content.AudioPath))
                {
                    await _audioManager.PlayAudioFileAsync(content.AudioPath);
                }
                else if (content.UseTextToSpeech)
                {
                    await _audioManager.PlayTextToSpeechAsync(content.TextContent, content.LanguageCode);
                }

                IsAudioPlaying = false;
                _geofenceEngine.SetAudioPlayingState(false);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Audio error: {ex.Message}";
            IsAudioPlaying = false;
            _geofenceEngine.SetAudioPlayingState(false);
        }
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        return GeofenceEngine.CalculateDistance(lat1, lon1, lat2, lon2);
    }
}
