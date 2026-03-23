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
    private int _poiId;

    public POIDetailPage(POIDetailViewModel viewModel, POIRepository poiRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _poiRepository = poiRepository;
        BindingContext = _viewModel;
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
