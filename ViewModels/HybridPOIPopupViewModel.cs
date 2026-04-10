using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    /// <summary>
    /// Enhanced ViewModel for hybrid popup with horizontal POI list
    /// </summary>
    public class HybridPOIPopupViewModel : INotifyPropertyChanged
    {
    private readonly AudioManager _audioManager;
       private readonly HybridPopupService _popupService;

        private POI? _selectedPOI;
        private string _selectedLanguage = "vi";
        private bool _isPlayingAudio;
        private bool _isLoadingAudio;
        private ObservableCollection<string> _availableLanguages;
   private string? _bannerImageUrl;
     private string? _avatarImageUrl;
     private string _queueIndicator = "";
        private bool _showQueueIndicator;
 private double _scrollPosition;

    public event PropertyChangedEventHandler? PropertyChanged;

    public HybridPOIPopupViewModel(AudioManager audioManager, HybridPopupService popupService)
     {
           _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));
           _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));

        _availableLanguages = new ObservableCollection<string>
          {
       "vi", "en", "zh", "ja", "ko", "fr", "ru"
            };

       PlayCommand = new Command(async () => await PlayAudioAsync(), CanPlayAudio);
       CloseCommand = new Command(async () => await ClosePopupAsync());
 SelectPOICommand = new Command<POI>(async p => await SelectPOIAsync(p));

         _audioManager.AudioStarted += OnAudioStarted;
   _audioManager.AudioCompleted += OnAudioCompleted;

     _popupService.POISelectionChanged += OnPOISelectionChanged;
     _popupService.POIAddedToActive += OnPOIAddedToActive;
 _popupService.POIRemovedFromActive += OnPOIRemovedFromActive;
      _popupService.QueueUpdated += OnQueueUpdated;

  UpdateQueueIndicator();
   }

    #region Properties

   public ObservableCollection<POI> ActivePOIs => _popupService.ActivePOIs;

     public POI? SelectedPOI
 {
  get => _selectedPOI ?? _popupService.SelectedPOI;
  set
      {
     if (_selectedPOI == value)
 return;

  _selectedPOI = value;
 OnPropertyChanged();
     }
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

      public string QueueIndicator
       {
  get => _queueIndicator;
   set { _queueIndicator = value; OnPropertyChanged(); }
 }

    public bool ShowQueueIndicator
   {
    get => _showQueueIndicator;
  set { _showQueueIndicator = value; OnPropertyChanged(); }
 }

   public double ScrollPosition
      {
       get => _scrollPosition;
  set { _scrollPosition = value; OnPropertyChanged(); }
     }

    #endregion

    #region Commands

     public ICommand PlayCommand { get; }
        public ICommand CloseCommand { get; }
   public ICommand SelectPOICommand { get; }

      #endregion

        #region Methods

   /// <summary>
      /// Update selected POI and load its images
   /// </summary>
       public async Task SelectPOIAsync(POI? poi)
  {
           if (poi == null)
  return;

     try
  {
   _selectedPOI = poi;
       SelectedLanguage = "vi"; // Reset language on selection
     IsPlayingAudio = false;

    // Notify UI of selection change
     OnPropertyChanged(nameof(SelectedPOI));

   // Notify popup service
     await _popupService.SelectPOIAsync(poi);

      System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] Selected POI: {poi.Name}");
 }
  catch (Exception ex)
    {
System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] Error selecting POI: {ex.Message}");
  }
     }

   /// <summary>
   /// Image loading removed - images no longer displayed in popup
     /// </summary>
      private async Task LoadPOIImagesAsync(POI poi)
  {
  // Images removed from UI - skip loading
  await Task.CompletedTask;
        }

