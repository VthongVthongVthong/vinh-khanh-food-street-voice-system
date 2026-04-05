using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
 /// <summary>
  /// ViewModel for QR code scanning (CameraPage)
    /// - Handles QR detection and POI lookup on background thread
    /// - Manages camera state and error handling
    /// - Notifies when QR is successfully scanned
    /// </summary>
    public class CameraViewModel : INotifyPropertyChanged
    {
        private readonly IPOIRepository _poiRepository;
        private string _statusMessage = "Chu?n b? quét...";
        private bool _isScanning;
     private bool _isProcessing;
 private POI? _scannedPOI;
     private string? _lastQrValue;
        private CancellationTokenSource? _scanningCts;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<POI>? QRScanned; // Fired when valid POI QR is scanned
        public event EventHandler<string>? GenericQRScanned; // Fired for non-POI QR codes
        public event EventHandler? ScanningCancelled;

        public CameraViewModel(IPOIRepository poiRepository)
     {
       _poiRepository = poiRepository ?? throw new ArgumentNullException(nameof(poiRepository));
    _scanningCts = new CancellationTokenSource();

    CancelCommand = new Command(OnCancel);
        }

    public string StatusMessage
        {
            get => _statusMessage;
    set { _statusMessage = value; OnPropertyChanged(); }
        }

    public bool IsScanning
     {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

     public bool IsProcessing
        {
 get => _isProcessing;
        set { _isProcessing = value; OnPropertyChanged(); }
   }

        public POI? ScannedPOI
        {
            get => _scannedPOI;
  set { _scannedPOI = value; OnPropertyChanged(); }
 }

      public ICommand CancelCommand { get; }

        /// <summary>
        /// Called when QR code is detected by ZXing
        /// Processes on background thread to avoid main thread blocking
        /// </summary>
        public async Task HandleQrDetectedAsync(string qrValue)
   {
         if (string.IsNullOrWhiteSpace(qrValue) || IsProcessing)
           return;

        // Debounce: ignore if same QR detected again within 1 second
       if (_lastQrValue == qrValue && (DateTime.UtcNow - _lastDetectionTime).TotalSeconds < 1)
      return;

            _lastQrValue = qrValue;
     _lastDetectionTime = DateTime.UtcNow;

   try
      {
        IsProcessing = true;
                StatusMessage = "?ang x? lý...";

           // Process on background thread
     var result = await Task.Run(async () =>
   {
          try
         {
           return await ProcessQrValueAsync(qrValue, _scanningCts?.Token ?? CancellationToken.None);
             }
 catch (OperationCanceledException)
      {
  return null;
        }
          });

         if (result == null)
       {
        IsProcessing = false;
          return;
   }

     // Switch back to main thread for UI updates
        await MainThread.InvokeOnMainThreadAsync(() =>
{
                  if (result.IsPOIQR && result.POI != null)
      {
       ScannedPOI = result.POI;
    StatusMessage = $"? Tìm th?y: {result.POI.Name}";
        QRScanned?.Invoke(this, result.POI);
      }
   else if (!string.IsNullOrEmpty(result.QRValue))
             {
   StatusMessage = $"Mã QR: {result.QRValue}";
           GenericQRScanned?.Invoke(this, result.QRValue);
          }
   });
   }
    catch (Exception ex)
            {
      System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Error handling QR: {ex.Message}");
         await MainThread.InvokeOnMainThreadAsync(() =>
                {
      StatusMessage = $"L?i: {ex.Message}";
   });
    }
  finally
            {
       IsProcessing = false;
         }
        }

        /// <summary>
   /// Process QR value on background thread
        /// Returns result with POI if found, or generic QR value
        /// </summary>
        private async Task<QRProcessResult?> ProcessQrValueAsync(string qrValue, CancellationToken ct)
        {
         ct.ThrowIfCancellationRequested();

 System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Processing QR: {qrValue}");

       // Check if it's a POI QR code (format: poi_<id>)
 if (qrValue.StartsWith("poi_", StringComparison.OrdinalIgnoreCase))
            {
var poiIdStr = qrValue.Substring(4);
   if (int.TryParse(poiIdStr, out var poiId))
                {
        try
     {
            ct.ThrowIfCancellationRequested();
         var poi = await _poiRepository.GetPOIByIdAsync(poiId);

   if (poi != null)
          {
  System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Found POI: {poi.Name}");
            return new QRProcessResult
         {
 IsPOIQR = true,
             POI = poi,
  QRValue = qrValue
            };
  }
              else
     {
  System.Diagnostics.Debug.WriteLine($"[CameraViewModel] POI not found: {poiId}");
        return new QRProcessResult
      {
        IsPOIQR = true,
  POI = null,
                QRValue = qrValue,
     ErrorMessage = $"POI #{poiId} không t?n t?i"
  };
            }
    }
      catch (OperationCanceledException)
        {
             throw;
          }
       catch (Exception ex)
     {
     System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Error looking up POI: {ex.Message}");
      return new QRProcessResult
            {
   IsPOIQR = true,
     POI = null,
          QRValue = qrValue,
  ErrorMessage = $"L?i tra c?u: {ex.Message}"
                };
        }
    }
    }

            // Not a POI QR code, just return the raw value
            return new QRProcessResult
     {
     IsPOIQR = false,
    POI = null,
       QRValue = qrValue
   };
    }

        public void StartScanning()
        {
            IsScanning = true;
            StatusMessage = "Quét mã QR...";
  System.Diagnostics.Debug.WriteLine("[CameraViewModel] Scanning started");
        }

      public void StopScanning()
        {
   IsScanning = false;
         StatusMessage = "?ã d?ng quét";
System.Diagnostics.Debug.WriteLine("[CameraViewModel] Scanning stopped");
     }

        private void OnCancel()
        {
     System.Diagnostics.Debug.WriteLine("[CameraViewModel] Cancel clicked");
 _scanningCts?.Cancel();
         ScanningCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void Cleanup()
        {
            _scanningCts?.Cancel();
 _scanningCts?.Dispose();
            _scanningCts = null;
        }

  private DateTime _lastDetectionTime = DateTime.MinValue;

protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Internal result object for QR processing
        /// </summary>
        private class QRProcessResult
        {
            public bool IsPOIQR { get; set; }
      public POI? POI { get; set; }
            public string QRValue { get; set; } = string.Empty;
            public string? ErrorMessage { get; set; }
        }
    }
}
