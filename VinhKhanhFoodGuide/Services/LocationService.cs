using VinhKhanhFoodGuide.Models;

namespace VinhKhanhFoodGuide.Services;

public interface ILocationService
{
    Task<bool> RequestPermissionsAsync();
    Task StartTrackingAsync();
    Task StopTrackingAsync();
    event EventHandler<LocationData> LocationChanged;
    LocationData CurrentLocation { get; }
    bool IsTracking { get; }
}

public class LocationService : ILocationService
{
    private CancellationTokenSource _cancellationTokenSource;
    private LocationData _currentLocation;
    private const int FastUpdateIntervalMs = 2000;   // Fast when moving
    private const int SlowUpdateIntervalMs = 5000;   // Slow when stationary
    private const double SpeedThreshold = 1.0;       // m/s threshold

    public event EventHandler<LocationData> LocationChanged;
    public LocationData CurrentLocation => _currentLocation;
    public bool IsTracking { get; private set; }

    public LocationService()
    {
        _currentLocation = new LocationData
        {
            Latitude = 10.7769,  // Default: Vinh Khanh Food Street area
            Longitude = 106.6789,
            Speed = 0,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Permission request error: {ex.Message}");
            return false;
        }
    }

    public async Task StartTrackingAsync()
    {
        try
        {
            if (IsTracking) return;

            var hasPermission = await RequestPermissionsAsync();
            if (!hasPermission)
            {
                throw new PermissionException("Location permission not granted");
            }

            IsTracking = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Start background tracking task
            _ = TrackLocationAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Start tracking error: {ex.Message}");
            IsTracking = false;
            throw;
        }
    }

    public async Task StopTrackingAsync()
    {
        try
        {
            IsTracking = false;
            _cancellationTokenSource?.Cancel();
            await Task.Delay(100); // Brief wait for cleanup
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Stop tracking error: {ex.Message}");
        }
    }

    private async Task TrackLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsTracking)
            {
                try
                {
                    var location = await Geolocation.GetLocationAsync(
                        new GeolocationRequest
                        {
                            DesiredAccuracy = GeolocationAccuracy.Best,
                            Timeout = TimeSpan.FromSeconds(10)
                        },
                        cancellationToken
                    );

                    if (location != null)
                    {
                        var oldLocation = _currentLocation;
                        _currentLocation = new LocationData
                        {
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Speed = location.Speed ?? 0,
                            Timestamp = DateTime.UtcNow
                        };

                        LocationChanged?.Invoke(this, _currentLocation);

                        // Adjust update interval based on speed
                        int updateInterval = (_currentLocation.Speed > SpeedThreshold) 
                            ? FastUpdateIntervalMs 
                            : SlowUpdateIntervalMs;

                        await Task.Delay(updateInterval, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(SlowUpdateIntervalMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting location: {ex.Message}");
                    await Task.Delay(SlowUpdateIntervalMs, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tracking error: {ex.Message}");
        }
    }
}
