using Microsoft.Maui.Devices.Sensors;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class LocationService
    {
        private bool _isCheckingLocation;
        private Location _lastLocation;
        private DateTime _lastUpdateTime;

        public bool IsTracking => _isCheckingLocation;

        public event EventHandler<Location> LocationUpdated;
        public event EventHandler<bool> TrackingStateChanged;

        public async Task<PermissionStatus> CheckAndRequestLocationPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
                return status;

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status;
        }

        public async Task StartListening(int intervalSeconds = 5)
        {
            try
            {
                if (_isCheckingLocation)
                    return;

                var hasPermission = await CheckAndRequestLocationPermission();
                if (hasPermission != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[LocationService] Location permission not granted");
                    return;
                }

                _isCheckingLocation = true;

                // Avoid duplicate subscription in edge cases
                Geolocation.LocationChanged -= Geolocation_LocationChanged;
                Geolocation.LocationChanged += Geolocation_LocationChanged;

                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    MinimumTime = TimeSpan.FromSeconds(intervalSeconds)
                };

                await Geolocation.StartListeningForegroundAsync(request);
                TrackingStateChanged?.Invoke(this, true);
                System.Diagnostics.Debug.WriteLine("[LocationService] Location listening started");
            }
            catch (FeatureNotSupportedException fex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationService] Geolocation not supported: {fex.Message}");
                _isCheckingLocation = false;
                TrackingStateChanged?.Invoke(this, false);
            }
            catch (FeatureNotEnabledException fex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationService] Geolocation not enabled: {fex.Message}");
                _isCheckingLocation = false;
                TrackingStateChanged?.Invoke(this, false);
            }
            catch (PermissionException pex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationService] Permission exception: {pex.Message}");
                _isCheckingLocation = false;
                TrackingStateChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationService] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                _isCheckingLocation = false;
                TrackingStateChanged?.Invoke(this, false);
            }
        }

        private DateTime _lastInvokeTime = DateTime.MinValue;

        private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            _lastLocation = e.Location;
            _lastUpdateTime = DateTime.Now;

            AdjustUpdateInterval(e.Location);

            // Throttle a bit more aggressively to reduce geofence+db+audio pressure
            if ((DateTime.Now - _lastInvokeTime).TotalSeconds >= 3)
            {
                _lastInvokeTime = DateTime.Now;
                LocationUpdated?.Invoke(this, e.Location);
            }
        }

        private void AdjustUpdateInterval(Location location)
        {
            if (location.Speed.HasValue)
            {
                double speedKmh = location.Speed.Value * 3.6;
                int newInterval = speedKmh < 5 ? 5 : speedKmh < 20 ? 3 : 2;
            }
        }

        public async Task StopListening()
        {
            try
            {
                if (!_isCheckingLocation)
                    return;

                try
                {
                    Geolocation.LocationChanged -= Geolocation_LocationChanged;
                    Geolocation.StopListeningForeground();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LocationService] Error stopping location listening: {ex.Message}");
                }

                _isCheckingLocation = false;
                TrackingStateChanged?.Invoke(this, false);

                await Task.CompletedTask;
                System.Diagnostics.Debug.WriteLine("[LocationService] Location listening stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocationService] Error in StopListening: {ex.Message}");
                _isCheckingLocation = false;
            }
        }

        public async Task<Location> GetCurrentLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                return await Geolocation.GetLocationAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get location error: {ex.Message}");
                return null;
            }
        }
    }
}
