using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(TourId), "tourId")]
public partial class TourDetailPage : ContentPage
{
    private readonly TourDetailViewModel _viewModel;
    private readonly LocalizationResourceManager _resourceManager;
    private int _tourId;

    public TourDetailPage(TourDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
   
        // Get resource manager for localization
        _resourceManager = LocalizationResourceManager.Instance;
        Resources["resourceManager"] = _resourceManager;
  
        // Subscribe to language changes
        _resourceManager.LanguageChanged += (s, e) => UpdateLocalizedStrings();
        
        // Set localized strings when page is loaded
        Loaded += (s, e) => UpdateLocalizedStrings();
    }

    private void UpdateLocalizedStrings()
    {
        if (TourDetailTitleLabel != null)
         TourDetailTitleLabel.Text = _resourceManager.GetString("Tour_Detail_Title") ?? "Chi Ti?t L? Trình";
        if (StartTourButtonLabel != null)
         StartTourButtonLabel.Text = _resourceManager.GetString("Tour_StartTour") ?? "B?t ??u hành trình";
    }

    public int TourId
    {
        get => _tourId;
        set
        {
            _tourId = value;
            _viewModel.TourId = value;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load avatar images for all POIs in the tour
        await _viewModel.LoadTourPoiAvatarsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // ? Clear status message when leaving the page to prevent it from showing in background
        _viewModel.StatusMessage = string.Empty;
    }
}