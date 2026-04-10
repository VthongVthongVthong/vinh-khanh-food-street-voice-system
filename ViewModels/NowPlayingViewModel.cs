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

    private bool _isVisible;
    private string _poiName = string.Empty;
    private string _poiAvatarUrl = string.Empty;
    private string _languageText = string.Empty;
    private bool _isPlaying;
    private POI? _currentPoi;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NowPlayingViewModel(AudioManager audioManager, SettingsService settingsService)
    {
        _audioManager = audioManager;
        _settingsService = settingsService;

        TogglePlayCommand = new Command(OnTogglePlay);
        OpenDetailCommand = new Command(OnOpenDetail);
        CloseCommand = new Command(OnClose);

        _audioManager.AudioStarted += OnAudioStarted;
        _audioManager.AudioCompleted += OnAudioCompleted;
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

    public ICommand TogglePlayCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand CloseCommand { get; }

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

    private async void OnOpenDetail()
    {
        if (_currentPoi != null)
        {
            await Shell.Current.GoToAsync($"detail?poiId={_currentPoi.Id}");
        }
    }

    private void OnClose()
    {
        _audioManager.StopCurrent();
        IsVisible = false;
        _currentPoi = null;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}