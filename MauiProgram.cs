using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using VinhKhanhstreetfoods.Data;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;
using VinhKhanhstreetfoods.Views;

namespace VinhKhanhstreetfoods
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"CRASH: {e.ExceptionObject}");
            };
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddSingleton<LocationService>();

            builder.Services.AddSingleton<IPOIRepository, POIRepository>();

            builder.Services.AddSingleton<TextToSpeechService>();
            //builder.Services.AddSingleton<MapService>(new MapService("YOUR_GOOGLE_MAPS_API_KEY"));
            builder.Services.AddSingleton<AudioManager>(sp => new AudioManager(sp.GetRequiredService<TextToSpeechService>()));
            builder.Services.AddSingleton<GeofenceEngine>(sp => new GeofenceEngine(
                sp.GetRequiredService<IPOIRepository>(),
                sp.GetRequiredService<AudioManager>()
            ));
            builder.Services.AddSingleton<RestaurantService>(sp => new RestaurantService(
                (POIRepository)sp.GetRequiredService<IPOIRepository>()
            ));

            // Register ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton<POIDetailViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();

            // Register Views
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<MapPage>();
            builder.Services.AddSingleton<POIDetailPage>();
            builder.Services.AddSingleton<SettingsPage>();

            // Register Routes
            Routing.RegisterRoute("home", typeof(HomePage));
            Routing.RegisterRoute("map", typeof(MapPage));
            Routing.RegisterRoute("detail", typeof(POIDetailPage));
            Routing.RegisterRoute("settings", typeof(SettingsPage));

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
