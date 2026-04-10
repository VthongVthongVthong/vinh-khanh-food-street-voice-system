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
        private string _statusMessage = "Chu?n b? qu�t...";
        private bool _isScanning;
     private bool _isProcessing;
 private POI? _scannedPOI;
     private string? _lastQrValue;
        private CancellationTokenSource? _scanningCts;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<QRProcessResult>? QRScanned; // Fired when valid POI QR is scanned
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
                StatusMessage = "?ang x? l�...";

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
                        System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Invoking QRScanned for: {result.POI.Name}");
                        ScannedPOI = result.POI;
                        StatusMessage = $"Đã tìm thấy: {result.POI.Name}";
                        QRScanned?.Invoke(this, result);
                    }
                    else if (!string.IsNullOrEmpty(result.QRValue))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CameraViewModel] Invoking GenericQRScanned for: {result.QRValue}");
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

            int poiId = -1;
            bool isPoiFound = false;
            bool autoPlay = false;
            string? lang = null;

            // Check if it's a POI QR code (format: poi_<id>)
            if (qrValue.StartsWith("poi_", StringComparison.OrdinalIgnoreCase))
            {
                var poiIdStr = qrValue.Substring(4);
                isPoiFound = int.TryParse(poiIdStr, out poiId);
            }
            else if (qrValue.StartsWith("vinhkhanh://poi", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(qrValue);
                    var queryStr = uri.Query.TrimStart('?');
                    var queryParams = queryStr.Split('&')
                        .Select(p => p.Split('='))
                        .Where(p => p.Length == 2)
                        .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

                    if (queryParams.TryGetValue("id", out var idStr))
                    {
                        isPoiFound = int.TryParse(idStr, out poiId);
                    }
                    if (queryParams.TryGetValue("action", out var actionStr) && actionStr == "play") 
                    {
                        autoPlay = true;
                    }
                    if (queryParams.TryGetValue("lang", out var langStr)) 
                    {
                        lang = langStr;
                    }
                }
                catch (Exception ex)
                { 
                    System.Diagnostics.Debug.WriteLine($"Failed to parse URI: {ex.Message}");
                }
            }

            if (isPoiFound)
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
                            QRValue = qrValue,
                            AutoPlay = autoPlay,
                            Language = lang
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
                            ErrorMessage = $"POI #{poiId} kh�ng t?n t?i"
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
            StatusMessage = "Qu�t m� QR...";
  System.Diagnostics.Debug.WriteLine("[CameraViewModel] Scanning started");
        }

      public void StopScanning()
        {
   IsScanning = false;
         StatusMessage = "?� d?ng qu�t";
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
        public class QRProcessResult
        {
            public bool IsPOIQR { get; set; }
            public POI? POI { get; set; }
            public string QRValue { get; set; } = string.Empty;
            public string? ErrorMessage { get; set; }
            public bool AutoPlay { get; set; }
            public string? Language { get; set; }
        }
    }
}


