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

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        System.Diagnostics.Debug.WriteLine("[CameraPage] OnNavigatedTo");

        if (_viewModel == null)
        {
            _viewModel = MauiProgram.ServiceProvider?.GetService<CameraViewModel>();
            if (_viewModel != null)
            {
      BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine("[CameraPage] ViewModel injected");
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[CameraPage] OnAppearing");

      if (_viewModel == null && BindingContext is CameraViewModel vm)
        {
            _viewModel = vm;
     }

        if (_viewModel != null)
        {
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
         await DisplayAlert("L?i", "C?n quy?n camera ?? quét QR", "OK");
          await Navigation.PopAsync();
          });
       System.Diagnostics.Debug.WriteLine("[CameraPage] Camera permission denied");
     return;
      }

            System.Diagnostics.Debug.WriteLine("[CameraPage] Creating camera view...");

    // Clear previous camera if exists
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
      IsDetecting = false,
         Options = new BarcodeReaderOptions
            {
          Formats = BarcodeFormats.TwoDimensional,
          AutoRotate = true,
      Multiple = false,
   TryHarder = true,
       TryInverted = true,
    }
     };

      _cameraView.BarcodesDetected += OnBarcodesDetected;
  CameraContainer.Children.Add(_cameraView);

  System.Diagnostics.Debug.WriteLine("[CameraPage] Camera view added to container");
       });

            var maxWait = 50;
  var waitCount = 0;

   while (_cameraView?.Handler == null && waitCount < maxWait)
     {
       await Task.Delay(100);
  waitCount++;
            }

     if (_cameraView?.Handler == null)
      {
  await MainThread.InvokeOnMainThreadAsync(async () =>
        {
    await DisplayAlert("L?i", "Camera handler không s?n sàng", "OK");
await Navigation.PopAsync();
         });
    System.Diagnostics.Debug.WriteLine("[CameraPage] Handler attach timeout");
          return;
}

   System.Diagnostics.Debug.WriteLine($"[CameraPage] Handler ready after {waitCount * 100}ms");

         await MainThread.InvokeOnMainThreadAsync(() =>
            {
        if (_cameraView?.Handler is not null)
      {
     _cameraView.IsDetecting = true;
    System.Diagnostics.Debug.WriteLine("[CameraPage] Camera detection enabled");
         }
    });
        }
        catch (Exception ex)
        {
     System.Diagnostics.Debug.WriteLine($"[CameraPage] Error: {ex.Message}");
     await MainThread.InvokeOnMainThreadAsync(async () =>
      {
         await DisplayAlert("L?i Camera", ex.Message, "OK");
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

    private async void OnPOIQRScanned(object? sender, Models.POI poi)
    {
        System.Diagnostics.Debug.WriteLine($"[CameraPage] POI QR Scanned: {poi.Name}");

        try
        {
         await Navigation.PopAsync();
           await Shell.Current.GoToAsync($"//home/detail?poiId={poi.Id}", true);
      }
    catch (Exception ex)
        {
         System.Diagnostics.Debug.WriteLine($"[CameraPage] Navigation error: {ex.Message}");
      await DisplayAlert("L?i", "Không th? m? chi ti?t", "OK");
     }
    }

    private async void OnGenericQRScanned(object? sender, string qrValue)
    {
    System.Diagnostics.Debug.WriteLine($"[CameraPage] Generic QR Scanned: {qrValue}");

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var result = await DisplayAlert("Mã QR", $"N?i dung: {qrValue}", "?óng", "Quét ti?p");

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
