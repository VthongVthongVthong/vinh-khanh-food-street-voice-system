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
            Debug.WriteLine($"[App] ❌ FATAL error in constructor: {ex.Message}");
            Debug.WriteLine($"[App] Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[App] Inner exception: {ex.InnerException.Message}");
                Debug.WriteLine($"[App] Inner stack: {ex.InnerException.StackTrace}");
            }

            // Re-throw to let system handle it
            throw;
        }
    }
}
