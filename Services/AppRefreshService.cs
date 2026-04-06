using System.Diagnostics;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Global app refresh coordinator
    /// Data refresh only. UI language refresh is handled only at app startup
    /// and when user explicitly changes UI language.
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

        public event EventHandler<RefreshEventArgs>? RefreshStarted;
        public event EventHandler<RefreshEventArgs>? RefreshCompleted;

        private bool _isRefreshing;
        private CancellationTokenSource _refreshCts = new();

        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set => _isRefreshing = value;
        }

        private AppRefreshService() { }

        public async Task RefreshAppAsync(Func<CancellationToken, Task> dataRefreshCallback)
        {
            if (IsRefreshing)
                return;

            IsRefreshing = true;
            _refreshCts = new CancellationTokenSource();

            try
            {
                OnRefreshStarted(new RefreshEventArgs { IsRefreshing = true });
                Debug.WriteLine("[AppRefreshService] Starting app-wide data refresh");

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

                Debug.WriteLine("[AppRefreshService] App-wide data refresh completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppRefreshService] Error during refresh: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
                OnRefreshCompleted(new RefreshEventArgs { IsRefreshing = false });
            }
        }

        public void CancelRefresh()
        {
            try
            {
                _refreshCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
        }

        protected virtual void OnRefreshStarted(RefreshEventArgs e)
            => RefreshStarted?.Invoke(this, e);

        protected virtual void OnRefreshCompleted(RefreshEventArgs e)
            => RefreshCompleted?.Invoke(this, e);
    }

    public class RefreshEventArgs : EventArgs
    {
        public bool IsRefreshing { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
