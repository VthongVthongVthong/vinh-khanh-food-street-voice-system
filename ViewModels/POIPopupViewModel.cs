using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    /// <summary>
    /// ViewModel for the POI popup.
    /// Manages popup state, language selection, and user interactions.
    /// </summary>
    public class POIPopupViewModel : INotifyPropertyChanged
    {
        private readonly AudioManager _audioManager;
  private readonly PopupService _popupService;

        private POI? _currentPOI;
  private string _selectedLanguage = "vi";
    private bool _isPlayingAudio;
        private bool _isLoadingAudio;
        private ObservableCollection<string> _availableLanguages;
        private string? _bannerImageUrl;
private string? _avatarImageUrl;

        public event PropertyChangedEventHandler? PropertyChanged;

     public POIPopupViewModel(AudioManager audioManager, PopupService popupService)
        {
       _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
 _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));

            _availableLanguages = new ObservableCollection<string>
            {
  "vi", "en", "zh", "ja", "ko", "fr", "ru"
            };

   PlayCommand = new Command(async () => await PlayAudioAsync(), CanPlayAudio);
      CloseCommand = new Command(async () => await ClosePopupAsync());

            _audioManager.AudioStarted += OnAudioStarted;
   _audioManager.AudioCompleted += OnAudioCompleted;
        }

        #region Properties

   public POI? CurrentPOI
        {
         get => _currentPOI;
        set { _currentPOI = value; OnPropertyChanged(); }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
      {
           if (_selectedLanguage == value)
            return;

      _selectedLanguage = value;
          OnPropertyChanged();
            }
 }

    public bool IsPlayingAudio
        {
  get => _isPlayingAudio;
            set { _isPlayingAudio = value; OnPropertyChanged(); }
    }

  public bool IsLoadingAudio
        {
          get => _isLoadingAudio;
            set { _isLoadingAudio = value; OnPropertyChanged(); }
        }

 public ObservableCollection<string> AvailableLanguages
        {
            get => _availableLanguages;
     set { _availableLanguages = value; OnPropertyChanged(); }
        }

        public string? BannerImageUrl
        {
  get => _bannerImageUrl;
            set { _bannerImageUrl = value; OnPropertyChanged(); }
        }

        public string? AvatarImageUrl
    {
          get => _avatarImageUrl;
            set { _avatarImageUrl = value; OnPropertyChanged(); }
        }

 #endregion

        #region Commands

        public ICommand PlayCommand { get; }
     public ICommand CloseCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Update popup with new POI data
        /// </summary>
   public async Task UpdatePOIAsync(POI poi)
        {
 if (poi == null)
       return;

            CurrentPOI = poi;
            SelectedLanguage = "vi"; // Reset to default language
    IsPlayingAudio = false;

    // Load images from POI
   await LoadPOIImagesAsync(poi);

            System.Diagnostics.Debug.WriteLine($"[POIPopupViewModel] Updated with POI: {poi.Name}");
        }

 /// <summary>
        /// Load banner and avatar images for POI
        /// </summary>
        private async Task LoadPOIImagesAsync(POI poi)
     {
            try
            {
        // Try to get images from ImageUrlList
          if (poi.ImageUrlList != null && poi.ImageUrlList.Count > 0)
  {
        BannerImageUrl = poi.ImageUrlList.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
 AvatarImageUrl = poi.ImageUrlList.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
                }

        // Fallback to first image in ImageUrls
                if (string.IsNullOrWhiteSpace(BannerImageUrl))
           {
           var images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(poi.ImageUrls ?? "[]") ?? new List<string>();
        BannerImageUrl = images.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
    AvatarImageUrl = BannerImageUrl;
  }
        }
        catch (Exception ex)
            {
       System.Diagnostics.Debug.WriteLine($"[POIPopupViewModel] Error loading images: {ex.Message}");
            }

        await Task.CompletedTask;
        }

        /// <summary>
        /// Play audio for current POI in selected language
      /// </summary>
   private async Task PlayAudioAsync()
        {
      if (CurrentPOI == null)
            {
   System.Diagnostics.Debug.WriteLine("[POIPopupViewModel] No POI selected for playback");
  return;
   }

  try
            {
     IsLoadingAudio = true;

            System.Diagnostics.Debug.WriteLine($"[POIPopupViewModel] Playing audio for {CurrentPOI.Name} in {SelectedLanguage}");

    // Add to audio queue with selected language context
 CurrentPOI.TtsLanguage = SelectedLanguage;
  _audioManager.AddToQueue(CurrentPOI);

            // Update play button state
           if (_audioManager.IsPlaying)
          {
      IsPlayingAudio = true;
      }
      }
     catch (Exception ex)
         {
     System.Diagnostics.Debug.WriteLine($"[POIPopupViewModel] Error playing audio: {ex.Message}");
     }
            finally
            {
IsLoadingAudio = false;
 }

          await Task.CompletedTask;
        }

   /// <summary>
        /// Close the popup
        /// </summary>
        private async Task ClosePopupAsync()
        {
   await _popupService.ClosePopupAsync();
        }

      /// <summary>
        /// Check if play button should be enabled
   /// </summary>
        private bool CanPlayAudio()
 {
       return CurrentPOI != null && !IsLoadingAudio;
  }

        /// <summary>
        /// Handle audio started event
      /// </summary>
        private void OnAudioStarted(object sender, POI poi)
        {
  MainThread.BeginInvokeOnMainThread(() =>
            {
 IsPlayingAudio = true;
    System.Diagnostics.Debug.WriteLine("[POIPopupViewModel] Audio playback started");
        });
        }

   /// <summary>
        /// Handle audio completed event
        /// </summary>
     private void OnAudioCompleted(object sender, POI poi)
  {
            MainThread.BeginInvokeOnMainThread(() =>
            {
   IsPlayingAudio = false;
     System.Diagnostics.Debug.WriteLine("[POIPopupViewModel] Audio playback completed");
 });
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
