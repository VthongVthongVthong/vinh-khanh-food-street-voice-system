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
    private readonly TimeSpan _popupDebounceDelay = TimeSpan.FromMilliseconds(100);

    public AppShell()
    {
    try
        {
            // ✅ MUST call InitializeComponent FIRST
            InitializeComponent();
    
    Routing.RegisterRoute("detail", typeof(Views.POIDetailPage));
    Routing.RegisterRoute("camera", typeof(Views.CameraPage));

        _localizationService = LocalizationService.Instance;
      _resourceManager = LocalizationResourceManager.Instance;

  var serviceProvider = MauiProgram.ServiceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized");
  _audioManager = serviceProvider.GetRequiredService<AudioManager>();
          _hybridPopupService = serviceProvider.GetRequiredService<HybridPopupService>();

  _localizationService.PropertyChanged += OnLanguageChanged;
            
            // ✅ Defer overlay setup after UI thread settles
            MainThread.BeginInvokeOnMainThread(async () =>
    {
     try
 {
     await InitializeHybridPopupOverlayAsync();
     ApplyLocalizedTabTitles();
    }
           catch (Exception ex)
{
      Debug.WriteLine($"[AppShell] ❌ Error in deferred init: {ex.Message}");
 }
    });
        }
        catch (Exception ex)
   {
            Debug.WriteLine($"[AppShell] ❌ FATAL Constructor error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
/// ✅ Initialize overlay popup system
    /// </summary>
    private async Task InitializeHybridPopupOverlayAsync()
    {
        try
        {
            // ✅ Create overlay
          _hybridPopupOverlay = new HybridPOIPopupOverlay(_hybridPopupService, _audioManager);
            
   Debug.WriteLine("[AppShell] ✅ Overlay created and subscribed");

            // ✅ Hook service events
_hybridPopupService.PopupRequested += OnHybridPopupRequested;
          _hybridPopupService.PopupClosed += OnHybridPopupClosed;

 Debug.WriteLine("[AppShell] ✅ Overlay initialized");
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"[AppShell] ❌ Error initializing popup: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// ✅ Monitor popup requests
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
     Debug.WriteLine($"[AppShell] 📍 Popup requested for POI {poi.Id}");
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
        try
        {
          // ✅ Find XAML controls by name safely
       var homeTab = this.FindByName<ShellContent>("HomeTab");
   var mapTab = this.FindByName<ShellContent>("MapTab");
    var settingsTab = this.FindByName<ShellContent>("SettingsTab");

          if (homeTab != null)
        homeTab.Title = _resourceManager.GetString("Nav_Home");
 
      if (mapTab != null)
  mapTab.Title = _resourceManager.GetString("Nav_Map");
  
 if (settingsTab != null)
      settingsTab.Title = _resourceManager.GetString("Nav_Settings");
 }
   catch (Exception ex)
     {
   Debug.WriteLine($"[AppShell] ⚠️ Error applying tab titles: {ex.Message}");
        }
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
     Debug.WriteLine("[AppShell] ✅ Cleanup complete");
        }
        catch (Exception ex)
     {
         Debug.WriteLine($"[AppShell] ❌ Error cleanup: {ex.Message}");
        }
    }
}
