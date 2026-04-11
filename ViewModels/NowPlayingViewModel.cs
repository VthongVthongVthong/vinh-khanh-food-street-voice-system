using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels;

public class NowPlayingViewModel : INotifyPropertyChanged
{
    private readonly AudioManager _audioManager;
    private readonly SettingsService _settingsService;
    private readonly IPOIRepository _poiRepository;
    private readonly LocationService _locationService;

    private bool _isVisible;
    private string _poiName = string.Empty;
    private string _poiAvatarUrl = string.Empty;
    private string _languageText = string.Empty;
    private bool _isPlaying;
    private POI? _currentPoi;
    private bool _isPlaylistVisible;
    private ObservableCollection<POI> _upcomingAudios = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public NowPlayingViewModel(AudioManager audioManager, SettingsService settingsService, IPOIRepository poiRepository, LocationService locationService)
    {
        _audioManager = audioManager;
        _settingsService = settingsService;
        _poiRepository = poiRepository;
        _locationService = locationService;

        TogglePlayCommand = new Command(OnTogglePlay);
        OpenDetailCommand = new Command(OnTogglePlaylist);
        CloseCommand = new Command(OnClose);
        PlayAudioCommand = new Command<POI>(OnPlayAudio);

        _audioManager.AudioStarted += OnAudioStarted;
        _audioManager.AudioCompleted += OnAudioCompleted;
        _locationService.LocationUpdated += OnLocationUpdated;
    }

    private async void OnLocationUpdated(object? sender, Location location)
    {
        if (_isPlaylistVisible && _isVisible)
        {
            await LoadUpcomingAudiosAsync();
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    public string PoiName
    {
        get => _poiName;
        set { _poiName = value; OnPropertyChanged(); }
    }

    public string PoiAvatarUrl
    {
        get => _poiAvatarUrl;
        set { _poiAvatarUrl = value; OnPropertyChanged(); }
    }

    public string LanguageText
    {
        get => _languageText;
        set { _languageText = value; OnPropertyChanged(); }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayPauseIcon));
        }
    }

    public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";

    public bool IsPlaylistVisible
    {
        get => _isPlaylistVisible;
        set { _isPlaylistVisible = value; OnPropertyChanged(); }
    }

    public ObservableCollection<POI> UpcomingAudios
    {
        get => _upcomingAudios;
        set { _upcomingAudios = value; OnPropertyChanged(); }
    }

    public ICommand TogglePlayCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand PlayAudioCommand { get; }

    private void OnAudioStarted(object? sender, POI poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentPoi = poi;
            PoiName = poi.Name ?? "Tên quán";
            PoiAvatarUrl = poi.AvatarImageUrl ?? "";
            
            var langCode = _settingsService.PreferredLanguage.ToUpper();
            LanguageText = LocalizationResourceManager.Instance.GetString("Settings_Language_Narration") + $": {langCode}";

            IsPlaying = true;
            IsVisible = true;
        });
    }

    private void OnAudioCompleted(object? sender, POI poi)
    {
        // When Audio is completed (either stopped by user or finished line), we update the icon
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = false;
            
            // Auto-play the next item in the upcoming audios list like a playlist
            // Only automatically add to queue if location tracking is currently active
            if (_locationService.IsTracking && UpcomingAudios != null && UpcomingAudios.Count > 0)
            {
                var nextPoi = UpcomingAudios.First();
                UpcomingAudios.RemoveAt(0);

                // Delay slightly before starting the next audio
                Task.Delay(1000).ContinueWith(_ =>
                {
                    _audioManager.AddToQueue(nextPoi);
                });
            }
            else if (!_locationService.IsTracking || UpcomingAudios == null || UpcomingAudios.Count == 0)
            {
                _currentPoi = null;
                IsVisible = false;
            }
        });
    }

    private void OnTogglePlay()
    {
        if (IsPlaying)
        {
            _audioManager.StopCurrent();
            IsPlaying = false;
        }
        else if (_currentPoi != null)
        {
            IsPlaying = true;
            _audioManager.AddToQueue(_currentPoi);
        }
    }

    private async void OnTogglePlaylist()
    {
        IsPlaylistVisible = !IsPlaylistVisible;
        
        if (IsPlaylistVisible)
        {
            await LoadUpcomingAudiosAsync();
        }
    }

    private async Task LoadUpcomingAudiosAsync()
    {
        try
        {
            var location = await _locationService.GetCurrentLocation();
            if (location == null) return;

            var allPois = await _poiRepository.GetActivePOIsAsync();
            if (allPois == null) return;

            var nearbyPois = allPois.Select(poi => {
                var distance = Location.CalculateDistance(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude, DistanceUnits.Kilometers) * 1000;
                poi.DistanceFromUser = distance;
                return poi;
            })
            .Where(p => p.DistanceFromUser <= p.TriggerRadius * 2 || p.DistanceFromUser <= 100) 
            .OrderBy(p => p.DistanceFromUser)
            .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingItems = UpcomingAudios.Select(u => u.Id).ToList();
                var newItems = nearbyPois.Where(p => _currentPoi == null || p.Id != _currentPoi.Id).ToList();

                // Only update the visual list if it has completely changed to avoid flickering
                if (!existingItems.SequenceEqual(newItems.Select(n => n.Id)))
                {
                    UpcomingAudios.Clear();
                    foreach (var p in newItems)
                    {
                        UpcomingAudios.Add(p);
                        
                        // "để đưa vào hàng đợi âm thanh"
                        // GeofenceEngine already triggers close ones, 
                        // but if we want UpcomingAudios to truly act as a backup queue:
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NowPlayingViewModel] Playlist error: {ex.Message}");
        }
    }

    private void OnPlayAudio(POI poi)
    {
        if (poi == null) return;

        // Play the selected audio, clear previous queue
        _audioManager.StopCurrent();
        _audioManager.ClearQueue();
        _audioManager.AddToQueue(poi);
        
        // Hide playlist
        IsPlaylistVisible = false;
    }

    private void OnClose()
    {
        _audioManager.StopCurrent();
        IsVisible = false;
        IsPlaylistVisible = false;
        _currentPoi = null;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}