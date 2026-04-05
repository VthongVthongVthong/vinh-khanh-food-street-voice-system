using System.Diagnostics;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Global app refresh coordinator
    /// Syncs data refresh with UI language refresh across entire app
    /// </summary>
    public class AppRefreshService
    {
        private static AppRefreshService? _instance;
        private static readonly object _syncLock = new();

      public static AppRefreshService Instance
        {
            get
   {
   if (_instance == null)
           {
     lock (_syncLock)
    {
_instance ??= new AppRefreshService();
                 }
                }
     return _instance;
            }
    }

        // Event fired when app-wide refresh starts
        public event EventHandler<RefreshEventArgs>? RefreshStarted;

  // Event fired when app-wide refresh completes
    public event EventHandler<RefreshEventArgs>? RefreshCompleted;

        private bool _isRefreshing;
 private CancellationTokenSource _refreshCts = new();

        public bool IsRefreshing
 {
    get => _isRefreshing;
            private set => _isRefreshing = value;
        }

    private AppRefreshService() { }

        /// <summary>
        /// Trigger app-wide refresh (data + UI language update)
        /// This is called when user swipes down on any page
        /// </summary>
        public async Task RefreshAppAsync(Func<CancellationToken, Task> dataRefreshCallback)
        {
            if (IsRefreshing)
    return;

            IsRefreshing = true;
       _refreshCts = new CancellationTokenSource();

        try
       {
      // Signal to all pages that refresh is starting
  OnRefreshStarted(new RefreshEventArgs { IsRefreshing = true });

                Debug.WriteLine("[AppRefreshService] Starting app-wide refresh (data + language)");

   // Run data refresh on background thread
    await Task.Run(async () =>
                {
   try
         {
            await dataRefreshCallback(_refreshCts.Token);
  }
         catch (OperationCanceledException)
           {
                    Debug.WriteLine("[AppRefreshService] Refresh cancelled");
     }
                }, _refreshCts.Token);

       // Reload localization resources to update UI strings
     MainThread.BeginInvokeOnMainThread(() =>
     {
        var language = LocalizationService.Instance.CurrentLanguage;
     LocalizationResourceManager.Instance.LoadResourcesForLanguage(language);
  Debug.WriteLine($"[AppRefreshService] UI language reloaded: {language}");
                });

     Debug.WriteLine("[AppRefreshService] App-wide refresh completed");
            }
            catch (Exception ex)
          {
                Debug.WriteLine($"[AppRefreshService] Error during refresh: {ex.Message}");
            }
      finally
            {
                IsRefreshing = false;

          // Signal to all pages that refresh is complete
      OnRefreshCompleted(new RefreshEventArgs { IsRefreshing = false });
            }
        }

        /// <summary>
      /// Cancel ongoing refresh
        /// </summary>
        public void CancelRefresh()
        {
            try
  {
    _refreshCts?.Cancel();
            }
       catch (ObjectDisposedException)
   {
            // Already disposed, ignore
            }
        }

        protected virtual void OnRefreshStarted(RefreshEventArgs e)
        {
      RefreshStarted?.Invoke(this, e);
        }

        protected virtual void OnRefreshCompleted(RefreshEventArgs e)
        {
     RefreshCompleted?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Refresh event arguments
    /// </summary>
    public class RefreshEventArgs : EventArgs
    {
     public bool IsRefreshing { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
