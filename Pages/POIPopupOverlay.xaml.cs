using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.Views;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Pages
{
    /// <summary>
   /// Overlay page that hosts the POI popup.
    /// Manages popup lifecycle and animations.
   /// </summary>
    public partial class POIPopupOverlay : ContentPage
  {
        private readonly Services.PopupService _popupService;
        private readonly AudioManager _audioManager;
 private POIPopup? _currentPopup;
      private POIPopupViewModel? _viewModel;

      public POIPopupOverlay(Services.PopupService popupService, AudioManager audioManager)
    {
         InitializeComponent();

     _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
      _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));

       // Hook into popup service events
           _popupService.PopupRequested += OnPopupRequested;
    _popupService.PopupUpdated += OnPopupUpdated;
     _popupService.PopupClosed += OnPopupClosed;

      System.Diagnostics.Debug.WriteLine("[POIPopupOverlay] Initialized");
}

        /// <summary>
       /// Handle popup show request
        /// </summary>
    private async void OnPopupRequested(object? sender, POI poi)
   {
            try
      {
   if (poi == null)
  return;

  // Create ViewModel
 _viewModel = new POIPopupViewModel(_audioManager, _popupService);
          await _viewModel.UpdatePOIAsync(poi);

     // Create Popup view
       _currentPopup = new POIPopup(_viewModel);

     // Add to container
    PopupContainer.Content = _currentPopup;

           // Show backdrop with animation
    await BackdropOverlay.FadeTo(0.3, 300, Easing.CubicOut);

            // Show popup with animation
  await _currentPopup.ShowWithAnimationAsync();

      System.Diagnostics.Debug.WriteLine($"[POIPopupOverlay] Showing popup for POI {poi.Name}");
             }
    catch (Exception ex)
        {
System.Diagnostics.Debug.WriteLine($"[POIPopupOverlay] Error showing popup: {ex.Message}");
      }
   }

 /// <summary>
        /// Handle popup update request
        /// </summary>
       private async void OnPopupUpdated(object? sender, POI poi)
 {
    try
            {
     if (_viewModel == null || _currentPopup == null)
  {
      // Fallback to show new popup if view model doesn't exist
   OnPopupRequested(sender, poi);
            return;
  }

 // Update view model with new POI
   await _viewModel.UpdatePOIAsync(poi);

    // Animate update
   await _currentPopup.UpdateWithAnimationAsync();

  System.Diagnostics.Debug.WriteLine($"[POIPopupOverlay] Updated popup for POI {poi.Name}");
   }
          catch (Exception ex)
    {
  System.Diagnostics.Debug.WriteLine($"[POIPopupOverlay] Error updating popup: {ex.Message}");
        }
   }

   /// <summary>
   /// Handle popup close request
      /// </summary>
private async void OnPopupClosed(object? sender, EventArgs e)
 {
            try
         {
          if (_currentPopup != null)
      {
        // Hide with animation
           await _currentPopup.HideWithAnimationAsync();
  }

     // Hide backdrop
       await BackdropOverlay.FadeTo(0, 200, Easing.CubicIn);

         // Clear content
      PopupContainer.Content = null;
              _currentPopup = null;
   _viewModel = null;

   System.Diagnostics.Debug.WriteLine("[POIPopupOverlay] Popup closed");
    }
           catch (Exception ex)
    {
       System.Diagnostics.Debug.WriteLine($"[POIPopupOverlay] Error closing popup: {ex.Message}");
    }
}

   /// <summary>
        /// Handle backdrop tap to close popup
    /// </summary>
        private async void OnBackdropTapped(object sender, TappedEventArgs e)
   {
        await _popupService.ClosePopupAsync();
   }
  }
}
