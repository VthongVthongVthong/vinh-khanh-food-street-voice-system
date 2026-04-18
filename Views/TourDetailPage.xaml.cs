using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(TourId), "tourId")]
public partial class TourDetailPage : ContentPage
{
    private readonly TourDetailViewModel _viewModel;
    private int _tourId;

    public TourDetailPage(TourDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
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