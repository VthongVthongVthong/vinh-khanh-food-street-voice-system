using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.Pages;
using System.Diagnostics;

namespace VinhKhanhstreetfoods;

public partial class AppShell : Shell
{
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;
    private readonly AudioManager _audioManager;
    private readonly HybridPopupService _hybridPopupService;
    private HybridPOIPopupOverlay? _hybridPopupOverlay;
    
    // ✅ Debounce protection
    private int _isProcessingPopupRequest;
    private DateTime _lastPopupRequestTime = DateTime.MinValue;
    private readonly TimeSpan _popupDebounceDelay = TimeSpan.FromMilliseconds(100); // Reduced debounce

    public AppShell()
    {
     // ✅ CRITICAL: Keep InitializeComponent minimal
        InitializeComponent();
        Routing.RegisterRoute("detail", typeof(Views.POIDetailPage));
        Routing.RegisterRoute("camera", typeof(Views.CameraPage));

        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;

        var serviceProvider = MauiProgram.ServiceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized");
        _audioManager = serviceProvider.GetRequiredService<AudioManager>();
  _hybridPopupService = serviceProvider.GetRequiredService<HybridPopupService>();

        _localizationService.PropertyChanged += OnLanguageChanged;
        
        // ✅ Defer overlay setup - don't block UI thread
    MainThread.BeginInvokeOnMainThread(() =>
 {
   InitializeHybridPopupOverlay();
  ApplyLocalizedTabTitles();
 });
  }

 /// <summary>
    /// ✅ Initialize overlay popup system with minimal delay
    /// </summary>
   private void InitializeHybridPopupOverlay()
    {
try
    {
     // ✅ Get overlay from XAML
  _hybridPopupOverlay = this.FindByName<Views.HybridPOIPopupOverlay>("PopupOverlay");
    
 if (_hybridPopupOverlay == null)
   {
    Debug.WriteLine("[AppShell] ❌ PopupOverlay not found in XAML");
       return;
 }

 Debug.WriteLine("[AppShell] ✅ Overlay found in XAML");

  // ✅ Hook service events
 _hybridPopupService.PopupRequested += OnHybridPopupRequested;
   _hybridPopupService.PopupClosed += OnHybridPopupClosed;

Debug.WriteLine("[AppShell] ✅ Overlay events subscribed");
     }
    catch (Exception ex)
 {
    Debug.WriteLine($"[AppShell] ❌ Error initializing popup: {ex.Message}");
 }
}

 /// <summary>
        /// ✅ Monitor popup requests (the actual showing is handled by overlay itself)
     /// </summary>
     private void OnHybridPopupRequested(object? sender, Models.POI poi)
   {
        if (poi == null)
 return;

        var now = DateTime.Now;
        if (now - _lastPopupRequestTime < _popupDebounceDelay)
      {
  return;
         }

       if (Interlocked.CompareExchange(ref _isProcessingPopupRequest, 1, 0) != 0)
      {
     return;
 }

     _lastPopupRequestTime = now;

    try
      {
    MainThread.BeginInvokeOnMainThread(() =>
     {
    try
       {
Debug.WriteLine($"[AppShell] 📍 Popup for POI {poi.Id}");
  }
  catch (Exception ex)
    {
        Debug.WriteLine($"[AppShell] ❌ Error: {ex.Message}");
   }
 finally
   {
Interlocked.Exchange(ref _isProcessingPopupRequest, 0);
      }
        });
      }
  catch (Exception ex)
       {
   Debug.WriteLine($"[AppShell] ❌ Error: {ex.Message}");
       Interlocked.Exchange(ref _isProcessingPopupRequest, 0);
       }
      }

    private void OnHybridPopupClosed(object? sender, EventArgs e)
   {
     Debug.WriteLine($"[AppShell] 🔒 Popup closed");
      }

       private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
         if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
   {
    MainThread.BeginInvokeOnMainThread(ApplyLocalizedTabTitles);
 }
      }

    private void ApplyLocalizedTabTitles()
     {
        if (HomeTab != null)
 HomeTab.Title = _resourceManager.GetString("Nav_Home");
         if (MapTab != null)
   MapTab.Title = _resourceManager.GetString("Nav_Map");
       if (SettingsTab != null)
      SettingsTab.Title = _resourceManager.GetString("Nav_Settings");
     }

    protected override void OnDisappearing()
        {
 base.OnDisappearing();
   try
   {
   if (_hybridPopupService != null)
       {
      _hybridPopupService.PopupRequested -= OnHybridPopupRequested;
          _hybridPopupService.PopupClosed -= OnHybridPopupClosed;
     }
 _localizationService.PropertyChanged -= OnLanguageChanged;
    }
          catch (Exception ex)
     {
  Debug.WriteLine($"[AppShell] Error cleanup: {ex.Message}");
         }
    }
}
