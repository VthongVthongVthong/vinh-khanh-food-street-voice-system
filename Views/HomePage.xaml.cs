using System.Linq;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhstreetfoods.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhstreetfoods.Views;

public partial class HomePage : ContentPage
{
    private bool _isDataLoaded;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isDataLoaded)
            return;

        if (BindingContext is HomeViewModel vm)
        {
            _isDataLoaded = true;
            _ = vm.EnsureInitialDataLoadedAsync();
        }
    }

    private async void OnQrButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[HomePage] QR button clicked - navigating to CameraPage");
        await Shell.Current.GoToAsync("camera");
    }
}
