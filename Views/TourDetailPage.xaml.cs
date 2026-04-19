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
        
        // Subscribe to IsTourStarted property changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TourDetailViewModel.IsTourStarted))
            {
                UpdateLocalizedStrings();
            }
        };
        
        // Set localized strings when page is loaded
        Loaded += (s, e) => UpdateLocalizedStrings();
    }

    private void UpdateLocalizedStrings()
    {
        // Title Label
        if (TourDetailTitleLabel != null)
         TourDetailTitleLabel.Text = _resourceManager.GetString("Tour_Detail_Title") ?? "Chi Ti?t L? Trình";

        // Start Tour Button Label
        if (StartTourButtonLabel != null)
        {
            var viewModel = BindingContext as TourDetailViewModel;
  var buttonText = (viewModel?.IsTourStarted ?? false) 
     ? _resourceManager.GetString("Tour_EndTour") ?? "K?t thúc hành trình"
     : _resourceManager.GetString("Tour_StartTour") ?? "B?t ??u Hành Trình";
            StartTourButtonLabel.Text = buttonText;
      }

        // QR Code Title Label
        if (QRCodeTitleLabel != null)
            QRCodeTitleLabel.Text = _resourceManager.GetString("Tour_QRCodeTitle") ?? "Mã QR L? Trình";

    // Force refresh converters by triggering binding update
        // This refreshes TourDescriptionConverter and POIDescriptionLocalizedConverter
     if (_viewModel?.Tour != null)
      {
            // Force converter re-evaluation by updating the binding
    var tour = _viewModel.Tour;
   _viewModel.Tour = null;
  _viewModel.Tour = tour;
        }

        // Force refresh POI list to update POIDescriptionLocalizedConverter
        if (_viewModel?.TourPois != null && _viewModel.TourPois.Count > 0)
        {
            var pois = new List<POI>(_viewModel.TourPois);
      _viewModel.TourPois = new List<POI>();
    _viewModel.TourPois = pois;
        }
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