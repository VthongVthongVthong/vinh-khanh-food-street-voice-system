using VinhKhanhstreetfoods.Data;
using VinhKhanhstreetfoods.Services;
using System.Diagnostics;

namespace VinhKhanhstreetfoods
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Debug.WriteLine("App started");

            _ = InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var poiRepository = IPlatformApplication.Current?.Services?.GetService<IPOIRepository>();
                if (poiRepository != null)
                {
                    // Với interface, không gọi InitializeAsync trực tiếp nữa.
                    // SeedData hiện vẫn cần SQLite implementation, nên chỉ seed nếu repo là POIRepository.
                    if (poiRepository is POIRepository sqliteRepo)
                    {
                        await sqliteRepo.InitializeAsync();
                        await SeedData.InitializeAsync(sqliteRepo);
                        Debug.WriteLine("Database initialized successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
