using VinhKhanhstreetfoods.Models;
using System.Collections.Concurrent;

namespace VinhKhanhstreetfoods.Services
{
    public class GeofenceEngine
    {
      private readonly IPOIRepository _poiRepository;
        private readonly AudioManager _audioManager;
        private readonly HybridPopupService _hybridPopupService;
    private readonly Dictionary<int, DateTime> _poiCooldowns = new();
    private readonly Dictionary<int, DateTime> _activeVisits = new();
    private readonly PresenceTrackerService _presenceTrackerService;
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromSeconds(5);
 private readonly TimeSpan _debouncePeriod = TimeSpan.FromSeconds(5);
        private readonly SemaphoreSlim _checkLock = new(1, 1);
        private readonly TimeSpan _refreshPoisInterval = TimeSpan.FromSeconds(15);

        private DateTime _lastPoiRefresh = DateTime.MinValue;
        private List<POI> _cachedActivePois = new();
        private Location _lastUserLocation;

        public event EventHandler<POI> POITriggered;

        public GeofenceEngine(IPOIRepository poiRepository, AudioManager audioManager, HybridPopupService hybridPopupService, PresenceTrackerService presenceTrackerService)
        {
 _poiRepository = poiRepository;
       _audioManager = audioManager;
          _hybridPopupService = hybridPopupService;
          _presenceTrackerService = presenceTrackerService;
        }

  public async Task CheckPOIs(Location userLocation)
  {
         if (userLocation == null)
    {
             System.Diagnostics.Debug.WriteLine("[GeofenceEngine] ? User location is null");
  return;
         }

        System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? User at {userLocation.Latitude:F6}, {userLocation.Longitude:F6}");
            _lastUserLocation = userLocation;

       if (!await _checkLock.WaitAsync(0))
      {
      System.Diagnostics.Debug.WriteLine("[GeofenceEngine] ? Previous check still running, skipping");
        return;
      }

try
            {
     var allPOIs = await GetActivePoisWithCacheAsync();

        if (allPOIs == null || allPOIs.Count == 0)
    {
     System.Diagnostics.Debug.WriteLine("[GeofenceEngine] ?? No active POIs");
return;
          }

           System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? Checking {allPOIs.Count} POIs");

       // ? FIX: Use concurrent processing to avoid main thread blocking
          var triggeredPois = new ConcurrentBag<(POI poi, double distance)>();
          var exitedPois = new ConcurrentBag<(POI poi, DateTime enterTime)>();
              
        await Task.Run(() =>
        {
          Parallel.ForEach(allPOIs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, poi =>
   {
          try
         {
    var distance = CalculateDistance(
   userLocation.Latitude, userLocation.Longitude,
  poi.Latitude, poi.Longitude);

          poi.DistanceFromUser = distance;

         // Check cooldown ONLY if we might trigger
              if (distance > poi.TriggerRadius)
          {
               lock (_activeVisits)
               {
                   if (_activeVisits.TryGetValue(poi.Id, out var enterTime))
                   {
                       exitedPois.Add((poi, enterTime));
                       _activeVisits.Remove(poi.Id);
                   }
               }
      return; // Out of range, skip
      }

   lock (_poiCooldowns)
                  {
   if (_poiCooldowns.TryGetValue(poi.Id, out var lastTriggered))
          {
    var timeSinceLast = DateTime.Now - lastTriggered;
          if (timeSinceLast < _cooldownPeriod)
           {
       return; // Still in cooldown
   }
         }
   }

          // Check debounce
          var timeSinceLastTrigger = DateTime.Now - poi.LastTriggered;
                 if (timeSinceLastTrigger < _debouncePeriod)
                 {
      return; // Debounced
           }

       // POI is triggerable - add to results
   triggeredPois.Add((poi, distance));
    }
      catch (Exception poiEx)
             {
       System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? Error checking POI {poi?.Id}: {poiEx.Message}");
       }
       });
       });

           // ? Handle all triggered POIs on main thread
                foreach (var (poi, distance) in triggeredPois)
                {
                     lock (_activeVisits)
                     {
                         if (!_activeVisits.ContainsKey(poi.Id))
                         {
                             _activeVisits[poi.Id] = DateTime.Now;
                         }
                     }
         System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? TRIGGER: {poi.Name} ({distance:F1}m)");
       await TriggerPOI(poi, distance);
            }

            foreach (var (poi, enterTime) in exitedPois)
            {
                var exitTime = DateTime.Now;
                var duration = (exitTime - enterTime).TotalSeconds;
                _ = _presenceTrackerService.LogVisitAsync(poi.Id, enterTime, exitTime, duration, userLocation.Latitude, userLocation.Longitude, "AUTO");
            }

    if (triggeredPois.Count == 0)
         {
         System.Diagnostics.Debug.WriteLine("[GeofenceEngine] ? No POIs in range");
          }
            }
     catch (Exception ex)
            {
           System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ? Error: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? Cached {_cachedActivePois.Count} POIs");
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

        private async Task TriggerPOI(POI poi, double distance)
        {
  try
            {
     if (poi == null)
        {
        return;
          }

   poi.LastTriggered = DateTime.Now;
       lock (_poiCooldowns)
         {
_poiCooldowns[poi.Id] = DateTime.Now;
   }

     // ? FIX: Call popup service with minimal delay
           await _hybridPopupService.HandleIncomingPOIAsync(poi, distance);
           
           // ? Add to audio queue to play TTS
           _audioManager.AddToQueue(poi);

         // Fire-and-forget DB update
        _ = Task.Run(async () =>
   {
          try
         {
     await _poiRepository.UpdatePOIAsync(poi);
   }
               catch (Exception updateEx)
      {
   System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ?? POI update error: {updateEx.Message}");
     }
        });

        POITriggered?.Invoke(this, poi);
                System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ? {poi.Name} event fired");
     }
      catch (Exception ex)
          {
        System.Diagnostics.Debug.WriteLine($"[GeofenceEngine] ? Trigger error: {ex.Message}");
   }
        }

        public void ResetCooldown(int poiId) => _poiCooldowns.Remove(poiId);

        public void StopAllVisits()
        {
            if (_lastUserLocation == null) return;
            var exitTime = DateTime.Now;
            lock (_activeVisits)
            {
                foreach (var kvp in _activeVisits)
                {
                    var enterTime = kvp.Value;
                    var duration = (exitTime - enterTime).TotalSeconds;
                    _ = _presenceTrackerService.LogVisitAsync(kvp.Key, enterTime, exitTime, duration, _lastUserLocation.Latitude, _lastUserLocation.Longitude, "AUTO");
                }
                _activeVisits.Clear();
            }
        }
    }
}
