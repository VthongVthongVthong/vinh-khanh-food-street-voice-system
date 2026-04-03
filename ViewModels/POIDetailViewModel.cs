using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class POIDetailViewModel : INotifyPropertyChanged, IDisposable
    {
        public sealed record LanguageOption(string CultureCode, string DisplayName)
        {
            public string DisplayLabel => DisplayName;
        }

        private readonly AudioManager _audioManager;
        private readonly MapService _mapService;
        private readonly SettingsService _settingsService;
        private readonly ITranslationService _translationService;

        private POI? _selectedPOI;
        private string _statusMessage = string.Empty;
        private bool _isPlaying;
        private bool _disposed;
        private LanguageOption? _selectedNarrationLanguage;
        private string _narrationPreviewText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public POIDetailViewModel(
            AudioManager audioManager,
            MapService mapService,
            SettingsService settingsService,
            ITranslationService translationService)
        {
            try
            {
                _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
                _mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
                _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
                _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));

                // ✅ Safe event subscription
                _audioManager.AudioStarted += OnAudioStarted;
                _audioManager.AudioCompleted += OnAudioCompleted;
                _settingsService.PreferredLanguageChanged += OnPreferredLanguageChanged;

                LanguageOptions = new List<LanguageOption>
                {
                    new("vi", "Tiếng Việt"),
                    new("en", "English"),
                    new("zh", "中文"),
                    new("ja", "日本語"),
                    new("ko", "한국어"),
                    new("fr", "Français"),
                    new("ru", "Русский")
                };

                SelectedNarrationLanguage = LanguageOptions.FirstOrDefault(x => x.CultureCode == _settingsService.PreferredLanguage)
                    ?? LanguageOptions[0];

                PlayAudioCommand = new Command(async () => await PlayAudio());
                StopAudioCommand = new Command(StopAudio);
                OpenMapCommand = new Command(async () => await OpenMap());
                ShareCommand = new Command(async () => await SharePOI());
                GoBackCommand = new Command(async () => await GoBack());

                System.Diagnostics.Debug.WriteLine("[POIDetailViewModel] Initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POIDetailViewModel] Constructor error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
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
                _ = RefreshNarrationPreviewAsync();
            }
        }

        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        public LanguageOption? SelectedNarrationLanguage
        {
            get => _selectedNarrationLanguage;
            set
            {
                if (Equals(_selectedNarrationLanguage, value))
                    return;

                _selectedNarrationLanguage = value;
                OnPropertyChanged();

                var code = value?.CultureCode ?? "vi";
                _settingsService.PreferredLanguage = code;
                _audioManager.StopCurrent();
                IsPlaying = false;
                _ = RefreshNarrationPreviewAsync();
                StatusMessage = $"Đã đổi ngôn ngữ: {code}";
            }
        }

        public string NarrationPreviewText
        {
            get => _narrationPreviewText;
            set
            {
                if (_narrationPreviewText == value)
                    return;

                _narrationPreviewText = value;
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

        private async Task RefreshNarrationPreviewAsync()
        {
            if (SelectedPOI is null)
            {
                NarrationPreviewText = string.Empty;
                return;
            }

            try
            {
                var language = SelectedNarrationLanguage?.CultureCode ?? _settingsService.PreferredLanguage;
                var text = await _translationService.ResolveNarrationTextAsync(SelectedPOI, language, preferTtsScript: true);
                NarrationPreviewText = string.IsNullOrWhiteSpace(text) ? (SelectedPOI.TtsScript ?? SelectedPOI.DescriptionText) : text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POIDetailViewModel] Refresh narration preview error: {ex.Message}");
                NarrationPreviewText = SelectedPOI.TtsScript ?? SelectedPOI.DescriptionText;
            }
        }

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

        private void OnPreferredLanguageChanged(object? sender, string language)
        {
            var selected = LanguageOptions.FirstOrDefault(x => x.CultureCode == language) ?? LanguageOptions[0];
            if (!Equals(SelectedNarrationLanguage, selected))
            {
                _selectedNarrationLanguage = selected;
                OnPropertyChanged(nameof(SelectedNarrationLanguage));
            }

            _ = RefreshNarrationPreviewAsync();
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
            _settingsService.PreferredLanguageChanged -= OnPreferredLanguageChanged;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