/// <summary>
        /// ? FIX: Extract image URLs from POI ImageUrls JSON
        /// Returns: [0] = avatar, [1] = banner
        /// </summary>
        private List<string> ExtractImageUrlsFromPOI(POI poi)
        {
            var result = new List<string>();

   try
    {
                // Try to deserialize ImageUrls as JSON array
           if (!string.IsNullOrWhiteSpace(poi.ImageUrls))
    {
    var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<string>>(poi.ImageUrls);
        if (deserialized != null)
  {
          result.AddRange(deserialized.Where(u => !string.IsNullOrWhiteSpace(u)));
 }
         }
       }
   catch (Exception ex)
            {
            System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] Failed to parse ImageUrls JSON: {ex.Message}");
 }

            // Try to use POI properties directly if available
  if (string.IsNullOrWhiteSpace(result.FirstOrDefault()) && !string.IsNullOrWhiteSpace(poi.AvatarImageUrl))
          result.Insert(0, poi.AvatarImageUrl);
  
      if (result.Count < 2 && !string.IsNullOrWhiteSpace(poi.BannerImageUrl))
             result.Add(poi.BannerImageUrl);

        return result;
 }

        /// <summary>
     /// ? FIX: Get default banner image URL
  /// </summary>
        private string GetDefaultBannerUrl()
  {
            // Return a placeholder image or app default
  return "https://via.placeholder.com/400x200?text=No+Image";
        }

        /// <summary>
        /// ? FIX: Get default avatar image URL
    /// </summary>
  private string GetDefaultAvatarUrl()
        {
 // Return a placeholder image or app default
            return "https://via.placeholder.com/70x70?text=POI";
        }

        /// <summary>
        /// Play audio for selected POI in selected language
        /// </summary>
     private async Task PlayAudioAsync()
       {
        if (SelectedPOI == null)
        {
        System.Diagnostics.Debug.WriteLine("[HybridPOIPopupViewModel] No POI selected for playback");
    return;
        }

    try
            {
    IsLoadingAudio = true;

       System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] Playing audio for {SelectedPOI.Name} in {SelectedLanguage}");

      // Set language and add to queue
   SelectedPOI.TtsLanguage = SelectedLanguage;
    _audioManager.AddToQueue(SelectedPOI);

       // Update play state
       if (_audioManager.IsPlaying)
          {
IsPlayingAudio = true;
        }
         }
 catch (Exception ex)
   {
System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] Error playing audio: {ex.Message}");
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
           return SelectedPOI != null && !IsLoadingAudio;
 }

        /// <summary>
        /// Update queue indicator text
       /// </summary>
   private void UpdateQueueIndicator()
   {
   QueueIndicator = _popupService.GetQueueIndicator();
     ShowQueueIndicator = !string.IsNullOrEmpty(QueueIndicator);
    }

        #endregion

        #region Event Handlers

     private void OnPOISelectionChanged(object? sender, (POI OldPOI, POI NewPOI) args)
        {
MainThread.BeginInvokeOnMainThread(() =>
 {
      _selectedPOI = args.NewPOI;
    OnPropertyChanged(nameof(SelectedPOI));
     });
    }

private void OnPOIAddedToActive(object? sender, POI poi)
        {
 MainThread.BeginInvokeOnMainThread(() =>
        {
 UpdateQueueIndicator();
      System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] POI added: {poi.Name}");
          });
        }

   private void OnPOIRemovedFromActive(object? sender, POI poi)
      {
   MainThread.BeginInvokeOnMainThread(() =>
          {
  UpdateQueueIndicator();
        System.Diagnostics.Debug.WriteLine($"[HybridPOIPopupViewModel] POI removed: {poi.Name}");
  });
        }

        private void OnQueueUpdated(object? sender, EventArgs e)
     {
    MainThread.BeginInvokeOnMainThread(() =>
        {
  UpdateQueueIndicator();
    });
  }

        private void OnAudioStarted(object sender, POI poi)
       {
   MainThread.BeginInvokeOnMainThread(() =>
         {
IsPlayingAudio = true;
        System.Diagnostics.Debug.WriteLine("[HybridPOIPopupViewModel] Audio playback started");
     });
     }

        private void OnAudioCompleted(object sender, POI poi)
    {
            MainThread.BeginInvokeOnMainThread(() =>
    {
      IsPlayingAudio = false;
           System.Diagnostics.Debug.WriteLine("[HybridPOIPopupViewModel] Audio playback completed");
         });
  }

        #endregion

   protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
       => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
