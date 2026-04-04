using System.Linq;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhstreetfoods.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhstreetfoods.Views;

public partial class HomePage : ContentPage
{
    private bool _isScannerVisible;
    private bool _isDataLoaded;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isDataLoaded)
            return;

        if (BindingContext is HomeViewModel vm)
        {
            _isDataLoaded = true;
            await vm.EnsureInitialDataLoadedAsync();
        }
    }

    private async void OnQrButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (!await EnsureCameraPermissionAsync())
            {
                return;
            }

            ToggleScannerOverlay(true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Lỗi quét QR: {ex.Message}", "OK");
        }
    }

    private async Task<bool> EnsureCameraPermissionAsync()
    {
        var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (cameraStatus != PermissionStatus.Granted)
        {
            cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (cameraStatus != PermissionStatus.Granted)
        {
            await DisplayAlert("Quyền camera", "Cần cấp quyền camera để quét mã QR.", "OK");
            return false;
        }

        return true;
    }

    private void ToggleScannerOverlay(bool isVisible)
    {
        _isScannerVisible = isVisible;

        if (QrScannerOverlay is not null)
        {
            QrScannerOverlay.IsVisible = isVisible;
        }

        if (QrCameraView is CameraBarcodeReaderView cameraView)
        {
            cameraView.IsDetecting = isVisible;
        }
    }

    private void OnCloseScannerClicked(object sender, EventArgs e)
    {
        ToggleScannerOverlay(false);
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!_isScannerVisible)
        {
            return;
        }

        var qrValue = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(qrValue))
        {
            return;
        }

        _isScannerVisible = false;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ToggleScannerOverlay(false);
            await DisplayAlert("QR Code Detected", $"Nội dung: {qrValue}", "OK");
            await HandleQrResult(qrValue);
        });
    }

    private async Task HandleQrResult(string qrValue)
    {
        if (qrValue.StartsWith("poi_", StringComparison.OrdinalIgnoreCase))
        {
            var poiIdStr = qrValue.Substring(4);

            if (int.TryParse(poiIdStr, out var poiId))
            {
                var viewModel = (HomeViewModel)BindingContext;
                var poi = viewModel.NearbyPOIs.FirstOrDefault(p => p.Id == poiId);

                if (poi != null)
                {
                    await DisplayAlert("POI Found", $"Tìm thấy: {poi.Name}", "OK");
                    viewModel.SelectedPOI = poi;
                }
                else
                {
                    await DisplayAlert("POI Not Found", "Không tìm thấy POI này", "OK");
                }   
            }
        }
        else
        {
            await DisplayAlert("Thông tin", $"QR Code: {qrValue}", "OK");
        }
    }
}
