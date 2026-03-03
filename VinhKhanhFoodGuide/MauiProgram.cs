using VinhKhanhFoodGuide.Services;
using VinhKhanhFoodGuide.Data;
using VinhKhanhFoodGuide.Pages;
using VinhKhanhFoodGuide.ViewModels;

namespace VinhKhanhFoodGuide;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
            });

        // Register Services
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IAudioManager, AudioManager>();
        builder.Services.AddSingleton<IPoiRepository, PoiRepository>();
        builder.Services.AddSingleton<IGeofenceEngine, GeofenceEngine>();

        // Register ViewModels
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<POIDetailViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Register Pages
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<POIDetailPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
