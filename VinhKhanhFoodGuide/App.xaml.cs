namespace VinhKhanhFoodGuide;

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
        InitializeDatabase();
    }

    private async void InitializeDatabase()
    {
        try
        {
            var repository = this.Handler.MauiContext?.Services?.GetService<IPoiRepository>();
            if (repository != null)
            {
                await repository.InitializeDatabaseAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database initialization error: {ex.Message}");
        }
    }
}
