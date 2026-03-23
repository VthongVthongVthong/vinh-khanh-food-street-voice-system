using System.Diagnostics;
using VinhKhanhstreetfoods.Services;
using Microsoft.Maui.Controls;

namespace VinhKhanhstreetfoods;

public partial class App : Application
{
    private bool _initializationComplete = false;

    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();

        // Initialize services asynchronously after UI is ready
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(500); // Give UI time to render
            await InitializeDatabaseAsync();
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        if (_initializationComplete)
            return;

        _initializationComplete = true;

        try
        {
            Debug.WriteLine("[App] 🔄 Starting database initialization on background thread...");
            
            // Get POI repository from DI container
            var repository = Application.Current?.Handler?.MauiContext?.Services
                .GetService<IPOIRepository>();
            
            if (repository != null)
            {
                // Run initialization on threadpool to avoid blocking UI
                await Task.Run(async () => 
                {
                    try
                    {
                        await repository.InitializeAsync();
                        Debug.WriteLine("✅ [App] Database initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ [App] Database init error: {ex.Message}");
                    }
                });
            }
            else
            {
                Debug.WriteLine("⚠️ [App] POIRepository not found in DI container");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [App] Initialization error: {ex.Message}");
        }
    }
}
