using CommunityToolkit.Maui;
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

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<GeofenceEngine>();
            builder.Services.AddSingleton<POIRepository>();
            builder.Services.AddSingleton<IPOIRepository>(sp => sp.GetRequiredService<POIRepository>());
            builder.Services.AddSingleton<AudioManager>();
            builder.Services.AddSingleton<TextToSpeechService>();
            builder.Services.AddSingleton<MapService>();

            // Register ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<POIDetailViewModel>();
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();

            // Register Views
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<POIDetailPage>();
            builder.Services.AddSingleton<MapPage>();
            builder.Services.AddSingleton<SettingsPage>();

            // Register Shell
            builder.Services.AddSingleton<AppShell>();

            var app = builder.Build();
            
            // Initialize database on app startup
            _ = CopySQLiteFileAsync();

            return app;
        }

        public static async Task CopySQLiteFileAsync()
        {
            try
            {
                var possiblePaths = new[]
                {
                    "Resources/Raw/poi_data.sqlite",
                    "Data/poi_data.sqlite",
                    "poi_data.sqlite"
                };

                var sourceFile = "";
                foreach (var path in possiblePaths)
                {
                    try
                    {
                        using (var stream = await FileSystem.OpenAppPackageFileAsync(path))
                        {
                            sourceFile = path;
                            break;
                        }
                    }
                    catch
                    {
                        // Try next path
                    }
                }

                if (string.IsNullOrEmpty(sourceFile))
                {
                    Debug.WriteLine("ERROR: Could not find poi_data.sqlite in any expected location");
                    return;
                }

                var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var targetPath = Path.Combine(folderPath, "VinhKhanhFoodGuide.db3");

                Debug.WriteLine($"[CopySQLiteFile] Found database at: {sourceFile}");
                Debug.WriteLine($"[CopySQLiteFile] Target path: {targetPath}");

                // ✅ Luôn xóa file cũ và copy lại (khi debug/dev)
#if DEBUG
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                    Debug.WriteLine($"[CopySQLiteFile] Deleted old database for fresh copy (DEBUG mode)");
                }
#endif

                if (!File.Exists(targetPath))
                {
                    using (var stream = await FileSystem.OpenAppPackageFileAsync(sourceFile))
                    using (var fileStream = File.Create(targetPath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                    Debug.WriteLine($"[CopySQLiteFile] Database copied successfully");
                }
                else
                {
                    Debug.WriteLine($"[CopySQLiteFile] Database already exists");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CopySQLiteFile] Error copying SQLite file: {ex.Message}");
                Debug.WriteLine($"[CopySQLiteFile] Stack trace: {ex.StackTrace}");
            }
        }
    }
}
