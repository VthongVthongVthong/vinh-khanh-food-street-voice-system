using System.Linq;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhstreetfoods.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanhstreetfoods.Views;

public partial class CameraPage : ContentPage
{
    private CameraBarcodeReaderView? _cameraView;
    private CameraViewModel? _viewModel;

    public CameraPage()
    {
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[CameraPage] Constructor");
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        System.Diagnostics.Debug.WriteLine("[CameraPage] OnNavigatedTo");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[CameraPage] OnAppearing");

        if (_viewModel == null)
        {
            if (BindingContext is CameraViewModel vm)
            {
                _viewModel = vm;
            }
            else
            {
                _viewModel = MauiProgram.ServiceProvider?.GetService<CameraViewModel>();
                BindingContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("[CameraPage] ViewModel injected in OnAppearing");
            }
        }

        if (_viewModel != null)
        {
            // Unsubscribe first to avoid double registration
            _viewModel.QRScanned -= OnPOIQRScanned;
            _viewModel.GenericQRScanned -= OnGenericQRScanned;
            _viewModel.ScanningCancelled -= OnScanningCancelled;

            _viewModel.QRScanned += OnPOIQRScanned;
            _viewModel.GenericQRScanned += OnGenericQRScanned;
            _viewModel.ScanningCancelled += OnScanningCancelled;
            _viewModel.StartScanning();
        }

        await InitializeCameraAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("[CameraPage] OnDisappearing");

        if (_viewModel != null)
        {
            _viewModel.QRScanned -= OnPOIQRScanned;
            _viewModel.GenericQRScanned -= OnGenericQRScanned;
            _viewModel.ScanningCancelled -= OnScanningCancelled;
            _viewModel.StopScanning();
            _viewModel.Cleanup();
        }

        await StopCameraAsync();
    }

    private async Task InitializeCameraAsync()
    {
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("[CameraPage] Requesting camera permission...");
                permissionStatus = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (permissionStatus != PermissionStatus.Granted)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Lỗi", "Cần quyền camera để quét QR", "OK");
                    await Navigation.PopAsync();
                });
                return;
            }

            if (_cameraView != null)
            {
                await StopCameraAsync();
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _cameraView = new CameraBarcodeReaderView
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    CameraLocation = CameraLocation.Rear,
                    IsDetecting = true,
                    
                    Options = new BarcodeReaderOptions
                    {
                        Formats = BarcodeFormats.TwoDimensional,
                        AutoRotate = true,
                        Multiple = false,
                        TryHarder = false,
                        TryInverted = false
                    }
                };

                _cameraView.BarcodesDetected += OnBarcodesDetected;
                CameraContainer.Children.Add(_cameraView);

                System.Diagnostics.Debug.WriteLine("[CameraPage] Camera view added to container");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraPage] Error: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Lỗi Camera", ex.Message, "OK");
                await Navigation.PopAsync();
            });
        }
    }

    private async Task StopCameraAsync()
    {
        if (_cameraView == null)
            return;

        System.Diagnostics.Debug.WriteLine("[CameraPage] Stopping camera...");

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (_cameraView != null)
            {
                _cameraView.IsDetecting = false;
                _cameraView.BarcodesDetected -= OnBarcodesDetected;
                CameraContainer.Children.Clear();
                _cameraView = null;
                System.Diagnostics.Debug.WriteLine("[CameraPage] Camera cleared");
            }
        });

        // Give time for cleanup
        await Task.Delay(100);
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var qrValue = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(qrValue) || _viewModel == null)
            return;

        System.Diagnostics.Debug.WriteLine($"[CameraPage] QR Detected: {qrValue}");

        if (_cameraView != null)
        {
            _cameraView.IsDetecting = false;
        }

        await _viewModel.HandleQrDetectedAsync(qrValue);
    }

    private async void OnPOIQRScanned(object? sender, CameraViewModel.QRProcessResult result)
    {
        System.Diagnostics.Debug.WriteLine($"[CameraPage] POI QR Scanned: {result.POI?.Name}. AutoPlay: {result.AutoPlay}");

        try
        {
            if (result.POI != null)
            {
                var settingsService = MauiProgram.ServiceProvider?.GetService<VinhKhanhstreetfoods.Services.SettingsService>();
                var targetLanguage = !string.IsNullOrEmpty(result.Language) ? result.Language : (settingsService?.PreferredLanguage ?? "vi");
                result.POI.TtsLanguage = targetLanguage;

                if (!string.IsNullOrEmpty(result.Language))
                {
                    if (settingsService != null)
                    {
                        settingsService.PreferredLanguage = targetLanguage;
                    }
                    VinhKhanhstreetfoods.Services.LocalizationService.Instance.CurrentLanguage = targetLanguage;
                }

                var audioManager = MauiProgram.ServiceProvider?.GetService<VinhKhanhstreetfoods.Services.AudioManager>();
                
                if (audioManager != null && (audioManager.IsPlaying || audioManager.GetQueueItems().Any()))
                {
                    bool playNow = await DisplayAlert("Phát âm thanh", $"Bạn có muốn ưu tiên phát âm thanh của '{result.POI.Name}' ngay không? Hàng đợi đang phát sẽ được chuyển xuống sau.", "Có", "Không");
                    if (playNow)
                    {
                        audioManager.PlayNowAndPauseCurrent(result.POI);
                    }
                    else
                    {
                        // Giữ nguyên luồng xử lý bình thường nếu không chọn ưu tiên
                        if (result.AutoPlay)
                        {
                            audioManager.AddToQueue(result.POI);
                        }
                        else
                        {
                            var popupService = MauiProgram.ServiceProvider?.GetService<VinhKhanhstreetfoods.Services.HybridPopupService>();
                            if (popupService != null)
                            {
                                await popupService.HandleIncomingPOIAsync(result.POI, 0);
                            }
                        }
                    }
                }
                else
                {
                    if (result.AutoPlay)
                    {
                        System.Diagnostics.Debug.WriteLine("[CameraPage] AutoPlay is true. Adding to audio queue.");
                        if (audioManager != null)
                        {
                            audioManager.AddToQueue(result.POI);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[CameraPage] AudioManager not found in DI container.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[CameraPage] Displaying POI Popup.");
                        var popupService = MauiProgram.ServiceProvider?.GetService<VinhKhanhstreetfoods.Services.HybridPopupService>();
                        if (popupService != null)
                        {
                            await popupService.HandleIncomingPOIAsync(result.POI, 0);
                        }
                    }
                }
            }
            
            // Pop after processing to ensure background processing begins correctly
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraPage] Navigation error: {ex.Message}");
            await DisplayAlert("Lỗi", "Không thể mở chi tiết", "OK");
        }
    }

    private async void OnGenericQRScanned(object? sender, string qrValue)
    {
        System.Diagnostics.Debug.WriteLine($"[CameraPage] Generic QR Scanned: {qrValue}");

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var result = await DisplayAlert("Mã QR", $"Nội dung: {qrValue}", "Đóng", "Quét tiếp");

            if (result)
            {
                await Navigation.PopAsync();
            }
            else
            {
                // Re-enable detection
                if (_cameraView != null)
                {
                    _cameraView.IsDetecting = true;
                }
            }
        });
    }

    private async void OnScanningCancelled(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[CameraPage] Scanning cancelled by user");
        await Navigation.PopAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[CameraPage] Back button clicked");
        await Navigation.PopAsync();
    }
}
