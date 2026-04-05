using System.Diagnostics;
using VinhKhanhstreetfoods.Services;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods;

public partial class App : Application
{
    public App()
    {
        try
        {
            Debug.WriteLine("[App] Initializing application...");
            InitializeComponent();
            MainPage = new AppShell();
            Debug.WriteLine("[App] Application initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] FATAL error in constructor: {ex.Message}");
            Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[App] Inner exception: {ex.InnerException.Message}");
                Debug.WriteLine($"[App] Inner stack: {ex.InnerException.StackTrace}");
            }

            throw;
        }
    }

    /// <summary>
    /// ✅ ALL 3 STAGES on Background Thread (ANR-Safe!)
    /// Called AFTER UI is shown
    /// </summary>
    protected override void OnStart()
    {
        base.OnStart();

        _ = Task.Run(async () =>
        {
            try
            {
                var locService = LocalizationService.Instance;
                var resourceManager = LocalizationResourceManager.Instance;
                var preferredLang = locService.CurrentLanguage;

                // Ensure preferred language is active first
                await resourceManager.PrefetchPreferredLanguageAsync(preferredLang);

                // Force UI to redraw localized labels even if language value didn't change
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    locService.NotifyLanguageRefreshed();
                });

                // Warm all others in cache without changing active language
                await resourceManager.CacheAllLanguagesAsync();

                Debug.WriteLine($"[App] Localization preload completed. Active={resourceManager.CurrentLanguage}, Cached={resourceManager.GetCachedLanguageCount()}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Localization preload error: {ex.Message}");
            }
        });
    }
}
