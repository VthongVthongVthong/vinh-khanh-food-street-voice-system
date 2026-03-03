using VinhKhanhFoodGuide.ViewModels;

namespace VinhKhanhFoodGuide.Pages;

public partial class POIDetailPage : ContentPage
{
    private POIDetailViewModel _viewModel;

    public POIDetailPage(POIDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public async void SetPoiId(int poiId)
    {
        if (_viewModel != null)
        {
            await _viewModel.LoadPoiAsync(poiId);
            if (_viewModel.Poi != null)
            {
                PoiNameLabel.Text = _viewModel.Poi.Name;
                PoiCategoryLabel.Text = _viewModel.Poi.Category ?? "Unknown";
            }
            if (_viewModel.CurrentContent != null)
            {
                ContentLabel.Text = _viewModel.CurrentContent.TextContent;
            }
        }
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.PlayAudioAsync();
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.StopAudioAsync();
        }
    }
}
