using System.Text;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class POIDetailPage : ContentPage
{
    private readonly POIDetailViewModel _viewModel;
    private readonly POIRepository _poiRepository;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;
    private int _poiId;

    public POIDetailPage(POIDetailViewModel viewModel, POIRepository poiRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _poiRepository = poiRepository;
        BindingContext = _viewModel;

        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;
        _localizationService.PropertyChanged += OnLanguageChanged;

        ApplyLocalizedText();
    }

    public int PoiId
    {
        get => _poiId;
        set
        {
            _poiId = value;
            _ = LoadPoiAsync(_poiId);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
        _localizationService.PropertyChanged += OnLanguageChanged;
        ApplyLocalizedText();
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
        Title = _resourceManager.GetString("POI_Title");
        BackBehavior.TextOverride = _resourceManager.GetString("Common_Back");

        ImagesSectionLabel.Text = _resourceManager.GetString("POI_Gallery");
        DetailsSectionLabel.Text = _resourceManager.GetString("POI_Description");
        AudioSectionLabel.Text = _resourceManager.GetString("Settings_Audio_Title");

        NarrationLanguageLabel.Text = _resourceManager.GetString("Settings_Language_Narration");
        NarrationLanguagePicker.Title = _resourceManager.GetString("Settings_Language_NarrationPickerTitle");

        PlayAudioButton.Text = _resourceManager.GetString("Settings_Audio_AutoPlay");
        StopAudioButton.Text = _resourceManager.GetString("Common_Close");
        OpenMapButton.Text = _resourceManager.GetString("POI_ViewOnMap");
        ShareButton.Text = _resourceManager.GetString("POI_Share");
    }

    private async Task LoadPoiAsync(int id)
    {
        var poi = await _poiRepository.GetPOIByIdAsync(id);
        _viewModel.SelectedPOI = poi;
    }

    private async void OnOpenMapPageClicked(object sender, EventArgs e)
    {
        var poi = _viewModel.SelectedPOI;
        if (poi == null) return;

        // Switch to map tab and pass poiId; MapPage will focus it.
        await Shell.Current.GoToAsync($"//map?poiId={poi.Id}");
    }
}
