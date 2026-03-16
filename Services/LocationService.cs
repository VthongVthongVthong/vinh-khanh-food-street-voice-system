using Microsoft.Maui.Devices.Sensors;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class LocationService
    {
        private CancellationTokenSource _cancelTokenSource;
        private bool _isCheckingLocation;
        private Location _lastLocation;
        private DateTime _lastUpdateTime;

        public event EventHandler<Location> LocationUpdated;

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
            if (_isCheckingLocation)
                return;

            var hasPermission = await CheckAndRequestLocationPermission();
            if (hasPermission != PermissionStatus.Granted)
                return;

            _isCheckingLocation = true;

            // Adaptive interval based on speed
            Geolocation.LocationChanged += Geolocation_LocationChanged;

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
            {
                // Adaptive interval logic
                DesiredAccuracy = GeolocationAccuracy.Best,
                MinimumTime = TimeSpan.FromSeconds(intervalSeconds)
            };

            try
            {
                await Geolocation.StartListeningForegroundAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Location listening error: {ex.Message}");
                _isCheckingLocation = false;
            }
        }

        private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
        {
            _lastLocation = e.Location;
            _lastUpdateTime = DateTime.Now;

            // Calculate speed and adjust interval
            AdjustUpdateInterval(e.Location);

            LocationUpdated?.Invoke(this, e.Location);
        }

        private void AdjustUpdateInterval(Location location)
        {
            // Simple speed-based interval adjustment
            if (location.Speed.HasValue)
            {
                double speedKmh = location.Speed.Value * 3.6;

                // Walking (< 5 km/h): 5 seconds
                // Biking (5-20 km/h): 3 seconds  
                // Driving (>20 km/h): 2 seconds
                int newInterval = speedKmh < 5 ? 5 : speedKmh < 20 ? 3 : 2;

                // Could implement actual interval change here
            }
        }

        public async Task StopListening()
        {
            if (!_isCheckingLocation)
                return;

            try
            {
                Geolocation.LocationChanged -= Geolocation_LocationChanged;
            }
            catch
            {
                // Ignore if event handler wasn't subscribed
            }
            
            _isCheckingLocation = false;
            await Task.CompletedTask;
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
                Debug.WriteLine($"Get location error: {ex.Message}");
                return null;
            }
        }
    }
}
