using System.Diagnostics;
using VinhKhanhstreetfoods.Services;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods;

public partial class App : Application
{
    private int _startupWarmupScheduled;

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
    /// Delay heavy localization warmup to avoid startup ANR on Android.
    /// </summary>
    protected override void OnStart()
    {
        base.OnStart();

        if (Interlocked.Exchange(ref _startupWarmupScheduled, 1) == 1)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Let first frame/UI navigation settle before warmup work.
                await Task.Delay(1200);

                var locService = LocalizationService.Instance;
                var resourceManager = LocalizationResourceManager.Instance;
                var preferredLang = locService.CurrentLanguage;

                // Stage 1: ensure preferred language only
                await resourceManager.PrefetchPreferredLanguageAsync(preferredLang);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    locService.NotifyLanguageRefreshed();
                });

                // Stage 2: warm remaining languages lazily
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(800);
                        await resourceManager.CacheAllLanguagesAsync();
                        Debug.WriteLine($"[App] Localization warm cache completed. Active={resourceManager.CurrentLanguage}, Cached={resourceManager.GetCachedLanguageCount()}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] Localization cache-all error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Localization preload error: {ex.Message}");
            }
        });
    }
}
