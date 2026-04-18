using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class TourListPage : ContentPage
{
    private readonly LocalizationResourceManager _resourceManager;

    public TourListPage()
    {
    InitializeComponent();
    
   // Resolve BindingContext from service provider
        var serviceProvider = MauiProgram.ServiceProvider ?? throw new InvalidOperationException("ServiceProvider not initialized");
        BindingContext = serviceProvider.GetRequiredService<ViewModels.TourListViewModel>();
        
        // Get resource manager for localization
        _resourceManager = LocalizationResourceManager.Instance;
     Resources["resourceManager"] = _resourceManager;
     
     // Set localized titles
     UpdateLocalizedStrings();
     
     // Subscribe to language changes
   _resourceManager.LanguageChanged += (s, e) => UpdateLocalizedStrings();
    }

 private void UpdateLocalizedStrings()
 {
     if (TourListTitleLabel != null)
 TourListTitleLabel.Text = _resourceManager.GetString("Tour_Title") ?? "Các L? Trình";
      if (TourListSubtitleLabel != null)
     TourListSubtitleLabel.Text = _resourceManager.GetString("Tour_Subtitle") ?? "Ch?n l? trình ?? khám phá";
 }

 protected override void OnAppearing()
{
        base.OnAppearing();
   
     // Load tours when page appears
 if (BindingContext is ViewModels.TourListViewModel viewModel)
  {
 System.Diagnostics.Debug.WriteLine("[TourListPage] ?? Page appearing, loading tours...");
  _ = viewModel.LoadToursAsync();
        }
    }
}
