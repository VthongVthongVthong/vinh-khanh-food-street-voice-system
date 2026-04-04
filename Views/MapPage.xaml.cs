using System.Collections.ObjectModel;
using System.Text.Json;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class MapPage : ContentPage
{
    private ObservableCollection<POI>? _currentPoiCollection;
    private int? _pendingPoiId;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
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
        if (BindingContext is MapViewModel vm)
        {
            _ = vm.EnsurePOIsLoadedAsync();

            vm.PropertyChanged += ViewModel_PropertyChanged;
            SubscribeToPoiCollection(vm.AllPOIs);
            MapWebView.Navigating += MapWebView_Navigating;

            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(600), async () =>
            {
                if (vm.UserLatitude != 0 && vm.UserLongitude != 0)
                    UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);

                UpdateTrackingState(vm.IsTracking);
                await RenderPOIsAsync(vm.AllPOIs);
                await TryFocusPendingPoiAsync();
            });
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
        if (BindingContext is MapViewModel vm)
        {
            vm.PropertyChanged -= ViewModel_PropertyChanged;
        }
        UnsubscribePoiCollection();
        MapWebView.Navigating -= MapWebView_Navigating;
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.UserLatitude) || e.PropertyName == nameof(MapViewModel.UserLongitude))
        {
            if (BindingContext is MapViewModel vm)
                UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
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
            }
        }
    }

    private async void UpdateTrackingState(bool isTracking)
    {
        try
        {
            string js = $"setTrackingState({isTracking.ToString().ToLower()});";
            await MapWebView.EvaluateJavaScriptAsync(js);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating tracking state script: {ex.Message}");
        }
    }

    private async void UpdateMapLocation(double lat, double lng)
    {
        if (lat == 0 && lng == 0) return;

        try
        {
            string js = $"updateLocation({lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {lng.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
            await MapWebView.EvaluateJavaScriptAsync(js);
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
                desc = p.DescriptionText
            }) ?? Enumerable.Empty<object>();

            var json = JsonSerializer.Serialize(payload);
            var js = $"renderPOIs({json});";
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
            _ = RenderPOIsAsync(vm.AllPOIs);
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
            _pendingPoiId = null;
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
    private void OnToggleFullScreenClicked(object sender, EventArgs e)
    {
        _isFullScreen = !_isFullScreen;

        if (_isFullScreen)
        {
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
            ToggleFullScreenBtn.Text = "✖";
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
            ToggleFullScreenBtn.Text = "⛶";
            ToggleFullScreenBtn.Margin = new Thickness(10);
        }
    }
}
