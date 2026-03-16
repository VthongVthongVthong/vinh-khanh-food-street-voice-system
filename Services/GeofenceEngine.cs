using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class GeofenceEngine
    {
        private readonly IPOIRepository _poiRepository;
        private readonly AudioManager _audioManager;
        private readonly Dictionary<int, DateTime> _poiCooldowns = new();
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _debouncePeriod = TimeSpan.FromSeconds(5);

        public event EventHandler<POI> POITriggered;

        public GeofenceEngine(IPOIRepository poiRepository, AudioManager audioManager)
        {
            _poiRepository = poiRepository;
            _audioManager = audioManager;
        }

        public async Task CheckPOIs(Location userLocation)
        {
            if (userLocation == null)
                return;

            var allPOIs = await _poiRepository.GetActivePOIsAsync();

            foreach (var poi in allPOIs)
            {
                if (_poiCooldowns.TryGetValue(poi.Id, out var lastTriggered))
                {
                    if (DateTime.Now - lastTriggered < _cooldownPeriod)
                        continue;
                }

                var distance = CalculateDistance(
                    userLocation.Latitude, userLocation.Longitude,
                    poi.Latitude, poi.Longitude);

                poi.DistanceFromUser = distance;

                if (distance <= poi.TriggerRadius)
                {
                    if (DateTime.Now - poi.LastTriggered < _debouncePeriod)
                        continue;

                    await TriggerPOI(poi);
                }
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        private async Task TriggerPOI(POI poi)
        {
            poi.LastTriggered = DateTime.Now;
            _poiCooldowns[poi.Id] = DateTime.Now;

            await _poiRepository.UpdatePOIAsync(poi);

            _audioManager.AddToQueue(poi);
            POITriggered?.Invoke(this, poi);

            Debug.WriteLine($"POI Triggered: {poi.Name} at {DateTime.Now}");
        }

        public void ResetCooldown(int poiId) => _poiCooldowns.Remove(poiId);
    }
}
