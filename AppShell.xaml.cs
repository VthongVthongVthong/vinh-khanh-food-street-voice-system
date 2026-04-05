using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods;

public partial class AppShell : Shell
{
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public AppShell()
    {
        // ✅ CRITICAL: InitializeComponent phải minimal, không block main thread
        InitializeComponent();
        Routing.RegisterRoute("detail", typeof(Views.POIDetailPage));
        Routing.RegisterRoute("camera", typeof(Views.CameraPage));

        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;

        _localizationService.PropertyChanged += OnLanguageChanged;
        
        // ✅ Defer tab title initialization - don't block UI thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplyLocalizedTabTitles();
 });
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
     // ✅ Check controls exist before setting
    if (HomeTab != null)
            HomeTab.Title = _resourceManager.GetString("Nav_Home");
  if (MapTab != null)
            MapTab.Title = _resourceManager.GetString("Nav_Map");
        if (SettingsTab != null)
            SettingsTab.Title = _resourceManager.GetString("Nav_Settings");
    }
}
