using VinhKhanhFoodGuide.Models;
using VinhKhanhFoodGuide.Data;

namespace VinhKhanhFoodGuide.Services;

public interface IGeofenceEngine
{
    event EventHandler<GeofenceEvent> GeofenceTriggered;
    void UpdateLocation(LocationData location);
    Task LoadPoisAsync();
    void SetAudioPlayingState(bool isPlaying);
}

public class GeofenceEngine : IGeofenceEngine
{
    private List<POI> _pois = new();
    private Dictionary<int, DateTime> _lastTriggerTime = new();
    private LocationData _currentLocation;
    private DateTime _lastDebounceTime = DateTime.MinValue;
    private const int DebounceMs = 3000;
    private bool _isAudioPlaying = false;
    private readonly IPoiRepository _repository;

    public event EventHandler<GeofenceEvent> GeofenceTriggered;

    public GeofenceEngine(IPoiRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadPoisAsync()
    {
        try
        {
            _pois = (await _repository.GetAllPoisAsync()).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Load POIs error: {ex.Message}");
        }
    }

    public void UpdateLocation(LocationData location)
    {
        _currentLocation = location;
        CheckGeofences();
    }

    public void SetAudioPlayingState(bool isPlaying)
    {
        _isAudioPlaying = isPlaying;
    }

    private void CheckGeofences()
    {
        if (_currentLocation == null || _isAudioPlaying) return;

        // Debounce check
        if ((DateTime.UtcNow - _lastDebounceTime).TotalMilliseconds < DebounceMs)
            return;

        foreach (var poi in _pois)
        {
            var distance = CalculateDistance(
                _currentLocation.Latitude,
                _currentLocation.Longitude,
                poi.Latitude,
                poi.Longitude
            );

            if (distance <= poi.Radius)
            {
                // Check cooldown
                if (_lastTriggerTime.TryGetValue(poi.Id, out var lastTrigger))
                {
                    var cooldownMs = poi.CooldownMinutes * 60 * 1000;
                    if ((DateTime.UtcNow - lastTrigger).TotalMilliseconds < cooldownMs)
                        continue;
                }

                // Trigger event
                _lastTriggerTime[poi.Id] = DateTime.UtcNow;
                _lastDebounceTime = DateTime.UtcNow;

                var geofenceEvent = new GeofenceEvent
                {
                    PoiId = poi.Id,
                    PoiName = poi.Name,
                    TriggerTime = DateTime.UtcNow,
                    Distance = distance
                };

                GeofenceTriggered?.Invoke(this, geofenceEvent);
            }
        }
    }

    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c; // distance in meters
    }

    private static double ToRad(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}



















































































































}    }        return degrees * Math.PI / 180.0;    {    private static double ToRad(double degrees)    }        return R * c; // distance in meters        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +        var dLon = ToRad(lon2 - lon1);        var dLat = ToRad(lat2 - lat1);        const double R = 6371000; // Earth radius in meters    {    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)    /// </summary>    /// Calculate distance between two coordinates using Haversine formula    /// <summary>    }        }            }                GeofenceTriggered?.Invoke(this, geofenceEvent);                };                    Distance = distance                    TriggerTime = DateTime.UtcNow,                    PoiName = poi.Name,                    PoiId = poi.Id,                {                var geofenceEvent = new GeofenceEvent                _lastDebounceTime = DateTime.UtcNow;                _lastTriggerTime[poi.Id] = DateTime.UtcNow;                // Trigger event                }                        continue;                    if ((DateTime.UtcNow - lastTrigger).TotalMilliseconds < cooldownMs)                    var cooldownMs = poi.CooldownMinutes * 60 * 1000;                {                if (_lastTriggerTime.TryGetValue(poi.Id, out var lastTrigger))                // Check cooldown            {            if (distance <= poi.Radius)            );                poi.Longitude                poi.Latitude,                _currentLocation.Longitude,                _currentLocation.Latitude,            var distance = CalculateDistance(        {        foreach (var poi in _pois)            return;        if ((DateTime.UtcNow - _lastDebounceTime).TotalMilliseconds < DebounceMs)        // Debounce check        if (_currentLocation == null || _isAudioPlaying) return;    {    private void CheckGeofences()    }        _isAudioPlaying = isPlaying;    {    public void SetAudioPlayingState(bool isPlaying)    }        CheckGeofences();        _currentLocation = location;    {    public void UpdateLocation(LocationData location)    }        }            Debug.WriteLine($"Load POIs error: {ex.Message}");        {        catch (Exception ex)        }            _pois = (await _repository.GetAllPoisAsync()).ToList();        {        try    {    public async Task LoadPoisAsync()    }        _repository = repository;    {    public GeofenceEngine(IPoiRepository repository)    public event EventHandler<GeofenceEvent> GeofenceTriggered;    private readonly IPoiRepository _repository;    private bool _isAudioPlaying = false;    private const int DebounceMs = 3000;    private DateTime _lastDebounceTime = DateTime.MinValue;    private LocationData _currentLocation;    private Dictionary<int, DateTime> _lastTriggerTime = new();    private List<POI> _pois = new();{public class GeofenceEngine : IGeofenceEngine}    Task LoadPoisAsync();    void UpdateLocation(LocationData location);    event EventHandler<GeofenceEvent> GeofenceTriggered;{public interface IGeofenceEnginenamespace VinhKhanhFoodGuide.Services;
namespace VinhKhanhFoodGuide.Services;

