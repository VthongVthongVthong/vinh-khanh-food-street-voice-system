using VinhKhanhstreetfoods;
using VinhKhanhstreetfoods.Services;
using System.Diagnostics;

namespace VinhKhanhstreetfoods;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();
        
        try
        {
            // ✅ Copy SQLite file nếu chưa có
            await MauiProgram.CopySQLiteFileAsync();
            
            // ✅ Initialize database
            var poiRepository = IPlatformApplication.Current?.Services?.GetService<IPOIRepository>();
            if (poiRepository != null)
            {
                await poiRepository.InitializeAsync();
                Debug.WriteLine("SQLite database initialized successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Error during initialization: {ex.Message}");
        }
    }
}
