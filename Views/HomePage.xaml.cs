using Microsoft.Maui.ApplicationModel;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class HomePage : ContentPage
{
    private bool _isDataLoaded;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;
        _localizationService.PropertyChanged += OnLanguageChanged;

        ApplyLocalizedText();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _localizationService.PropertyChanged -= OnLanguageChanged;
        _localizationService.PropertyChanged += OnLanguageChanged;
        ApplyLocalizedText();

        if (_isDataLoaded)
            return;

        if (BindingContext is HomeViewModel vm)
        {
            _isDataLoaded = true;
            _ = vm.EnsureInitialDataLoadedAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            MainThread.BeginInvokeOnMainThread(ApplyLocalizedText);
        }
    }

    private void ApplyLocalizedText()
    {
        Title = _resourceManager.GetString("Home_Title");
        HeaderTitleLabel.Text = _resourceManager.GetString("Home_Title");
        HeaderLocationLabel.Text = $"📍 {_resourceManager.GetString("Home_Location")}";
        SearchEntry.Placeholder = _resourceManager.GetString("Home_Search_Placeholder");
        StartLocationButton.Text = _resourceManager.GetString("Home_Button_StartLocation");
        StopLocationButton.Text = _resourceManager.GetString("Home_Button_StopLocation");
    }

    private async void OnQrButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[HomePage] QR button clicked - navigating to CameraPage");
        await Shell.Current.GoToAsync("camera");
    }
}
