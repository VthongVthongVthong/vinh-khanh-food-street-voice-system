using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;
using VinhKhanhstreetfoods.Views;
using System.Diagnostics;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhstreetfoods
{
    public static class MauiProgram
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // ? Startup-safe configuration: no blocking package file reads on UI startup path
            var config = LoadConfigurationOptimized();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // ? Register configuration service
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<ConfigurationService>();

            // ? Reuse static singleton instances to avoid double initialization work
            builder.Services.AddSingleton(_ => LocalizationService.Instance);
            builder.Services.AddSingleton(_ => LocalizationResourceManager.Instance);
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<GeofenceEngine>();
            builder.Services.AddSingleton<Services.PopupService>(_ => Services.PopupService.Instance);
            builder.Services.AddSingleton<HybridPopupService>(_ => HybridPopupService.Instance);
            builder.Services.AddSingleton<POICacheService>(_ => POICacheService.Instance);
            builder.Services.AddSingleton<POIRepository>();
            builder.Services.AddSingleton<IPOIRepository>(sp => sp.GetRequiredService<POIRepository>());
            builder.Services.AddSingleton<ITourRepository, TourRepository>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<TextToSpeechService>();
            builder.Services.AddSingleton<ITranslationService, TranslationService>();
            builder.Services.AddSingleton(Plugin.Maui.Audio.AudioManager.Current);
            builder.Services.AddSingleton<VinhKhanhstreetfoods.Services.AudioManager>();
            builder.Services.AddSingleton<MapService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<PresenceTrackerService>();
            builder.Services.AddSingleton<POICacheService>();

            // HTTP client for API calls
            builder.Services.AddSingleton(new HttpClient());

            // Register ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<NowPlayingViewModel>(sp =>
            {
                var audioManager = sp.GetRequiredService<AudioManager>();
                var settingsService = sp.GetRequiredService<SettingsService>();
                var poiRepository = sp.GetRequiredService<IPOIRepository>();
                var locationService = sp.GetRequiredService<LocationService>();
                var localizationManager = sp.GetRequiredService<LocalizationResourceManager>();
                return new NowPlayingViewModel(audioManager, settingsService, poiRepository, locationService, localizationManager);
            });
            builder.Services.AddTransient<CameraViewModel>();
            builder.Services.AddSingleton<POIPopupViewModel>(sp =>
            {
                var audioManager = sp.GetRequiredService<AudioManager>();
                var popupService = sp.GetRequiredService<Services.PopupService>();
                return new POIPopupViewModel(audioManager, popupService);
            });
            builder.Services.AddSingleton<HybridPOIPopupViewModel>(sp =>
            {
                var audioManager = sp.GetRequiredService<AudioManager>();
                var hybridPopupService = sp.GetRequiredService<HybridPopupService>();
                return new HybridPOIPopupViewModel(audioManager, hybridPopupService);
            });
            builder.Services.AddSingleton<POIDetailViewModel>(sp =>
            {
                var audioManager = sp.GetRequiredService<AudioManager>();
                var mapService = sp.GetRequiredService<MapService>();
                var settingsService = sp.GetRequiredService<SettingsService>();
                var translationService = sp.GetRequiredService<ITranslationService>();
                var localizationManager = sp.GetRequiredService<LocalizationResourceManager>();
                var poiRepository = sp.GetRequiredService<IPOIRepository>();
                return new POIDetailViewModel(audioManager, mapService, settingsService, translationService, localizationManager, poiRepository);
            });
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>(sp =>
            {
                var settingsService = sp.GetRequiredService<SettingsService>();
                var translationService = sp.GetRequiredService<ITranslationService>();
                var poiRepository = sp.GetRequiredService<IPOIRepository>();
                return new SettingsViewModel(settingsService, translationService, poiRepository);
            });

            // Tour ViewModels
            builder.Services.AddSingleton<TourListViewModel>();
            builder.Services.AddSingleton<TourDetailViewModel>(sp =>
            {
                var tourRepository = sp.GetRequiredService<ITourRepository>();
                var poiRepository = sp.GetRequiredService<IPOIRepository>();
                var localizationManager = sp.GetRequiredService<LocalizationResourceManager>();
                var settingsService = sp.GetRequiredService<SettingsService>();
                var audioManager = sp.GetRequiredService<AudioManager>();
                return new TourDetailViewModel(tourRepository, poiRepository, localizationManager, settingsService, audioManager);
            });

            // Register Views
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddTransient<CameraPage>();
            builder.Services.AddSingleton<POIDetailPage>();
            builder.Services.AddSingleton<MapPage>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<Views.POIPopup>();
            // ? ONLY: New HybridPOIPopupOverlay (lightweight overlay view)
            builder.Services.AddSingleton<Views.HybridPOIPopup>();
            builder.Services.AddSingleton<Pages.HybridPOIPopupOverlay>();

            // Tour Pages
            builder.Services.AddSingleton<TourListPage>();
            builder.Services.AddSingleton<TourDetailPage>();

            // Register Shell
            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();
            ServiceProvider = app.Services;

            // Keep app bootstrap minimal: no startup warm-up work here.
            // DB/Firebase/services initialize lazily after UI is shown.

            // ? Add global exception handler
#if DEBUG
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Debug.WriteLine($"? [FATAL] Unhandled exception: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Debug.WriteLine($"? [FATAL] Unobserved task exception: {e.Exception}");
                e.SetObserved();
            };
#endif

            return app;
        }

        /// <summary>
        /// ? OPTIMIZED: Load configuration from app resources
        /// Falls back to in-memory defaults if file not found
        /// </summary>
        private static IConfiguration LoadConfigurationOptimized()
        {
            // ? ANR-safe: avoid synchronous FileSystem.OpenAppPackageFileAsync(...).GetResult() during app bootstrap.
            // Keep startup configuration lightweight and non-blocking.
            Debug.WriteLine("?? [MauiProgram] Using non-blocking startup configuration");

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Environment", "Development" },
                { "TrackAsiaApiKey", "" },
                { "GoogleMapsApiKey", "" },
                { "TranslationServiceKey", "" },
                { "DatabasePath", "VinhKhanhFoodGuide.db3" },
                { "Logging:LogLevel:Default", "Debug" }
            });

            return builder.Build();
        }

        private static IConfiguration CreateDefaultConfiguration()
        {
            // Keep for compatibility; routes to optimized startup-safe config.
            return LoadConfigurationOptimized();
        }

        /// <summary>
        /// Detect current environment (Development vs Production)
        /// </summary>
        private static string GetEnvironment()
        {
#if DEBUG
            return "Development";
#else
            return "Production";
#endif
        }
    }
}

