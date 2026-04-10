using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views
{
    /// <summary>
    /// Hybrid POI Popup with horizontal list of nearby POIs
    /// Supports smooth animations and POI selection
    /// </summary>
    public partial class HybridPOIPopup : ContentView
    {
       private readonly HybridPOIPopupViewModel _viewModel;

        public HybridPOIPopup(HybridPOIPopupViewModel viewModel)
       {
   InitializeComponent();
         _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
  BindingContext = _viewModel;

  // Add swipe gesture for POI navigation
    SetupSwipeGestures();
    }

  /// <summary>
    /// Setup swipe left/right gestures to navigate POI list
  /// </summary>
    private void SetupSwipeGestures()
 {
try
  {
 // Swipe left to next POI
 var swipeLeftGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
     swipeLeftGesture.Swiped += (s, e) => NavigateToNextPOI();
this.GestureRecognizers.Add(swipeLeftGesture);

  // Swipe right to previous POI
var swipeRightGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
   swipeRightGesture.Swiped += (s, e) => NavigateToPreviousPOI();
this.GestureRecognizers.Add(swipeRightGesture);

    System.Diagnostics.Debug.WriteLine("[HybridPOIPopup] Swipe gestures configured");
   }
       catch (Exception ex)
           {
  System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Error setting up gestures: {ex.Message}");
           }
 }

   /// <summary>
        /// Navigate to next POI in active list
        /// </summary>
   private void NavigateToNextPOI()
        {
try
            {
var activePOIs = _viewModel.ActivePOIs;
  if (activePOIs.Count == 0)
          return;

     var currentIndex = activePOIs.IndexOf(_viewModel.SelectedPOI);
var nextIndex = (currentIndex + 1) % activePOIs.Count;

 _ = _viewModel.SelectPOIAsync(activePOIs[nextIndex]);
 System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Swiped to next POI (index {nextIndex})");
        }
     catch (Exception ex)
   {
System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Error navigating to next: {ex.Message}");
     }
        }

  /// <summary>
        /// Navigate to previous POI in active list
        /// </summary>
        private void NavigateToPreviousPOI()
       {
 try
            {
    var activePOIs = _viewModel.ActivePOIs;
         if (activePOIs.Count == 0)
        return;

            var currentIndex = activePOIs.IndexOf(_viewModel.SelectedPOI);
    var previousIndex = (currentIndex - 1 + activePOIs.Count) % activePOIs.Count;

 _ = _viewModel.SelectPOIAsync(activePOIs[previousIndex]);
  System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Swiped to previous POI (index {previousIndex})");
       }
  catch (Exception ex)
    {
 System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Error navigating to previous: {ex.Message}");
   }
        }

      /// <summary>
   /// Animate popup entrance with fade and slide effect
/// </summary>
        public async Task ShowWithAnimationAsync()
  {
 try
     {
   Opacity = 0;
         TranslationY = 50;

    await Task.WhenAll(
 this.FadeTo(1, 300, Easing.CubicOut),
  this.TranslateTo(0, 0, 300, Easing.CubicOut)
           );

System.Diagnostics.Debug.WriteLine("[HybridPOIPopup] Popup shown with animation");
     }
       catch (Exception ex)
 {
  System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Animation error: {ex.Message}");
           }
  }

   /// <summary>
 /// Animate popup exit with fade and slide effect
  /// </summary>
        public async Task HideWithAnimationAsync()
  {
try
 {
    await Task.WhenAll(
 this.FadeTo(0, 200, Easing.CubicIn),
      this.TranslateTo(0, 100, 200, Easing.CubicIn)
   );

System.Diagnostics.Debug.WriteLine("[HybridPOIPopup] Popup hidden with animation");
    }
      catch (Exception ex)
  {
    System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Animation error: {ex.Message}");
      }
   }

   /// <summary>
     /// Animate POI selection change (scale + fade pulse)
  /// </summary>
        public async Task SelectionChangedAnimationAsync()
     {
 try
   {
  // Subtle scale animation on selection change
       // Note: POICollectionView would be accessed via XAML reference if needed
      await this.ScaleTo(0.98, 100, Easing.CubicOut);
   await this.ScaleTo(1.0, 100, Easing.CubicIn);

  System.Diagnostics.Debug.WriteLine("[HybridPOIPopup] Selection changed animation played");
}
       catch (Exception ex)
   {
    System.Diagnostics.Debug.WriteLine($"[HybridPOIPopup] Animation error: {ex.Message}");
 }
        }
 }
}