public interface IGeofenceEngine
{
    event EventHandler<GeofenceEvent> PoiEntered;
    
    void CheckGeofence(LocationData location, List<POI> pois);
    void UpdateCooldowns(TimeSpan elapsed);
    bool CanReplayAudio { get; set; }
}

public class GeofenceEngine : IGeofenceEngine
{
    private readonly Dictionary<int, DateTime> _poiLastTriggeredTime = new();
    private DateTime _lastDebounceCheck = DateTime.UtcNow;
    private const double DEBOUNCE_SECONDS = 3.0;
    private LocationData _lastProcessedLocation;

    public event EventHandler<GeofenceEvent> PoiEntered;
    public bool CanReplayAudio { get; set; } = true;

    // Haversine formula to calculate distance between two coordinate points
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters

        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public void CheckGeofence(LocationData location, List<POI> pois)
    {
        if (location == null || pois == null || pois.Count == 0)
            return;

        // Debounce check - only process if 3 seconds have passed
        var timeSinceLastCheck = (DateTime.UtcNow - _lastDebounceCheck).TotalSeconds;
        if (timeSinceLastCheck < DEBOUNCE_SECONDS)
            return;

        _lastDebounceCheck = DateTime.UtcNow;
        _lastProcessedLocation = location;

        // Sort POIs by priority (descending) and distance
        var relevantPois = pois
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => CalculateDistance(location.Latitude, location.Longitude, p.Latitude, p.Longitude))
            .ToList();

        foreach (var poi in relevantPois)
        {
            var distance = CalculateDistance(location.Latitude, location.Longitude, poi.Latitude, poi.Longitude);

            if (distance <= poi.Radius)
            {
                // Check if POI has cooled down since last trigger
                if (_poiLastTriggeredTime.TryGetValue(poi.Id, out var lastTriggered))
                {
                    var cooldownMs = poi.CooldownMinutes * 60 * 1000;
                    if ((DateTime.UtcNow - lastTriggered).TotalMilliseconds < cooldownMs)
                    {
                        Debug.WriteLine($"POI {poi.Name} is still in cooldown period");
                        continue;
                    }
                }

                // Check if another audio is playing
                if (!CanReplayAudio)
                {
                    Debug.WriteLine($"POI {poi.Name} triggered but audio is already playing");
                    continue;
                }

                // Trigger geofence event
                _poiLastTriggeredTime[poi.Id] = DateTime.UtcNow;
                var geofenceEvent = new GeofenceEvent
                {
                    PoiId = poi.Id,
                    PoiName = poi.Name,
                    TriggerTime = DateTime.UtcNow,
                    Distance = distance
                };

                PoiEntered?.Invoke(this, geofenceEvent);
                Debug.WriteLine($"Geofence triggered for POI: {poi.Name} (Distance: {distance:F1}m)");
            }
        }
    }

    public void UpdateCooldowns(TimeSpan elapsed)
    {
        // This could be called periodically to clean up old cooldown entries
        var expiredPois = _poiLastTriggeredTime
            .Where(kvp => (DateTime.UtcNow - kvp.Value).TotalMinutes > 60)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var poiId in expiredPois)
        {
            _poiLastTriggeredTime.Remove(poiId);
        }
    }
}
