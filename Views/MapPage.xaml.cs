using System.Collections.ObjectModel;
using System.Text.Json;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class MapPage : ContentPage
{
private ObservableCollection<POI>? _currentPoiCollection;
  private int? _pendingPoiId;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public MapPage(MapViewModel viewModel)
  {
        InitializeComponent();
        BindingContext = viewModel;

        _localizationService = LocalizationService.Instance;
  _resourceManager = LocalizationResourceManager.Instance;
        _localizationService.PropertyChanged += OnLanguageChanged;

        ApplyLocalizedText();
    }

    public int PoiId
    {
      set
        {
            _pendingPoiId = value;
            _ = TryFocusPendingPoiAsync();
  }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        _localizationService.PropertyChanged -= OnLanguageChanged;
        _localizationService.PropertyChanged += OnLanguageChanged;

        ApplyLocalizedText();
        _ = ApplyMapLocalizedJsAsync();

        if (BindingContext is MapViewModel vm)
        {
            _ = vm.EnsurePOIsLoadedAsync();

     vm.PropertyChanged += ViewModel_PropertyChanged;
            SubscribeToPoiCollection(vm.AllPOIs);
            MapWebView.Navigating += MapWebView_Navigating;
            MapWebView.Navigated += MapWebView_Navigated;

            if (vm.UserLatitude != 0 && vm.UserLongitude != 0)
            {
                UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
            }
            UpdateTrackingState(vm.IsTracking);
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
#if ANDROID
        if (MapWebView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
        {
       nativeWebView.Touch += (sender, e) =>
            {
             if (e.Event?.ActionMasked == Android.Views.MotionEventActions.Down ||
      e.Event?.ActionMasked == Android.Views.MotionEventActions.Move)
    {
       nativeWebView.Parent?.RequestDisallowInterceptTouchEvent(true);
          }
      else if (e.Event?.ActionMasked == Android.Views.MotionEventActions.Up ||
  e.Event?.ActionMasked == Android.Views.MotionEventActions.Cancel)
            {
              nativeWebView.Parent?.RequestDisallowInterceptTouchEvent(false);
      }
                e.Handled = false;
          };
        }
#endif
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        _pendingPoiId = null;
        if (BindingContext is MapViewModel vm)
        {
   vm.PropertyChanged -= ViewModel_PropertyChanged;
        }
     UnsubscribePoiCollection();
        MapWebView.Navigating -= MapWebView_Navigating;
        MapWebView.Navigated -= MapWebView_Navigated;
    }

    private async void MapWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        if (BindingContext is MapViewModel vm)
        {
            if (vm.UserLatitude != 0 && vm.UserLongitude != 0)
          {
              UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
 }

            UpdateTrackingState(vm.IsTracking);
            // ?? Render FilteredPOIs instead of AllPOIs
            await RenderPOIsAsync(vm.FilteredPOIs);
   await TryFocusPendingPoiAsync();
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
    MainThread.BeginInvokeOnMainThread(() =>
      {
           ApplyLocalizedText();
 _ = ApplyMapLocalizedJsAsync();
     // Refresh localization properties in ViewModel
    if (BindingContext is MapViewModel vm)
        vm.RefreshLocalizationStrings();
        });
        }
    }

    private void ApplyLocalizedText()
  {
        Title = _resourceManager.GetString("Map_Title");
        HeaderTitleLabel.Text = $"{_resourceManager.GetString("Map_Title")}";
HeaderSubtitleLabel.Text = _resourceManager.GetString("Home_Featured_Desc");

    CurrentLocationTitleLabel.Text = _resourceManager.GetString("Map_CurrentLocation");
   CurrentLocationSubtitleLabel.Text = _resourceManager.GetString("Home_Location");

      NearbyHeaderTitleLabel.Text = _resourceManager.GetString("Map_Restaurants");
   StatsHeaderLabel.Text = _resourceManager.GetString("Map_Restaurants");
   StatsExploredLabel.Text = _resourceManager.GetString("POI_ViewOnMap");
   StatsListenedLabel.Text = _resourceManager.GetString("Home_AudioBadge");

        // Update locations count label
        if (BindingContext is MapViewModel vm)
        {
  var locationText = _resourceManager.GetString("Map_Locations") ?? "??a ?i?m";
  LocationsCountLabel.Text = $"{vm.AllPOIs.Count} {locationText}";
    }
 }

    private async Task ApplyMapLocalizedJsAsync()
    {
        try
        {
     var detailText = EscapeJs(_resourceManager.GetString("POI_ViewOnMap"));
      var js = $"setDetailButtonText('{detailText}');";
            await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(js));
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"Error applying map locale JS: {ex.Message}");
        }
    }

    private static string EscapeJs(string value)
        => (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ").Replace("\r", " ");

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.UserLatitude) || e.PropertyName == nameof(MapViewModel.UserLongitude))
        {
            if (BindingContext is MapViewModel vm)
      {
    UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
    // ? FIX: Also apply radius filter when location updates
        ApplyRadiusFilter();
      }
     }
      else if (e.PropertyName == nameof(MapViewModel.IsTracking))
   {
     if (BindingContext is MapViewModel vm)
            UpdateTrackingState(vm.IsTracking);
 }
        else if (e.PropertyName == nameof(MapViewModel.AllPOIs))
        {
            if (BindingContext is MapViewModel vm)
            {
          SubscribeToPoiCollection(vm.AllPOIs);
         _ = RenderPOIsAsync(vm.AllPOIs);
          _ = RenderHeatmapAsync(vm.HotScores, vm.AllPOIs);
    // Update locations count
            var locationText = _resourceManager.GetString("Map_Locations") ?? "??a ?i?m";
      LocationsCountLabel.Text = $"{vm.AllPOIs.Count} {locationText}";
            }
    }
        // ?? Monitor FilteredPOIs changes
        else if (e.PropertyName == nameof(MapViewModel.FilteredPOIs))
 {
  if (BindingContext is MapViewModel vm)
     _ = RenderPOIsAsync(vm.FilteredPOIs);
        }
        // ?? Monitor RadiusFilterKm changes
    else if (e.PropertyName == nameof(MapViewModel.RadiusFilterKm))
    {
    if (BindingContext is MapViewModel vm)
    {
    // Radius changed, re-render filtered POIs on map
_ = RenderPOIsAsync(vm.FilteredPOIs);
 }
        }
     // ?? Monitor IsLocationEnabled changes
        else if (e.PropertyName == nameof(MapViewModel.IsLocationEnabled))
  {
     if (BindingContext is MapViewModel vm)
         {
       // Re-apply filter when location enabled/disabled
 ApplyRadiusFilter();
}
        }
  // ?? Monitor HasPOIsInRadius changes
        else if (e.PropertyName == nameof(MapViewModel.HasPOIsInRadius))
   {
      // UI will automatically update due to binding
        }
        else if (e.PropertyName == nameof(MapViewModel.HotScores) || e.PropertyName == nameof(MapViewModel.SelectedHour))
     {
    if (BindingContext is MapViewModel vm)
        _ = RenderHeatmapAsync(vm.HotScores, vm.AllPOIs);
}
    }

  // ?? Helper method to apply radius filter
    private void ApplyRadiusFilter()
    {
     try
{
     if (BindingContext is MapViewModel vm)
  {
  vm.ApplyRadiusFilter();
        }
}
catch (Exception ex)
{
  System.Diagnostics.Debug.WriteLine($"[MapPage] Error in ApplyRadiusFilter: {ex.Message}");
}
    }

    private async Task RenderHeatmapAsync(Dictionary<int, double> hotScores, IEnumerable<POI> pois)
    {
        if (hotScores == null || pois == null) return;
        
        try
        {
            var features = new List<object>();
            foreach(var poi in pois)
            {
                if (hotScores.TryGetValue(poi.Id, out var score) && score > 0)
                {
                    features.Add(new {
                        type = "Feature",
                        geometry = new {
                            type = "Point",
                            coordinates = new[] { poi.Longitude, poi.Latitude }
                        },
                        properties = new {
                            id = poi.Id,
                            hotScore = score
                        }
                    });
                }
            }

            var geojson = new {
                type = "FeatureCollection",
                features = features
            };

            var jsonString = JsonSerializer.Serialize(geojson);
            var js = $"updateHotspots({jsonString});";
            await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(js));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MapPage] Heatmap render error: {ex.Message}");
        }
    }

    private async void UpdateTrackingState(bool isTracking)
    {
        try
        {
            string js = $"setTrackingState({isTracking.ToString().ToLower()});";
            await MainThread.InvokeOnMainThreadAsync(async () => 
            {
                await MapWebView.EvaluateJavaScriptAsync(js);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating tracking state script: {ex.Message}");
        }
    }

    private async void UpdateMapLocation(double lat, double lng)
    {
        if (lat == 0 || lng == 0) return;

        try
        {
            string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            
            string js = $"updateLocation({latStr}, {lngStr});";
            
            await MainThread.InvokeOnMainThreadAsync(async () => 
            {
                await MapWebView.EvaluateJavaScriptAsync(js);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating map script: {ex.Message}");
        }
    }

    private async Task RenderPOIsAsync(IEnumerable<POI>? pois)
    {
        try
   {
            var payload = pois?.Select(p => new
            {
          id = p.Id,
                name = p.Name,
    lat = p.Latitude,
lng = p.Longitude,
     address = p.Address ?? string.Empty,
      desc = p.DescriptionText,
   imageUrl = p.AvatarImageUrl
        }) ?? Enumerable.Empty<object>();

    var json = JsonSerializer.Serialize(payload);
    bool shouldFitBounds = _pendingPoiId == null;
   var js = $"renderPOIs({json}, {shouldFitBounds.ToString().ToLower()});";
     await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(js));
    await TryFocusPendingPoiAsync();
        }
      catch (Exception ex)
      {
 System.Diagnostics.Debug.WriteLine($"Error rendering POIs: {ex.Message}");
        }
    }

  private void SubscribeToPoiCollection(ObservableCollection<POI>? collection)
    {
        if (_currentPoiCollection == collection)
    return;

    UnsubscribePoiCollection();
      _currentPoiCollection = collection;
        if (_currentPoiCollection != null)
_currentPoiCollection.CollectionChanged += PoiCollectionChanged;
    }

    private void UnsubscribePoiCollection()
    {
        if (_currentPoiCollection != null)
            _currentPoiCollection.CollectionChanged -= PoiCollectionChanged;
        _currentPoiCollection = null;
  }

    private void PoiCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
    if (BindingContext is MapViewModel vm)
   // ?? Render FilteredPOIs to respect current radius filter
 _ = RenderPOIsAsync(vm.FilteredPOIs);
  }

    private async Task TryFocusPendingPoiAsync()
    {
 if (_pendingPoiId is null)
   return;

        if (BindingContext is not MapViewModel vm)
          return;

        var targetId = _pendingPoiId.Value;
        var poi = vm.AllPOIs?.FirstOrDefault(p => p.Id == targetId);
        if (poi == null)
   return;

        try
        {
    string js = $"focusPOI({targetId});";
            await MainThread.InvokeOnMainThreadAsync(() => MapWebView.EvaluateJavaScriptAsync(js));
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"Error focusing POI: {ex.Message}");
      }
    }

    private async void MapWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url))
            return;

        if (e.Url.StartsWith("app://poi?", StringComparison.OrdinalIgnoreCase))
        {
    e.Cancel = true;
            var query = new Uri(e.Url).Query.TrimStart('?');
var idValue = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
           .Select(p => p.Split('='))
    .FirstOrDefault(p => p.Length == 2 && p[0] == "id")?[1];

            if (int.TryParse(idValue, out var poiId))
  {
                await Shell.Current.GoToAsync($"detail?poiId={poiId}");
        }
   }
    }

    private bool _isFullScreen = false;
    private async void OnToggleFullScreenClicked(object sender, EventArgs e)
    {
     _isFullScreen = !_isFullScreen;

    if (_isFullScreen)
        {
            await MainScrollView.ScrollToAsync(0, 0, false);
            
     Shell.SetTabBarIsVisible(this, false);
      NavigationPage.SetHasNavigationBar(this, false);

            HeaderFrame.IsVisible = false;
   LocationCardFrame.IsVisible = false;
   NearbyHeaderGrid.IsVisible = false;
 POIsCollectionView.IsVisible = false;
        StatsHeaderLabel.IsVisible = false;
StatsGrid.IsVisible = false;

 MainStackLayout.Padding = 0;
            MapFrame.CornerRadius = 0;

            MainScrollView.Orientation = ScrollOrientation.Neither;

 MapWebView.HeightRequest = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
      ToggleFullScreenBtn.Margin = new Thickness(10, 45, 10, 10);
  }
        else
   {
            Shell.SetTabBarIsVisible(this, true);

    HeaderFrame.IsVisible = true;
  LocationCardFrame.IsVisible = true;
         NearbyHeaderGrid.IsVisible = true;
 POIsCollectionView.IsVisible = true;
StatsHeaderLabel.IsVisible = true;
            StatsGrid.IsVisible = true;

  MainStackLayout.Padding = 15;
    MapFrame.CornerRadius = 15;

            MainScrollView.Orientation = ScrollOrientation.Vertical;

            MapWebView.HeightRequest = 350;
       ToggleFullScreenBtn.Margin = new Thickness(10);
      }
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        // Go back to the Home tab
        await Shell.Current.GoToAsync("//home");
    }
}
