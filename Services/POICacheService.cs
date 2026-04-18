using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// ? NEW: Shared POI cache service
/// Enables Pages/ViewModels to share cached POI data without redundant reloads
/// Reduces database queries and improves performance across the app
/// </summary>
public class POICacheService
{
    private static POICacheService? _instance;
    private Dictionary<int, POI> _poiCache = new();
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30); // 30-minute cache validity

    public static POICacheService Instance => _instance ??= new POICacheService();

  /// <summary>
    /// Check if cache has data and is still valid
    /// </summary>
    public bool IsCacheValid => _poiCache.Count > 0 && (DateTime.UtcNow - _lastCacheTime) < _cacheExpiration;

    /// <summary>
    /// Get cached POI by ID - returns null if not in cache or cache expired
    /// </summary>
    public POI? GetCachedPOI(int poiId)
  {
if (!IsCacheValid)
    return null;

    return _poiCache.TryGetValue(poiId, out var poi) ? poi : null;
    }

    /// <summary>
    /// Get all cached POIs
    /// </summary>
    public List<POI> GetAllCachedPOIs()
    {
        if (!IsCacheValid)
     return new List<POI>();

        return _poiCache.Values.ToList();
    }

 /// <summary>
    /// Update cache with new POI data
    /// </summary>
    public void UpdateCache(IEnumerable<POI> pois)
    {
     _poiCache.Clear();
        foreach (var poi in pois)
        {
            _poiCache[poi.Id] = poi;
        }
  _lastCacheTime = DateTime.UtcNow;
      System.Diagnostics.Debug.WriteLine($"[POICacheService] ? Cache updated with {_poiCache.Count} POIs");
    }

    /// <summary>
    /// Update cache with a single POI or batch of POIs
    /// </summary>
    public void UpdateCacheSingle(POI poi)
    {
        _poiCache[poi.Id] = poi;
     System.Diagnostics.Debug.WriteLine($"[POICacheService] ? Cache updated for POI {poi.Id}");
    }

    /// <summary>
    /// Clear cache (e.g., when syncing new data from server)
    /// </summary>
    public void ClearCache()
    {
        _poiCache.Clear();
  _lastCacheTime = DateTime.MinValue;
        System.Diagnostics.Debug.WriteLine($"[POICacheService] ??? Cache cleared");
    }

    /// <summary>
    /// Get cache stats for debugging
    /// </summary>
    public string GetCacheStats()
    {
        var age = DateTime.UtcNow - _lastCacheTime;
        var isValid = IsCacheValid;
  return $"POIs: {_poiCache.Count}, Age: {age.TotalSeconds:F1}s, Valid: {isValid}";
    }
}
