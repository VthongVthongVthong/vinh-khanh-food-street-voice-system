using VinhKhanhFoodGuide.ViewModels;

namespace VinhKhanhFoodGuide.Pages;

public partial class HomePage : ContentPage
{
    private HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync();
        }
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.StartTrackingAsync();
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.StopTrackingAsync();
        }
    }
}
