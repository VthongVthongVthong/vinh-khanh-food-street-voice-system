using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class MapPage : ContentPage
{
	public MapPage(MapViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is MapViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
            
            // Xử lý một chút delay mượt mà để đợi WebView Map được tải xong trước khi điều hướng
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () => 
            {
                if (vm.UserLatitude != 0 && vm.UserLongitude != 0)
                {
                    UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
                }
                UpdateTrackingState(vm.IsTracking);
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
                    // Ngăn không cho ScrollView cha bắt sự kiện vuốt khi đang tương tác bản đồ
                    nativeWebView.Parent?.RequestDisallowInterceptTouchEvent(true);
                }
                else if (e.Event?.ActionMasked == Android.Views.MotionEventActions.Up || 
                         e.Event?.ActionMasked == Android.Views.MotionEventActions.Cancel)
                {
                    nativeWebView.Parent?.RequestDisallowInterceptTouchEvent(false);
                }
                // Vẫn để giá trị Handled = false để WebView nội bộ xử lý panning, zooming
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
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.UserLatitude) || e.PropertyName == nameof(MapViewModel.UserLongitude))
        {
            if (BindingContext is MapViewModel vm)
            {
                UpdateMapLocation(vm.UserLatitude, vm.UserLongitude);
            }
        }
        else if (e.PropertyName == nameof(MapViewModel.IsTracking))
        {
            if (BindingContext is MapViewModel vm)
            {
                UpdateTrackingState(vm.IsTracking);
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

    private bool _isFullScreen = false;

    private void OnToggleFullScreenClicked(object sender, EventArgs e)
    {
        _isFullScreen = !_isFullScreen;

        if (_isFullScreen)
        {
            // Vào chế độ toàn màn hình
            Shell.SetTabBarIsVisible(this, false);
            NavigationPage.SetHasNavigationBar(this, false); // Đảm bảo ẩn thanh điều hướng
            
            HeaderFrame.IsVisible = false;
            LocationCardFrame.IsVisible = false;
            NearbyHeaderGrid.IsVisible = false;
            POIsCollectionView.IsVisible = false;
            StatsHeaderLabel.IsVisible = false;
            StatsGrid.IsVisible = false;
            
            MainStackLayout.Padding = 0;
            MapFrame.CornerRadius = 0;
            
            // Khóa ScrollView lại để có thể vuốt/pan bản đồ mà không bị nội dung cuộn lên xuống
            MainScrollView.Orientation = ScrollOrientation.Neither;
            
            // Lấy chiều cao của màn hình, đặt height request cho WebView để tràn màn hình
            MapWebView.HeightRequest = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
            ToggleFullScreenBtn.Text = "✖"; // Nút thu nhỏ
            ToggleFullScreenBtn.Margin = new Thickness(10, 45, 10, 10); // Đẩy nút xuống một chút để tránh bị che bởi status bar
        }
        else
        {
            // Thoát chế độ toàn màn hình
            Shell.SetTabBarIsVisible(this, true);
            
            HeaderFrame.IsVisible = true;
            LocationCardFrame.IsVisible = true;
            NearbyHeaderGrid.IsVisible = true;
            POIsCollectionView.IsVisible = true;
            StatsHeaderLabel.IsVisible = true;
            StatsGrid.IsVisible = true;
            
            MainStackLayout.Padding = 15;
            MapFrame.CornerRadius = 15;
            
            // Mở lại Scroll chức năng
            MainScrollView.Orientation = ScrollOrientation.Vertical;
            
            MapWebView.HeightRequest = 350;
            ToggleFullScreenBtn.Text = "⛶"; // Nút phóng to
            ToggleFullScreenBtn.Margin = new Thickness(10); // Trả về vị trí cũ
        }
    }
}
