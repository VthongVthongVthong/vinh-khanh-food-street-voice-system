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
        private readonly SemaphoreSlim _checkLock = new(1, 1);
        private readonly TimeSpan _refreshPoisInterval = TimeSpan.FromSeconds(15);

        private DateTime _lastPoiRefresh = DateTime.MinValue;
        private List<POI> _cachedActivePois = new();

        public event EventHandler<POI> POITriggered;

        public GeofenceEngine(IPOIRepository poiRepository, AudioManager audioManager)
        {
            _poiRepository = poiRepository;
            _audioManager = audioManager;
        }

        public async Task CheckPOIs(Location userLocation)
        {
            if (userLocation == null)
            {
                System.Diagnostics.Debug.WriteLine("[GeofenceEngine] User location is null");
                return;
            }

            if (!await _checkLock.WaitAsync(0))
            {
                // Skip this tick if previous check still running to avoid backlog/ANR pressure
                return;
            }

            try
            {
                var allPOIs = await GetActivePoisWithCacheAsync();

                if (allPOIs == null || allPOIs.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[GeofenceEngine] No active POIs found");
                    return;
                }

                foreach (var poi in allPOIs)
                {
                    try
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
                    catch (Exception poiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] Error checking POI {poi?.Id}: {poiEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] Error in CheckPOIs: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _checkLock.Release();
            }
        }

        private async Task<List<POI>> GetActivePoisWithCacheAsync()
        {
            var now = DateTime.UtcNow;
            if (_cachedActivePois.Count > 0 && now - _lastPoiRefresh < _refreshPoisInterval)
            {
                return _cachedActivePois;
            }

            var pois = await _poiRepository.GetActivePOIsAsync();
            _cachedActivePois = pois ?? new List<POI>();
            _lastPoiRefresh = now;
            return _cachedActivePois;
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
            try
            {
                if (poi == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GeofenceEngine] Cannot trigger null POI");
                    return;
                }

                poi.LastTriggered = DateTime.Now;
                _poiCooldowns[poi.Id] = DateTime.Now;

                try
                {
                    _audioManager.AddToQueue(poi);
                }
                catch (Exception audioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] Warning: Could not add POI to audio queue: {audioEx.Message}");
                }

                // Fire-and-forget DB update so trigger path stays lightweight
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _poiRepository.UpdatePOIAsync(poi);
                    }
                    catch (Exception updateEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] Warning: Could not update POI last triggered time: {updateEx.Message}");
                    }
                });

                POITriggered?.Invoke(this, poi);

                System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] POI Triggered: {poi.Name} at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] Error in TriggerPOI: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        public void ResetCooldown(int poiId) => _poiCooldowns.Remove(poiId);
    }
}
