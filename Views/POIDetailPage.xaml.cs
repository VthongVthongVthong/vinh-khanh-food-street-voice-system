using System.Text;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(PoiId), "poiId")]
[QueryProperty(nameof(AutoPlay), "autoplay")]
[QueryProperty(nameof(Language), "lang")]
public partial class POIDetailPage : ContentPage
{
    private readonly POIDetailViewModel _viewModel;
    private readonly POIRepository _poiRepository;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;
    private int _poiId;
    private bool _autoPlay;
    private string _language;

    public POIDetailPage(POIDetailViewModel viewModel, POIRepository poiRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _poiRepository = poiRepository;
        BindingContext = _viewModel;

        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;
        _localizationService.PropertyChanged += OnLanguageChanged;
        // ? Subscribe to ResourceManager language changes
        _resourceManager.LanguageChanged += OnResourceManagerLanguageChanged;

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

    public bool AutoPlay
    {
        get => _autoPlay;
        set => _autoPlay = value;
    }

    public string Language
    {
        get => _language;
        set => _language = value;
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        // WORKAROUND FOR MAUI CAROUSELVIEW BUG:
        // Clear ItemsSource and Position before the page is popped to prevent IndexOutOfRangeException
        ImagesCarousel.Position = 0;
        ImagesCarousel.ItemsSource = null;
        
        // Also clear the selected POI to strictly avoid caching the previous state explicitly
        _viewModel.SelectedPOI = null;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        // Restore ItemsSource binding whenever navigating to this page
        ImagesCarousel.SetBinding(ItemsView.ItemsSourceProperty, "SelectedPOI.ImageUrlList");
        ImagesCarousel.Position = 0;
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
        // ? Unsubscribe from ResourceManager
        _resourceManager.LanguageChanged -= OnResourceManagerLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            MainThread.BeginInvokeOnMainThread(() => {
                ApplyLocalizedText();
                // Force refresh binding for localization properties
                _viewModel?.RefreshLocalizationStrings();
            });
        }
    }

    private void OnResourceManagerLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Refresh ALL localization strings when ResourceManager language changes
            ApplyLocalizedText();
            _viewModel?.RefreshLocalizationStrings();
        });
    }

    private void ApplyLocalizedText()
    {
        Title = _resourceManager.GetString("POI_Title");
   BackBehavior.TextOverride = _resourceManager.GetString("Common_Back");

        ImagesSectionLabel.Text = _resourceManager.GetString("POI_Images");
        
        AudioSectionLabel.Text = _resourceManager.GetString("POI_AudioSection");

        NarrationLanguageLabel.Text = _resourceManager.GetString("POI_NarrationLanguage");
        NarrationLanguagePicker.Title = _resourceManager.GetString("POI_SelectNarrationLanguage");
        
        ShareButton.Text = _resourceManager.GetString("POI_ShareButton");
  }

    private async Task LoadPoiAsync(int id)
    {
        var poi = await _poiRepository.GetPOIByIdAsync(id);
        _viewModel.SelectedPOI = poi;
        
        if (!string.IsNullOrEmpty(Language))
        {
            var matchLang = _viewModel.LanguageOptions.FirstOrDefault(l => l.CultureCode.Equals(Language, StringComparison.OrdinalIgnoreCase));
            if (matchLang != null)
            {
                _viewModel.SelectedNarrationLanguage = matchLang;
            }
        }
        
        if (AutoPlay)
        {
            // Delay slightly to ensure UI and bindings are ready
            await Task.Delay(500);
            if (_viewModel.PlayAudioCommand.CanExecute(null))
            {
                _viewModel.PlayAudioCommand.Execute(null);
            }
        }
    }

    private async void OnOpenMapPageTapped(object sender, EventArgs e)
    {
        var poi = _viewModel.SelectedPOI;
        if (poi == null) return;

        // Switch to map tab and pass poiId; MapPage will focus it.
        await Shell.Current.GoToAsync($"//map?poiId={poi.Id}");
    }

    private void OnScrollLeftClicked(object sender, EventArgs e)
    {
        if (ImagesCarousel.Position > 0)
        {
            ImagesCarousel.Position -= 1;
        }
    }

    private void OnScrollRightClicked(object sender, EventArgs e)
    {
        if (ImagesCarousel.ItemsSource is System.Collections.IList list && ImagesCarousel.Position < list.Count - 1)
        {
            ImagesCarousel.Position += 1;
        }
    }
}

