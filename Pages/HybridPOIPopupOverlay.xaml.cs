using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.Views;
using VinhKhanhstreetfoods.ViewModels;
using System.Diagnostics;

namespace VinhKhanhstreetfoods.Pages
{
    /// <summary>
    /// ? Overlay view for hybrid popup with priority queue support
    /// Converted from ContentPage to ContentView for lightweight rendering
  /// No navigation stack overhead
    /// </summary>
    public partial class HybridPOIPopupOverlay : ContentView
    {
        private readonly HybridPopupService _popupService;
     private readonly AudioManager _audioManager;
        private HybridPOIPopup? _currentPopup;
        private HybridPOIPopupViewModel? _viewModel;
     
        // ? Animation reentry protection
        private bool _isShowingAnimation = false;
        private bool _isHidingAnimation = false;

public HybridPOIPopupOverlay(HybridPopupService popupService, AudioManager audioManager)
        {
 InitializeComponent();

       _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
_audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));

      // ? Hook into popup service events
   _popupService.PopupRequested += OnPopupRequested;
            _popupService.POISelectionChanged += OnPOISelectionChanged;
            _popupService.PopupClosed += OnPopupClosed;

         Debug.WriteLine("[HybridPOIPopupOverlay] ? Initialized as ContentView");
        }

        /// <summary>
        /// ? Handle popup show request
        /// </summary>
        private async void OnPopupRequested(object? sender, POI poi)
 {
            try
            {
              if (poi == null)
   return;

    // ? Prevent animation reentry
 if (_isShowingAnimation)
   {
        Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Show animation already in progress | Time: {DateTime.Now:HH:mm:ss.fff}");
           return;
  }

                _isShowingAnimation = true;
                Debug.WriteLine($"[HybridPOIPopupOverlay] ?? ShowPopup START for POI {poi.Id} ({poi.Name}) | Time: {DateTime.Now:HH:mm:ss.fff}");

  // ? Clear previous popup
     if (_currentPopup != null)
       {
   PopupContainer.Content = null;
         _currentPopup = null;
    _viewModel = null;
      Debug.WriteLine("[HybridPOIPopupOverlay] ??? Cleared previous popup");
                }

          // ? Create new popup
 _viewModel = new HybridPOIPopupViewModel(_audioManager, _popupService);
     await _viewModel.SelectPOIAsync(poi);
             _currentPopup = new HybridPOIPopup(_viewModel);

    // ? Add to visual tree
        PopupContainer.Content = _currentPopup;
           Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Popup attached to visual tree | Time: {DateTime.Now:HH:mm:ss.fff}");

 // ? Wait for layout before animation
  await Task.Delay(50);

   // ? Animate backdrop
            try
       {
    await BackdropOverlay.FadeTo(0.3, 300, Easing.CubicOut);
           Debug.WriteLine($"[HybridPOIPopupOverlay] ? Backdrop animation done | Time: {DateTime.Now:HH:mm:ss.fff}");
    }
       catch (Exception ex)
           {
       Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Backdrop animation error: {ex.Message}");
  }

  // ? Animate popup
          if (_currentPopup != null)
    {
              try
       {
            await _currentPopup.ShowWithAnimationAsync();
    Debug.WriteLine($"[HybridPOIPopupOverlay] ? Popup animation done | Time: {DateTime.Now:HH:mm:ss.fff}");
    }
        catch (Exception ex)
   {
       Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Popup animation error: {ex.Message}");
    }
           }

         Debug.WriteLine($"[HybridPOIPopupOverlay] ?? ShowPopup COMPLETE | Time: {DateTime.Now:HH:mm:ss.fff}");
     }
  catch (Exception ex)
       {
            Debug.WriteLine($"[HybridPOIPopupOverlay] ? Error showing popup: {ex.Message}\n{ex.StackTrace}");
}
          finally
  {
       _isShowingAnimation = false;
        }
        }

        /// <summary>
        /// ? Handle POI selection change
  /// </summary>
        private async void OnPOISelectionChanged(object? sender, (POI OldPOI, POI NewPOI) args)
   {
            try
  {
         if (_currentPopup == null)
   return;

     Debug.WriteLine($"[HybridPOIPopupOverlay] ?? POI selection changed from {args.OldPOI.Id} to {args.NewPOI.Id} | Time: {DateTime.Now:HH:mm:ss.fff}");
             await _currentPopup.SelectionChangedAnimationAsync();
     }
            catch (Exception ex)
       {
  Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Error on POI selection: {ex.Message}");
}
        }

 /// <summary>
  /// ? Handle popup close request
    /// </summary>
        private async void OnPopupClosed(object? sender, EventArgs e)
 {
   try
            {
          // ? Prevent animation reentry
            if (_isHidingAnimation)
 {
        Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Hide animation already in progress | Time: {DateTime.Now:HH:mm:ss.fff}");
        return;
       }

 _isHidingAnimation = true;
              Debug.WriteLine($"[HybridPOIPopupOverlay] ?? ClosePopup START | Time: {DateTime.Now:HH:mm:ss.fff}");

      // ? Animate popup out
         if (_currentPopup != null)
             {
try
          {
  await _currentPopup.HideWithAnimationAsync();
   Debug.WriteLine($"[HybridPOIPopupOverlay] ? Popup hide animation done | Time: {DateTime.Now:HH:mm:ss.fff}");
           }
    catch (Exception ex)
           {
         Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Popup hide animation error: {ex.Message}");
       }
                }

          // ? Animate backdrop out
    try
         {
        await BackdropOverlay.FadeTo(0, 200, Easing.CubicIn);
           Debug.WriteLine($"[HybridPOIPopupOverlay] ? Backdrop hide animation done | Time: {DateTime.Now:HH:mm:ss.fff}");
   }
          catch (Exception ex)
            {
          Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Backdrop hide animation error: {ex.Message}");
         }

           // ? Clear content
      PopupContainer.Content = null;
         _currentPopup = null;
          _viewModel = null;

      Debug.WriteLine($"[HybridPOIPopupOverlay] ?? ClosePopup COMPLETE | Time: {DateTime.Now:HH:mm:ss.fff}");
     }
         catch (Exception ex)
    {
        Debug.WriteLine($"[HybridPOIPopupOverlay] ? Error closing popup: {ex.Message}");
            }
    finally
            {
           _isHidingAnimation = false;
            }
        }

        /// <summary>
     /// ? Cleanup when overlay is destroyed
 /// </summary>
    protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
  if (!this.IsLoaded)
            {
    try
       {
   if (_popupService != null)
               {
             _popupService.PopupRequested -= OnPopupRequested;
     _popupService.POISelectionChanged -= OnPOISelectionChanged;
                  _popupService.PopupClosed -= OnPopupClosed;
           }
        Debug.WriteLine("[HybridPOIPopupOverlay] ? Cleanup complete");
    }
      catch (Exception ex)
     {
          Debug.WriteLine($"[HybridPOIPopupOverlay] ?? Error during cleanup: {ex.Message}");
        }
            }
        }
    }
}
