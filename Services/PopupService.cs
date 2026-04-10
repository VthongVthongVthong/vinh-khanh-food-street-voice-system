using System;
using System.Diagnostics;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Singleton service that manages a single POI popup instance.
    /// Ensures only one popup is shown at a time, with smooth transitions between POIs.
    /// </summary>
    public class PopupService
    {
        private static PopupService? _instance;
      private static readonly object _instanceLock = new();

        private POI? _currentPOI;
        private bool _isVisible;
  private CancellationTokenSource? _autoDismissCts;
        private readonly TimeSpan _autoDismissDelay = TimeSpan.FromSeconds(12);

    public event EventHandler<POI>? PopupRequested;
        public event EventHandler? PopupClosed;
      public event EventHandler<POI>? PopupUpdated;

        private PopupService()
{
            _currentPOI = null;
      _isVisible = false;
     }

    public static PopupService Instance
        {
    get
   {
              if (_instance == null)
          {
       lock (_instanceLock)
        {
             _instance ??= new PopupService();
        }
       }
         return _instance;
            }
     }

        /// <summary>
        /// Show or update popup with a new POI.
        /// If popup is already open, smoothly transitions to new POI.
        /// If popup is closed, shows new popup.
        /// </summary>
        public async Task ShowPOIPopupAsync(POI poi)
        {
            if (poi == null)
         {
       Debug.WriteLine("[PopupService] Attempted to show null POI");
       return;
          }

            try
    {
                // Cancel existing auto-dismiss timer
  CancelAutoDismiss();

      if (_isVisible && _currentPOI?.Id == poi.Id)
         {
      // Same POI already showing, just reset timer
          Debug.WriteLine($"[PopupService] POI {poi.Id} already showing, resetting timer");
      ScheduleAutoDismiss();
 return;
      }

    if (_isVisible && _currentPOI?.Id != poi.Id)
  {
// Different POI showing - update existing popup
         Debug.WriteLine($"[PopupService] Updating popup from POI {_currentPOI?.Id} to POI {poi.Id}");
    _currentPOI = poi;
      
          await MainThread.InvokeOnMainThreadAsync(() =>
  {
   PopupUpdated?.Invoke(this, poi);
               });
     }
       else
         {
          // No popup showing - create new one
 Debug.WriteLine($"[PopupService] Showing new popup for POI {poi.Id}");
        _currentPOI = poi;
  _isVisible = true;

          await MainThread.InvokeOnMainThreadAsync(() =>
{
        PopupRequested?.Invoke(this, poi);
  });
        }

      // Schedule auto-dismiss
            ScheduleAutoDismiss();
          }
   catch (Exception ex)
    {
        Debug.WriteLine($"[PopupService] Error showing popup: {ex.Message}");
            }
        }

   /// <summary>
        /// Close the currently visible popup
        /// </summary>
public async Task ClosePopupAsync()
     {
            if (!_isVisible)
                return;

 try
            {
 CancelAutoDismiss();
 _isVisible = false;
        _currentPOI = null;

        await MainThread.InvokeOnMainThreadAsync(() =>
                {
      PopupClosed?.Invoke(this, EventArgs.Empty);
    Debug.WriteLine("[PopupService] Popup closed");
          });
            }
     catch (Exception ex)
        {
                Debug.WriteLine($"[PopupService] Error closing popup: {ex.Message}");
    }
        }

        /// <summary>
  /// Check if popup is currently visible
     /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Get currently displayed POI (if any)
      /// </summary>
        public POI? CurrentPOI => _currentPOI;

        /// <summary>
        /// Schedule auto-dismiss after delay
        /// </summary>
        private void ScheduleAutoDismiss()
        {
        CancelAutoDismiss();

     _autoDismissCts = new CancellationTokenSource();
         _ = Task.Delay(_autoDismissDelay, _autoDismissCts.Token).ContinueWith(async t =>
   {
                if (!t.IsCanceled)
        {
           Debug.WriteLine("[PopupService] Auto-dismissing popup due to timeout");
           await ClosePopupAsync();
            }
    });
        }

/// <summary>
    /// Cancel pending auto-dismiss
/// </summary>
        private void CancelAutoDismiss()
        {
        _autoDismissCts?.Cancel();
            _autoDismissCts?.Dispose();
       _autoDismissCts = null;
  }
    }
}
