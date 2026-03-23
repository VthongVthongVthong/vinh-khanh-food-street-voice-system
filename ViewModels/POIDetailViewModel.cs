using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class POIDetailViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly AudioManager _audioManager;
        private readonly MapService _mapService;

        private POI? _selectedPOI;
        private string _statusMessage = string.Empty;
        private bool _isPlaying;
        private bool _disposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        public POIDetailViewModel(AudioManager audioManager, MapService mapService)
        {
            _audioManager = audioManager;
            _mapService = mapService;

            _audioManager.AudioStarted += OnAudioStarted;
            _audioManager.AudioCompleted += OnAudioCompleted;

            PlayAudioCommand = new Command(async () => await PlayAudio());
            StopAudioCommand = new Command(StopAudio);
            OpenMapCommand = new Command(async () => await OpenMap());
            ShareCommand = new Command(async () => await SharePOI());
            GoBackCommand = new Command(async () => await GoBack());
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
        public ICommand StopAudioCommand { get; }
        public ICommand OpenMapCommand { get; }
        public ICommand ShareCommand { get; }
        public ICommand GoBackCommand { get; }

        private async Task PlayAudio()
        {
            if (SelectedPOI is null)
                return;

            _audioManager.AddToQueue(SelectedPOI);
            StatusMessage = "Phát âm thanh...";
            await Task.CompletedTask;
        }

        private void StopAudio()
        {
            _audioManager.StopCurrent();
            IsPlaying = false;
            StatusMessage = "Đã dừng";
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
                    StatusMessage = "Không tạo được link bản đồ";
                    return;
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    StatusMessage = "Link bản đồ không hợp lệ";
                    return;
                }

                await Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
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
                StatusMessage = $"Lỗi chia sẻ: {ex.Message}";
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
                StatusMessage = $"Lỗi: {ex.Message}";
            }
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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _audioManager.AudioStarted -= OnAudioStarted;
            _audioManager.AudioCompleted -= OnAudioCompleted;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
