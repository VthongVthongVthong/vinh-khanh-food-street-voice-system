using System;
using System.Text.Json;
using System.Globalization;
using SQLite;
using VinhKhanhstreetfoods.Models;
using System.Diagnostics;

namespace VinhKhanhstreetfoods.Services;

public class TourRepository : ITourRepository
{
    private readonly string _databasePath;
    private readonly HttpClient _httpClient;
    private SQLiteAsyncConnection? _database;
    private DateTime _lastSyncUtc = DateTime.MinValue;
    private static readonly TimeSpan SyncThrottle = TimeSpan.FromMinutes(5);

    public TourRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
        var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _databasePath = Path.Combine(folderPath, "VinhKhanhFoodGuide.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        _database = new SQLiteAsyncConnection(_databasePath);
        await _database.CreateTableAsync<Tour>();
        await _database.CreateTableAsync<TourPOI>();
        
        Debug.WriteLine("[TourRepository] Initialized");
    }

    public async Task<List<Tour>> GetAllToursAsync()
    {
        await InitializeAsync();
        
        // Auto-sync if needed (throttled)
        if (DateTime.UtcNow - _lastSyncUtc >= SyncThrottle)
        {
            _ = SyncToursFromFirebaseAsync(); // Fire and forget
        }
        
        return await _database!.Table<Tour>()
            .Where(t => t.IsActive == 1)
            .ToListAsync();
    }

    public async Task<Tour?> GetTourByIdAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Tour>()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive == 1);
    }

    public async Task<List<TourPOI>> GetTourPOIsAsync(int tourId)
    {
        await InitializeAsync();
        return await _database!.Table<TourPOI>()
            .Where(tp => tp.TourId == tourId)
            .OrderBy(tp => tp.SortOrder)
            .ToListAsync();
    }

    public async Task<int> AddTourAsync(Tour tour)
    {
        await InitializeAsync();
        return await _database!.InsertAsync(tour);
    }

    public async Task<int> AddTourPOIAsync(TourPOI tourPoi)
    {
        await InitializeAsync();
        return await _database!.InsertAsync(tourPoi);
    }

    /// <summary>
    /// Sync tours and tour_pois from Firebase
    /// </summary>
    public async Task<int> SyncToursFromFirebaseAsync()
    {
        try
        {
            await InitializeAsync();

            var url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/.json";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue 
            { 
                NoCache = true, 
                NoStore = true 
            };

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
 using var response = await _httpClient.SendAsync(request, cts.Token);
      if (!response.IsSuccessStatusCode)
     {
           Debug.WriteLine($"[TourRepository] Firebase sync failed: HTTP {response.StatusCode}");
             return 0;
            }

        var json = await response.Content.ReadAsStringAsync();
  if (string.IsNullOrWhiteSpace(json) || json == "null")
         return 0;

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var tourCount = 0;
       var tourPoiCount = 0;

   // Parse Tour array
    if (TryGetProperty(root, "Tour", out var tourElement) && tourElement.ValueKind == JsonValueKind.Array)
  {
      foreach (var item in tourElement.EnumerateArray())
      {
            if (item.ValueKind != JsonValueKind.Object)
     continue;

       var tour = MapJsonToTour(item);
    if (tour != null)
   {
     var existing = await _database.FindAsync<Tour>(tour.Id);
            if (existing != null)
      await _database.UpdateAsync(tour);
      else
    await _database.InsertAsync(tour);
   tourCount++;
             }
          }
}

            // Parse TourPOI array
          if (TryGetProperty(root, "TourPOI", out var tourPoiElement) && tourPoiElement.ValueKind == JsonValueKind.Array)
      {
       // Clear old mappings
          await _database.ExecuteAsync("DELETE FROM TourPOI");

   foreach (var item in tourPoiElement.EnumerateArray())
      {
      if (item.ValueKind != JsonValueKind.Object)
          continue;

  var tourPoi = MapJsonToTourPOI(item);
      if (tourPoi != null)
                    {
      await _database.InsertAsync(tourPoi);
     tourPoiCount++;
      }
                }
 }

            _lastSyncUtc = DateTime.UtcNow;
  Debug.WriteLine($"[TourRepository] Synced {tourCount} tours, {tourPoiCount} tour-poi mappings");
      return tourCount;
        }
      catch (Exception ex)
        {
       Debug.WriteLine($"[TourRepository] Sync error: {ex.Message}");
  return 0;
        }
    }

    private static Tour? MapJsonToTour(JsonElement item)
    {
      var id = GetInt(item, "id");
        if (id <= 0)
 return null;

        return new Tour
        {
   Id = id,
            Name = GetString(item, "name") ?? "Unnamed Tour",
            Description = GetString(item, "description"),
     IsActive = GetIntOrDefault(item, 1, "isActive", "is_active"),
CreatedAt = GetDateTime(item, "createdAt", "created_at") ?? DateTime.Now,
 UpdatedAt = GetDateTime(item, "updatedAt", "updated_at") ?? DateTime.Now
    };
    }

    private static TourPOI? MapJsonToTourPOI(JsonElement item)
 {
        var tourId = GetInt(item, "tourId", "tour_id");
        var poiId = GetInt(item, "poiId", "poi_id");
      var sortOrder = GetIntOrDefault(item, 0, "sortOrder", "sort_order");

        if (tourId <= 0 || poiId <= 0)
            return null;

        return new TourPOI
        {
         TourId = tourId,
    POIId = poiId,
  SortOrder = sortOrder
        };
    }

    private static bool TryGetProperty(JsonElement obj, string propertyName, out JsonElement value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
          {
     value = property.Value;
  return true;
      }
        }
 value = default;
   return false;
    }

    private static string? GetString(JsonElement obj, params string[] names)
 {
        foreach (var name in names)
    {
          if (TryGetProperty(obj, name, out var value) && value.ValueKind == JsonValueKind.String)
    return value.GetString();
     }
        return null;
    }

    private static int GetInt(JsonElement obj, params string[] names)
 {
        foreach (var name in names)
        {
   if (TryGetProperty(obj, name, out var value))
      {
      if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intVal))
          return intVal;
                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out intVal))
        return intVal;
 }
   }
        return 0;
    }

    private static int GetIntOrDefault(JsonElement obj, int defaultValue, params string[] names)
    {
        var val = GetInt(obj, names);
        return val > 0 ? val : defaultValue;
    }

    private static DateTime? GetDateTime(JsonElement obj, params string[] names)
  {
        foreach (var name in names)
        {
         if (TryGetProperty(obj, name, out var value) && value.ValueKind == JsonValueKind.String)
            {
   if (DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
     return dt;
            }
    }
        return null;
}
}
