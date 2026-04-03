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
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // ✅ Load configuration - simplified and optimized
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

            // ✅ Register configuration service
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<ConfigurationService>();

            // ✅ CRITICAL: Register services as LAZY SINGLETONS
            // This prevents heavy initialization on app startup
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<GeofenceEngine>();
            builder.Services.AddSingleton<POIRepository>();
            builder.Services.AddSingleton<IPOIRepository>(sp => sp.GetRequiredService<POIRepository>());
            builder.Services.AddSingleton<ITourRepository, TourRepository>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<TextToSpeechService>();
            builder.Services.AddSingleton<ITranslationService, HybridTranslationService>();
            builder.Services.AddSingleton<AudioManager>();
            builder.Services.AddSingleton<MapService>();

            // HTTP client for API calls
            builder.Services.AddSingleton(new HttpClient());

            // Register ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<POIDetailViewModel>();
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>(sp =>
            {
                var settingsService = sp.GetRequiredService<SettingsService>();
                var translationService = sp.GetRequiredService<ITranslationService>();
                var poiRepository = sp.GetRequiredService<IPOIRepository>();
                return new SettingsViewModel(settingsService, translationService, poiRepository);
            });

            // Register Views
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<POIDetailPage>();
            builder.Services.AddSingleton<MapPage>();
            builder.Services.AddSingleton<SettingsPage>();

            // Register Shell
            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();

            // ✅ Add global exception handler
#if DEBUG
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Debug.WriteLine($"❌ [FATAL] Unhandled exception: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Debug.WriteLine($"❌ [FATAL] Unobserved task exception: {e.Exception}");
                e.SetObserved();
            };
#endif

            return app;
        }

        /// <summary>
        /// ✅ OPTIMIZED: Load configuration from app resources
        /// Falls back to in-memory defaults if file not found
        /// </summary>
        private static IConfiguration LoadConfigurationOptimized()
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                    .GetAwaiter()
                    .GetResult();

                var builder = new ConfigurationBuilder();
                builder.AddJsonStream(stream);

                Debug.WriteLine("✅ [MauiProgram] Loaded appsettings.json from app package");
                return builder.Build();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ [MauiProgram] Cannot load appsettings.json: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        private static IConfiguration CreateDefaultConfiguration()
        {
            Debug.WriteLine("ℹ️ [MauiProgram] Using default/fallback configuration (no real API keys)");

            var configBuilder = new ConfigurationBuilder();

            // Security-first fallback: never store real API keys in code.
            // Real keys must be loaded only from appsettings.json.
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Environment", "Development" },
                { "TrackAsiaApiKey", "" },
                { "GoogleMapsApiKey", "" },
                { "TranslationServiceKey", "" },
                { "DatabasePath", "VinhKhanhFoodGuide.db3" },
                { "Logging:LogLevel:Default", "Debug" }
            });

            return configBuilder.Build();
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
