using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using VinhKhanhstreetfoods.Models;
using SQLite;
using System.Diagnostics;
using System.Linq;

namespace VinhKhanhstreetfoods.Services
{
    public class POIRepository : IPOIRepository
    {
        private static readonly Uri[] AdminSyncUris =
        {
            new("https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/.json")
        };
        private static readonly TimeSpan AdminSyncThrottle = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan RealtimeSyncInterval = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan RealtimeSyncMaxInterval = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan InitialRealtimeSyncDelay = TimeSpan.FromSeconds(3);

        private readonly string _databasePath;
        private readonly HttpClient _httpClient;
        private SQLiteAsyncConnection? _database;
        private Task? _schemaInitializationTask;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _adminSyncLock = new SemaphoreSlim(1, 1);
        private Task? _realtimeSyncTask;
        private CancellationTokenSource? _realtimeSyncCts;
        private string? _lastFirebasePayloadHash;
        private DateTime _lastAdminSyncUtc = DateTime.MinValue;
        private DateTime _skipRealtimeSyncUntilUtc = DateTime.MinValue;
        private TimeSpan _currentRealtimeInterval = RealtimeSyncInterval;

        public event EventHandler<int>? POIsSynced;

        public POIRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _databasePath = Path.Combine(folderPath, "VinhKhanhFoodGuide.db3");
        }

        public async Task InitializeAsync()
        {
            // ? CRITICAL FIX: Use SemaphoreSlim to prevent race conditions
            await _initializationLock.WaitAsync();
            try
            {
                if (_database != null)
                {
                    EnsureRealtimeSyncStarted();
                    return;
                }

                try
                {
                    // ? Copy database file asynchronously FIRST
                    await EnsureDatabaseFileAsync();

                    // ? Create async connection (doesn't block)
                    _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

                    // ? IMPORTANT: Don't await schema immediately on UI thread
                    _schemaInitializationTask ??= InitializeSchemaAsync();

                    EnsureRealtimeSyncStarted();

                    // ? Don't wait for schema - let it initialize in background
                    // Fire and forget - UI can proceed with offline data
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[POIRepository] Error initializing database connection: {ex.Message}");
                    throw;
                }
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private void EnsureRealtimeSyncStarted()
        {
            if (_realtimeSyncTask != null)
                return;

            _realtimeSyncCts = new CancellationTokenSource();
            _realtimeSyncTask = Task.Run(() => RealtimeSyncLoopAsync(_realtimeSyncCts.Token));
            Debug.WriteLine($"[POIRepository] Firebase realtime polling scheduled (initial delay: {InitialRealtimeSyncDelay.TotalSeconds}s, interval: {RealtimeSyncInterval.TotalSeconds}s)");
        }

        private async Task RealtimeSyncLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(InitialRealtimeSyncDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow >= _skipRealtimeSyncUntilUtc)
                    {
                        var changedCount = await SyncPOIsFromAdminAsync(force: true, cancellationToken);

                        if (changedCount > 0)
                        {
                            _currentRealtimeInterval = RealtimeSyncInterval;
                        }
                        else
                        {
                            var next = TimeSpan.FromSeconds(Math.Min(
                                RealtimeSyncMaxInterval.TotalSeconds,
                                _currentRealtimeInterval.TotalSeconds + 2));
                            _currentRealtimeInterval = next;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[POIRepository] Realtime sync loop error: {ex.Message}");
                    _currentRealtimeInterval = TimeSpan.FromSeconds(Math.Min(
                        RealtimeSyncMaxInterval.TotalSeconds,
                        _currentRealtimeInterval.TotalSeconds + 2));
                }

                try
                {
                    await Task.Delay(_currentRealtimeInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// ? PUBLIC: Wait for repository to be fully initialized
        /// Ensures all background schema operations are complete
        /// </summary>
    public async Task EnsureInitializedAsync()
{
         await InitializeAsync();
   // ? OPTIMIZED: Use shorter timeout to prevent ANR
      // Schema can continue initializing in background
     await WaitForSchemaReadyAsync(500); // Reduced from 2000ms
   }

    private async Task WaitForSchemaReadyAsync(int timeoutMs = 500)
        {
          var task = _schemaInitializationTask;
  if (task is null)
 return;

      // ? Only wait if schema is still initializing
     if (task.IsCompleted)
       return;

         try
            {
       await Task.WhenAny(task, Task.Delay(timeoutMs));
          }
    catch (Exception ex)
   {
    Debug.WriteLine($"[POIRepository] Warning while waiting for schema: {ex.Message}");
     }
 }

        /// <summary>
      /// ? DEFERRED: Schema initialization runs in background
     /// This prevents ANR by not blocking the UI thread with schema migrations
        /// </summary>
        private async Task InitializeSchemaAsync()
        {
            try
            {
                if (_database == null)
                    return;

                await MigrateFromOldSchemaIfNeeded();
                await EnsureSchemaAsync();
                await EnsureCorePoiColumnsAsync();
                await EnsureHybridTranslationColumnsAsync();

                var count = await _database.Table<POI>().CountAsync();
                Debug.WriteLine($"[POIRepository] Schema initialized. POI table has {count} records.");

                if (count == 0)
                    await SeedInitialDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Error in background schema initialization: {ex.Message}");
                // Don't throw - schema can be fixed on next run
            }
        }

        private async Task EnsureCorePoiColumnsAsync()
        {
            try
            {
                var tableInfo = await _database!.QueryAsync<PragmaTableInfo>("PRAGMA table_info('POI');");
                var columnNames = tableInfo.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                // legacy migration: POI.language -> POI.ttsLanguage
                if (!columnNames.Contains("ttsLanguage") && columnNames.Contains("language"))
                {
                    await _database.ExecuteAsync("ALTER TABLE POI ADD COLUMN ttsLanguage TEXT;");
                    await _database.ExecuteAsync("UPDATE POI SET ttsLanguage = COALESCE(ttsLanguage, language, 'vi');");
                    Debug.WriteLine("[POIRepository] Migrated POI.language -> POI.ttsLanguage");
                }

                var requiredColumns = new (string Name, string SqlType)[]
                {
                    ("ttsLanguage", "TEXT"),
                    ("audioFile", "TEXT"),
                    ("priority", "INTEGER"),
                    ("ownerId", "INTEGER")
                };

                foreach (var (name, sqlType) in requiredColumns)
                {
                    if (!columnNames.Contains(name))
                    {
                        await _database.ExecuteAsync($"ALTER TABLE POI ADD COLUMN {name} {sqlType};");
                        Debug.WriteLine($"[POIRepository] Added core column: POI.{name}");
                    }
                }

                await _database.ExecuteAsync(@"UPDATE POI
SET ttsLanguage = COALESCE(ttsLanguage, 'vi'),
    priority = COALESCE(priority, 1)
WHERE ttsLanguage IS NULL OR priority IS NULL;");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Core column migration error (continuing): {ex.Message}");
            }
        }

        private async Task EnsureDatabaseFileAsync()
        {
        if (File.Exists(_databasePath))
            return;

try
            {
        // ? OPTIMIZED: Use multiple fallback paths for packaged database
           var possiblePaths = new[]
   {
   "poi_data_new.sqlite",
    "poi_data_new.sqlite3",
          "Resources/Raw/poi_data_new.sqlite",
"poi_data.sqlite"
       };

       string? sourcePath = null;
                foreach (var path in possiblePaths)
                {
       try
                    {
              using (var testStream = await FileSystem.OpenAppPackageFileAsync(path))
 {
     sourcePath = path;
   break;
           }
     }
    catch
          {
         // Try next path
 }
                }

                if (string.IsNullOrEmpty(sourcePath))
                {
 Debug.WriteLine("?? [POIRepository] No packaged database found - will use empty database");
   return;
        }

     // ? Only copy if file doesn't exist (safe check)
     if (File.Exists(_databasePath))
  return;

        using var stream = await FileSystem.OpenAppPackageFileAsync(sourcePath);
    using var fileStream = File.Create(_databasePath);
        await stream.CopyToAsync(fileStream);
  Debug.WriteLine($"? [POIRepository] Copied packaged database from {sourcePath}");
            }
  catch (Exception ex)
            {
         Debug.WriteLine($"?? [POIRepository] Could not copy packaged DB: {ex.Message}");
          // Fall through - let database initialize with empty schema
 }
        }

  private async Task EnsureSchemaAsync()
        {
 try
         {
    // ? OPTIMIZED: Check which tables exist first
         var existingTables = await GetExistingTablesAsync();

  // Only create missing tables
      if (!existingTables.Contains("POI"))
           await _database!.CreateTableAsync<POI>();
   if (!existingTables.Contains("Tour"))
          await _database!.CreateTableAsync<Tour>();
        if (!existingTables.Contains("TourPOI"))
        await _database!.CreateTableAsync<TourPOI>();

   // ? Create optional tables if they don't exist (but don't fail if they do)
              var tablesAndSchemas = new (string name, string schema)[]
      {
          ("User", createUserSchema),
        ("POIImage", createPOIImageSchema),
     ("VisitLog", createVisitLogSchema),
        ("AudioPlayLog", createAudioPlayLogSchema),
      ("TranslationCache", createTranslationCacheSchema)
              };

     foreach (var (tableName, schema) in tablesAndSchemas)
    {
     if (!existingTables.Contains(tableName))
          {
          await _database.ExecuteAsync(schema);
      }
        }

    // Create unique index if it doesn't exist
     await _database.ExecuteAsync(
      "CREATE UNIQUE INDEX IF NOT EXISTS IX_TranslationCache_Unique ON TranslationCache (poiId, languageCode, isTtsScript);");
        }
            catch (Exception ex)
      {
       Debug.WriteLine($"[POIRepository] Schema creation error (continuing): {ex.Message}");
        // Don't throw - non-critical tables can be created later if needed
      }
        }

        private async Task<HashSet<string>> GetExistingTablesAsync()
     {
          try
    {
  var result = await _database!.QueryAsync<TableInfo>(
         "SELECT name FROM sqlite_master WHERE type='table';");
     return new HashSet<string>(result.Select(r => r.name), StringComparer.OrdinalIgnoreCase);
         }
            catch
            {
       return new HashSet<string>();
       }
     }

        private const string createUserSchema = @"CREATE TABLE IF NOT EXISTS User (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    passwordHash TEXT NOT NULL,
    email TEXT UNIQUE,
    phone TEXT,
    role TEXT NOT NULL CHECK (role IN ('ADMIN','OWNER','CUSTOMER')),
createdAt TEXT NOT NULL DEFAULT (datetime('now')),
    updatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);";

        private const string createPOIImageSchema = @"CREATE TABLE IF NOT EXISTS POIImage (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    imageUrl TEXT NOT NULL,
    caption TEXT,
    imageType TEXT CHECK (imageType IN ('avatar','banner','gallery')),
    displayOrder INTEGER NOT NULL DEFAULT 0,
    poiId INTEGER NOT NULL,
    createdAt TEXT DEFAULT (datetime('now'))
);";

        private const string createVisitLogSchema = @"CREATE TABLE IF NOT EXISTS VisitLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
 visitTime TEXT NOT NULL DEFAULT (datetime('now')),
    latitude REAL NOT NULL,
    longitude REAL NOT NULL,
    userId INTEGER NOT NULL,
 poiId INTEGER NOT NULL
);";

     private const string createAudioPlayLogSchema = @"CREATE TABLE IF NOT EXISTS AudioPlayLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
  playTime TEXT NOT NULL DEFAULT (datetime('now')),
    durationListened REAL,
    userId INTEGER NOT NULL,
    poiId INTEGER NOT NULL
);";

        private const string createTranslationCacheSchema = @"CREATE TABLE IF NOT EXISTS TranslationCache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    poiId INTEGER NOT NULL,
    languageCode TEXT NOT NULL,
    isTtsScript INTEGER NOT NULL DEFAULT 0,
    translatedText TEXT NOT NULL,
    isDownloadedPack INTEGER NOT NULL DEFAULT 0,
    updatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);";

        private async Task MigrateFromOldSchemaIfNeeded()
        {
try
 {
            // ? OPTIMIZED: Quick check without full PRAGMA if table doesn't exist
 var tableExists = await _database!.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='POI';");

if (tableExists == 0)
       {
            Debug.WriteLine("[POIRepository] POI table doesn't exist - no migration needed");
         return;
            }

      // Only if POI table exists, check for legacy columns
var tableInfo = await _database.QueryAsync<PragmaTableInfo>("PRAGMA table_info('POI');");
      var hasLanguageColumn = tableInfo.Any(c => c.Name.Equals("language", StringComparison.OrdinalIgnoreCase));
    var hasTtsScriptColumn = tableInfo.Any(c => c.Name.Equals("ttsScript", StringComparison.OrdinalIgnoreCase));

    if (!hasLanguageColumn && hasTtsScriptColumn)
              {
           // Schema is already modern - no migration needed
       return;
            }

  Debug.WriteLine("[POIRepository] Detected legacy schema - starting migration...");

       await _database.ExecuteAsync("ALTER TABLE POI RENAME TO POI_old;");
         await _database.CreateTableAsync<POI>();

                const string copySql = @"INSERT INTO POI (id,name,latitude,longitude,address,phone,descriptionText,ttsScript,ttsLanguage,imageUrls,mapLink,triggerRadiusMeters,isActive,createdAt,updatedAt,ownerId)
SELECT id,
       name,
latitude,
       longitude,
       address,
 phone,
       descriptionText,
       COALESCE(ttsScript, descriptionText),
       'vi',
       imageUrls,
     mapLink,
       COALESCE(triggerRadiusMeters, 20),
    isActive,
       createdAt,
       updatedAt,
       ownerId
FROM POI_old;";

   await _database.ExecuteAsync(copySql);
          await _database.ExecuteAsync("DROP TABLE IF EXISTS POI_old;");

          Debug.WriteLine("[POIRepository] Migration completed successfully.");
        }
            catch (Exception ex)
          {
  Debug.WriteLine($"[POIRepository] Migration error (continuing): {ex.Message}");
       // Don't throw - migration failure shouldn't crash app
            }
        }

        private async Task SeedInitialDataAsync()
        {
            try
            {
                // 1?? Th? load t? poi_data_new.sqlite
         var poiDataFromFile = await TryLoadPOIFromEmbeddedDatabaseAsync();

      if (poiDataFromFile?.Count > 0)
            {
            // 2?? Insert to�n b? 20 POI
   await _database!.InsertAllAsync(poiDataFromFile);
           Debug.WriteLine($"? Imported {poiDataFromFile.Count} POIs from poi_data_new.sqlite");
        return;
  }

       // 3?? Fallback: n?u file kh�ng c�, seed m?u
            var initialPOIs = new List<POI>
                {
          new POI
     {
     Id = 1,
    Name = "?c Oanh",
                 Latitude = 10.760866,
             Longitude = 106.682495,
         Address = "534 V?nh Kh�nh, P.8, Q.4",
            Phone = "0909123001",
          DescriptionText = "Qu�n ?c l�u ??i n?i ti?ng nh?t tr�n ph? ?m th?c V?nh Kh�nh.",
                TtsScript = "Ch�o m?ng b?n ??n ?c Oanh, qu�n ?c l�u ??i n?i ti?ng nh?t tr�n ph? ?m th?c V?nh Kh�nh, qu?n 4.",
              ImageUrls = "[\"https://cdn.vinhkhanh.vn/img/poi1-avatar.jpg\", \"https://cdn.vinhkhanh.vn/img/poi1-banner.jpg\"]",
        MapLink = "https://maps.app.goo.gl/oc-oanh",
            TriggerRadius = 20,
       IsActive = 1,
  CreatedAt = DateTime.Now,
      UpdatedAt = DateTime.Now,
       OwnerId = 2
      },
           new POI
     {
      Id = 2,
     Name = "?c Th?o",
       Latitude = 10.761234,
             Longitude = 106.682800,
            Address = "383 V?nh Kh�nh, P.8, Q.4",
            Phone = "0388004422",
    DescriptionText = "Qu�n ?c r?ng r�i, n?i ti?ng v?i c�c m�n n??ng v� s?t tr?ng mu?i.",
             TtsScript = "Ch�o m?ng b?n ??n ?c Th?o, qu�n ?c r?ng r�i n?i ti?ng v?i c�c m�n n??ng v� s?t tr?ng mu?i, t?i ??a ch? 383 V?nh Kh�nh.",
    ImageUrls = "[\"https://cdn.vinhkhanh.vn/img/poi2-avatar.jpg\", \"https://cdn.vinhkhanh.vn/img/poi2-banner.jpg\"]",
      MapLink = "https://maps.app.goo.gl/oc-thao",
            TriggerRadius = 20,
    IsActive = 1,
             CreatedAt = DateTime.Now,
   UpdatedAt = DateTime.Now,
       OwnerId = 2
      }
   };

    await _database!.InsertAllAsync(initialPOIs);
       Debug.WriteLine($"?? Seeded {initialPOIs.Count} sample POIs (file not found)");
          }
   catch (Exception ex)
          {
   Debug.WriteLine($"? Error seeding: {ex.Message}");
            }
        }

        private async Task<List<POI>?> TryLoadPOIFromEmbeddedDatabaseAsync()
      {
            try
       {
            // B??c 1: M? file poi_data_new.sqlite t? app package
        using (var stream = await FileSystem.OpenAppPackageFileAsync("poi_data_new.sqlite"))
      {
         // B??c 2: Copy stream th�nh byte array
              var bytes = new byte[stream.Length];
  await stream.ReadAsync(bytes, 0, (int)stream.Length);

            // B??c 3: T?o file t?m
 var tempPath = Path.Combine(Path.GetTempPath(), "poi_temp.db3");
       await File.WriteAllBytesAsync(tempPath, bytes);

           // B??c 4: M? connection t?i temp file
    var sourceDb = new SQLiteAsyncConnection(tempPath);
   try
     {
   // B??c 5: Query t?t c? POI
      var pois = await sourceDb.Table<POI>().ToListAsync();

          // B??c 6: X�a file t?m
   File.Delete(tempPath);

        return pois;
           }
               finally
     {
     await sourceDb.CloseAsync();  // ? G?i CloseAsync()
        }
   }
            }
          catch (Exception ex)
    {
          Debug.WriteLine($"?? Could not load from embedded: {ex.Message}");
       return null;
            }
        }

    public async Task<bool> HasAnyPOIAsync()
  {
            await InitializeAsync();
  // ? Don't wait for schema - just proceed with query
     var first = await _database!.Table<POI>().FirstOrDefaultAsync();
   return first != null;
        }

 public async Task<List<POI>> GetAllPOIsAsync()
 {
           await InitializeAsync();
   return await _database!.Table<POI>().ToListAsync();
    }

        public async Task<List<POI>> GetActivePOIsAsync()
      {
  await InitializeAsync();
    return await _database!.Table<POI>().Where(p => p.IsActive == 1).ToListAsync();
        }

 public async Task<POI?> GetPOIByIdAsync(int id)
        {
  await InitializeAsync();
   return await _database!.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Dictionary<int, string>> GetAllAvatarImagesAsync()
        {
            await InitializeAsync();
            var result = new Dictionary<int, string>();

            try
            {
                // Primary source: POIImage table (type = avatar)
                var avatarRows = await _database!.Table<POIImage>()
                    .Where(img => img.Type == "avatar")
                    .OrderBy(img => img.POIId)
                    .ThenBy(img => img.DisplayOrder)
                    .ToListAsync();

                foreach (var row in avatarRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.ImageUrl) && !result.ContainsKey(row.POIId))
                        result[row.POIId] = row.ImageUrl;
                }

                Debug.WriteLine($"[POIRepository] [AVATAR] Loaded {result.Count} avatars from POIImage table");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] [AVATAR] POIImage table read failed: {ex.Message}");
            }

            // Per-POI fallback: fill missing avatar from POI.ImageUrls first URL.
            try
            {
                var pois = await _database!.Table<POI>().Where(p => p.IsActive == 1).ToListAsync();
                var fallbackCount = 0;

                foreach (var poi in pois)
                {
                    if (result.ContainsKey(poi.Id) || string.IsNullOrWhiteSpace(poi.ImageUrls))
                        continue;

                    try
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(poi.ImageUrls) ?? new List<string>();
                        var first = list.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
                        if (!string.IsNullOrWhiteSpace(first))
                        {
                            result[poi.Id] = first;
                            fallbackCount++;
                        }
                    }
                    catch
                    {
                        // ignore malformed JSON in ImageUrls
                    }
                }

                if (fallbackCount > 0)
                    Debug.WriteLine($"[POIRepository] [AVATAR] Fallback filled {fallbackCount} avatars from POI.ImageUrls");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] [AVATAR] Fallback from POI.ImageUrls failed: {ex.Message}");
            }

            return result;
        }

        public async Task<Dictionary<int, string>> GetAllBannerImagesAsync()
        {
            await InitializeAsync();
            var result = new Dictionary<int, string>();

            try
            {
                var bannerRows = await _database!.Table<POIImage>()
                    .Where(img => img.Type == "banner")
                    .OrderBy(img => img.POIId)
                    .ThenBy(img => img.DisplayOrder)
                    .ToListAsync();

                foreach (var row in bannerRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.ImageUrl) && !result.ContainsKey(row.POIId))
                        result[row.POIId] = row.ImageUrl;
                }

                Debug.WriteLine($"[POIRepository] [BANNER] Loaded {result.Count} banners from POIImage table");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] [BANNER] POIImage table read failed: {ex.Message}");
            }

            // Per-POI fallback: use first ImageUrl
            try
            {
                var pois = await _database!.Table<POI>().Where(p => p.IsActive == 1).ToListAsync();
                var fallbackCount = 0;

                foreach (var poi in pois)
                {
                    if (result.ContainsKey(poi.Id) || string.IsNullOrWhiteSpace(poi.ImageUrls))
                        continue;

                    try
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(poi.ImageUrls) ?? new List<string>();
                        var first = list.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
                        if (!string.IsNullOrWhiteSpace(first))
                        {
                            result[poi.Id] = first;
                            fallbackCount++;
                        }
                    }
                    catch
                    {
                    }
                }

                if (fallbackCount > 0)
                    Debug.WriteLine($"[POIRepository] [BANNER] Fallback filled {fallbackCount} banners from POI.ImageUrls");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] [BANNER] Fallback from POI.ImageUrls failed: {ex.Message}");
            }

            return result;
        }

        public async Task<string?> GetPOIAvatarImageAsync(int poiId)
        {
            var all = await GetAllAvatarImagesAsync();
            return all.TryGetValue(poiId, out var url) ? url : null;
        }

        public async Task<int> UpsertPOIImageAsync(POIImage image)
        {
            await InitializeAsync();
            try
            {
                var existing = await _database!.Table<POIImage>()
                    .Where(img => img.POIId == image.POIId && img.Type == image.Type)
                    .FirstOrDefaultAsync();

                if (existing is null)
                {
                    return await _database.InsertAsync(image);
                }

                existing.ImageUrl = image.ImageUrl;
                existing.DisplayOrder = image.DisplayOrder;
                existing.CreatedAt = DateTime.Now;
                return await _database.UpdateAsync(existing);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] [AVATAR] Upsert error for POI={image.POIId}, Type={image.Type}: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> AddPOIAsync(POI poi)
        {
  await InitializeAsync();
         return await _database!.InsertAsync(poi);
 }

        public async Task<int> AddPOIsAsync(List<POI> pois)
    {
    await InitializeAsync();
       return await _database!.InsertAllAsync(pois);
  }

     public async Task<int> UpdatePOIAsync(POI poi)
   {
   await InitializeAsync();
   poi.UpdatedAt = DateTime.Now;
   return await _database!.UpdateAsync(poi);
        }

  public async Task<int> DeletePOIAsync(POI poi)
{
  await InitializeAsync();
    return await _database!.DeleteAsync(poi);
        }

 public async Task ClearAllPOIsAsync()
  {
    await InitializeAsync();
      await _database!.DeleteAllAsync<POI>();
        }

        public Task<DateTime?> GetLastAdminSyncTimeUtcAsync()
        {
            DateTime? value = _lastAdminSyncUtc == DateTime.MinValue ? null : _lastAdminSyncUtc;
            return Task.FromResult(value);
        }

        public async Task<int> SyncPOIsFromAdminAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            // Removed Connectivity.Current.NetworkAccess check for Android emulator/device compatibility.
            // If offline, the HttpClient will simply fail gracefully.
            await _adminSyncLock.WaitAsync(cancellationToken);
            try
            {
                if (!force && DateTime.UtcNow - _lastAdminSyncUtc < AdminSyncThrottle)
                    return 0;

                await InitializeAsync();
                // Wait for schema before syncing so we don't hit locked database or missing columns
                // on slower Android devices during startup.
                if (_schemaInitializationTask != null && !_schemaInitializationTask.IsCompleted)
                {
                    await _schemaInitializationTask;
                }

                var payload = await FetchFirebasePayloadAsync(force, cancellationToken);
                if (string.IsNullOrWhiteSpace(payload) || string.Equals(payload.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("[POIRepository] Firebase returned null/empty at known paths. Check RTDB node path and rules.");
                    _lastAdminSyncUtc = DateTime.UtcNow;
                    if (force) throw new Exception("Không tải được dữ liệu JSON, Firebase trả về rỗng.");
                    return 0;
                }

                var payloadHash = ComputePayloadHash(payload);
                // ALWAYS respect hash check, even if force=true. This prevents wasteful DB writes
                // and UI flashes when the fetched payload is identical to what we already have.
                if (string.Equals(payloadHash, _lastFirebasePayloadHash, StringComparison.Ordinal))
                {
                    _lastAdminSyncUtc = DateTime.UtcNow;
                    return 0;
                }

                var pois = ParseAdminPoiPayload(payload);
                if (pois.Count == 0)
                {
                    Debug.WriteLine("[POIRepository] Firebase sync returned empty/invalid payload");
                    _lastAdminSyncUtc = DateTime.UtcNow;
                    _lastFirebasePayloadHash = payloadHash;
                    return 0;
                }

                var poiImages = ParsePoiImagesFromPayload(payload);
                Debug.WriteLine($"[POIRepository] [AVATAR] Parsed {poiImages.Count} POIImage rows from payload");

                var now = DateTime.Now;
                var inserted = 0;

                await _database!.RunInTransactionAsync(db =>
                {
                    // Clean up duplicate shifts from previous autoincrement bug
                    if (pois.Count > 0)
                    {
                        var firebaseIds = string.Join(",", pois.Select(p => p.Id));
                        
                        // Xóa các record con bị mồ côi do ràng buộc khoá ngoại (FOREIGN KEY constraint)
                        db.Execute($"DELETE FROM POIImage WHERE poiId NOT IN ({firebaseIds})");
                        db.Execute($"DELETE FROM AudioPlayLog WHERE poiId NOT IN ({firebaseIds})");
                        db.Execute($"DELETE FROM VisitLog WHERE poiId NOT IN ({firebaseIds})");
                        db.Execute($"DELETE FROM TranslationCache WHERE poiId NOT IN ({firebaseIds})");
                        // Tạm thời Disable foreign_keys để xử lý dedupe rowid (hạn chế lỗi Android sqlite)
                        db.Execute("PRAGMA foreign_keys = OFF;");
                        db.Execute($"DELETE FROM POI WHERE id NOT IN ({firebaseIds})");
                    }

                    foreach (var poi in pois)
                    {
                        ApplyDefaultsForPersistedPoi(poi, now);
                        
                        var existing = db.Find<POI>(poi.Id);
                        if (existing != null)
                        {
                            db.Update(poi);
                        }
                        else
                        {
                            db.Insert(poi);
                        }
                        
                        inserted++;
                    }
                    
                    if (pois.Count > 0)
                    {
                        db.Execute("PRAGMA foreign_keys = ON;");
                    }
                });

                // Hard dedupe by business key Id (keep latest rowid)
                await _database.ExecuteAsync("PRAGMA foreign_keys = OFF;");
                await _database.ExecuteAsync(@"DELETE FROM POI
WHERE rowid NOT IN (
    SELECT MAX(rowid)
    FROM POI
    WHERE Id IS NOT NULL
    GROUP BY Id
);");
                await _database.ExecuteAsync("PRAGMA foreign_keys = ON;");

                // Attempt to enforce uniqueness for future syncs
                try
                {
                    await _database.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_POI_Id_Unique ON POI(id);");
                }
                catch (Exception idxEx)
                {
                    Debug.WriteLine($"[POIRepository] Unique index IX_POI_Id_Unique skipped: {idxEx.Message}");
                }

                Debug.WriteLine($"[POIRepository] Sync upsert done. Inserted={inserted}");

                if (poiImages.Count > 0)
                {
                    foreach (var image in poiImages)
                    {
                        try
                        {
                            var existingImage = await _database.Table<POIImage>()
                                .Where(x => x.POIId == image.POIId && x.Type == image.Type)
                                .FirstOrDefaultAsync();

                            if (existingImage is null)
                            {
                                await _database.InsertAsync(image);
                            }
                            else
                            {
                                existingImage.ImageUrl = image.ImageUrl;
                                existingImage.DisplayOrder = image.DisplayOrder;
                                existingImage.CreatedAt = DateTime.Now;
                                await _database.UpdateAsync(existingImage);
                            }
                        }
                        catch (Exception imgEx)
                        {
                            Debug.WriteLine($"[POIRepository] [AVATAR] Upsert image failed for POI={image.POIId}, Type={image.Type}: {imgEx.Message}");
                        }
                    }
                }

                _lastFirebasePayloadHash = payloadHash;
                _lastAdminSyncUtc = DateTime.UtcNow;
                _skipRealtimeSyncUntilUtc = DateTime.UtcNow.AddSeconds(8); // avoid immediate repetitive pulls after manual refresh
                POIsSynced?.Invoke(this, pois.Count);
                Debug.WriteLine($"[POIRepository] Synced {pois.Count} POIs from Firebase");
                return pois.Count;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Firebase sync error (fallback to offline DB): {ex.Message}");
                if (force) throw new Exception($"Lỗi trong quá trình cập nhật offline DB: {ex.Message}");
                return 0;
            }
            finally
            {
                _adminSyncLock.Release();
            }
        }

        private async Task<string?> FetchFirebasePayloadAsync(bool force, CancellationToken cancellationToken)
        {
            Exception? lastException = null;
            foreach (var baseUri in AdminSyncUris)
            {
                var uri = baseUri;
                try
                {
                    if (force)
                    {
                        var uriBuilder = new UriBuilder(baseUri);
                        var query = uriBuilder.Query ?? string.Empty;
                        if (query.StartsWith("?")) query = query.Substring(1);
                        var separator = string.IsNullOrEmpty(query) ? "" : "&";
                        uriBuilder.Query = $"{query}{separator}_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                        uri = uriBuilder.Uri;
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Accept.ParseAdd("application/json");
                    request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true, NoStore = true };

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"[POIRepository] Firebase path failed: {uri} -> HTTP {(int)response.StatusCode}");
                        if (force) lastException = new Exception($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                        continue;
                    }

                    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(payload) && !string.Equals(payload.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                    {
                        var nextHash = ComputePayloadHash(payload);
                        if (!string.Equals(nextHash, _lastFirebasePayloadHash, StringComparison.Ordinal))
                            Debug.WriteLine($"[POIRepository] Firebase source path: {uri}");

                        return payload;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[POIRepository] Firebase path error: {uri} -> {ex.Message}");
                    if (force) lastException = ex;
                }
            }

            if (force && lastException != null)
                throw new Exception($"Không thể gọi Firebase API: {lastException.Message}");

            return null;
        }

        private static string ComputePayloadHash(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static List<POI> ParseAdminPoiPayload(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return new List<POI>();

            try
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                JsonElement poiArray;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    Debug.WriteLine("[POIRepository] Payload root is array - parsing directly as POIs");
                    poiArray = root;
                    return ParsePoiArray(poiArray);
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryFindPoiArray(root, out var extracted))
                        return ParsePoiArray(extracted);

                    if (TryFindPoiObject(root, out var poiObject))
                    {
                        Debug.WriteLine("[POIRepository] Payload has POI object map - parsing object entries");
                        return ParseFirebasePoiMap(poiObject);
                    }

                    // Firebase RTDB fallback format: { "1": {poi...}, "2": {poi...} }
                    Debug.WriteLine("[POIRepository] Fallback parse as root object map");
                    return ParseFirebasePoiMap(root);
                }

                Debug.WriteLine($"[POIRepository] Unsupported payload root kind: {root.ValueKind}");
                return new List<POI>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Invalid Firebase JSON: {ex.Message}");
                return new List<POI>();
            }
        }

        private static bool TryFindPoiObject(JsonElement root, out JsonElement poiObject)
        {
            var candidates = new[] { "data", "pois", "poi", "POI", "result", "items", "rows" };
            foreach (var key in candidates)
            {
                if (TryGetPropertyIgnoreCase(root, key, out var value) && value.ValueKind == JsonValueKind.Object)
                {
                    poiObject = value;
                    return true;
                }
            }

            poiObject = default;
            return false;
        }

        private static List<POI> ParsePoiArray(JsonElement poiArray)
        {
            var result = new List<POI>();
            foreach (var item in poiArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var poi = MapJsonToPoi(item);
                if (poi is not null)
                    result.Add(poi);
            }

            return result;
        }

        private static List<POI> ParseFirebasePoiMap(JsonElement root)
        {
            var result = new List<POI>();
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                    continue;

                var fallbackId = int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var keyId)
                    ? keyId
                    : 0;

                var poi = MapJsonToPoi(property.Value, fallbackId);
                if (poi is not null)
                    result.Add(poi);
            }

            return result;
        }

        private static POI? MapJsonToPoi(JsonElement item)
            => MapJsonToPoi(item, 0);

        private static POI? MapJsonToPoi(JsonElement item, int fallbackId)
        {
            var id = GetInt(item, "id", "poiId", "poi_id");
            if (id <= 0)
                id = GetInt(item, "Id");
            if (id <= 0)
                id = fallbackId;

            if (id <= 0)
                return null;

            var createdAt = GetDateTime(item, "createdAt", "created_at") ?? GetUnixDateTime(item, "createdAtUnix", "createdAtTs", "created_ts");
            var updatedAt = GetDateTime(item, "updatedAt", "updated_at") ?? GetUnixDateTime(item, "updatedAtUnix", "updatedAtTs", "updated_ts");

            var poi = new POI
            {
                Id = id,
                Name = GetString(item, "name", "title") ?? string.Empty,
                Latitude = GetDouble(item, "latitude", "lat"),
                Longitude = GetDouble(item, "longitude", "lng", "lon"),
                Address = GetString(item, "address"),
                Phone = GetString(item, "phone", "phoneNumber"),
                DescriptionText = GetString(item, "descriptionText", "description_text", "description") ?? string.Empty,
                DescriptionEn = GetString(item, "descriptionEn", "description_en"),
                DescriptionZh = GetString(item, "descriptionZh", "description_zh"),
                DescriptionJa = GetString(item, "descriptionJa", "description_ja"),
                DescriptionKo = GetString(item, "descriptionKo", "description_ko"),
                DescriptionFr = GetString(item, "descriptionFr", "description_fr"),
                DescriptionRu = GetString(item, "descriptionRu", "description_ru"),
                TtsScript = GetString(item, "ttsScript", "tts_script"),
                TtsScriptEn = GetString(item, "ttsScriptEn", "tts_script_en"),
                TtsScriptZh = GetString(item, "ttsScriptZh", "tts_script_zh"),
                TtsScriptJa = GetString(item, "ttsScriptJa", "tts_script_ja"),
                TtsScriptKo = GetString(item, "ttsScriptKo", "tts_script_ko"),
                TtsScriptFr = GetString(item, "ttsScriptFr", "tts_script_fr"),
                TtsScriptRu = GetString(item, "ttsScriptRu", "tts_script_ru"),
                TtsLanguage = GetString(item, "ttsLanguage", "tts_language", "language") ?? "vi",
                AudioFile = GetString(item, "audioFile", "audio_file", "audioUrl", "audio_url"),
                ImageUrls = NormalizeImageUrls(item),
                MapLink = GetString(item, "mapLink", "map_link"),
                TriggerRadius = GetIntOrDefault(item, 20, "triggerRadiusMeters", "trigger_radius_meters", "triggerRadius", "radius"),
                Priority = GetIntOrDefault(item, 1, "priority"),
                IsActive = GetIntOrDefault(item, 1, "isActive", "is_active", "active"),
                CreatedAt = createdAt ?? default,
                UpdatedAt = updatedAt ?? default,
                OwnerId = GetInt(item, "ownerId", "owner_id")
            };

            return poi;
        }

        private static int GetIntOrDefault(JsonElement obj, int defaultValue, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                    return intValue;

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
                    return intValue;
            }

            return defaultValue;
        }

        private static DateTime? GetUnixDateTime(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Number)
                {
                    if (value.TryGetInt64(out var unixSeconds) && unixSeconds > 0)
                        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;

                    if (value.TryGetDouble(out var unixDouble) && unixDouble > 0)
                        return DateTimeOffset.FromUnixTimeSeconds((long)unixDouble).LocalDateTime;
                }

                if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedUnix) && parsedUnix > 0)
                    return DateTimeOffset.FromUnixTimeSeconds(parsedUnix).LocalDateTime;
            }

            return null;
        }

        private static void ApplyDefaultsForPersistedPoi(POI poi, DateTime now)
        {
            poi.Name = string.IsNullOrWhiteSpace(poi.Name) ? "(Kh�ng r� t�n)" : poi.Name.Trim();
            poi.DescriptionText = string.IsNullOrWhiteSpace(poi.DescriptionText) ? poi.Name : poi.DescriptionText.Trim();
            poi.TtsScript ??= poi.DescriptionText;
            poi.TtsLanguage = string.IsNullOrWhiteSpace(poi.TtsLanguage) ? "vi" : poi.TtsLanguage;
            poi.ImageUrls = string.IsNullOrWhiteSpace(poi.ImageUrls) ? "[]" : poi.ImageUrls;
            poi.TriggerRadius = poi.TriggerRadius <= 0 ? 20 : poi.TriggerRadius;
            poi.Priority = poi.Priority <= 0 ? 1 : poi.Priority;
            poi.IsActive = poi.IsActive is 0 or 1 ? poi.IsActive : 1;
            poi.CreatedAt = poi.CreatedAt == default ? now : poi.CreatedAt;
            poi.UpdatedAt = now;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string propertyName, out JsonElement value)
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
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString();

                if (value.ValueKind == JsonValueKind.Number || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                    return value.ToString();
            }

            return null;
        }

        private static int GetInt(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                    return intValue;

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out intValue))
                    return intValue;
            }

            return 0;
        }

        private static double GetDouble(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleValue))
                    return doubleValue;

                if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    return doubleValue;
            }

            return 0;
        }

        private static DateTime? GetDateTime(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyIgnoreCase(obj, name, out var value))
                    continue;

                if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                    return dateTime;
            }

            return null;
        }

        private static bool TryFindPoiArray(JsonElement root, out JsonElement poiArray)
        {
            // Support Firebase keys: POI, poi, pois, data...
            var candidates = new[] { "POI", "poi", "pois", "data", "result", "items", "rows" };
            foreach (var key in candidates)
            {
                if (TryGetPropertyIgnoreCase(root, key, out var value) && value.ValueKind == JsonValueKind.Array)
                {
                    Debug.WriteLine($"[POIRepository] Found POI array key='{key}', length={value.GetArrayLength()}");
                    poiArray = value;
                    return true;
                }
            }

            Debug.WriteLine("[POIRepository] No POI array key found in payload root object");
            poiArray = default;
            return false;
        }

        private async Task EnsureHybridTranslationColumnsAsync()
        {
            try
    {
                var tableInfo = await _database!.QueryAsync<PragmaTableInfo>("PRAGMA table_info('POI');");

        // ? OPTIMIZED: Only add missing columns
    var columnNames = tableInfo.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

  var missingColumns = new[]
                {
           ("descriptionEn", "TEXT"),
    ("descriptionZh", "TEXT"),
       ("descriptionJa", "TEXT"),
("descriptionKo", "TEXT"),
            ("descriptionFr", "TEXT"),
           ("descriptionRu", "TEXT"),
             ("ttsScriptEn", "TEXT"),
           ("ttsScriptZh", "TEXT"),
             ("ttsScriptJa", "TEXT"),
     ("ttsScriptKo", "TEXT"),
 ("ttsScriptFr", "TEXT"),
           ("ttsScriptRu", "TEXT")
    };

 foreach (var (columnName, sqlType) in missingColumns)
            {
  if (!columnNames.Contains(columnName))
              {
    await _database.ExecuteAsync($"ALTER TABLE POI ADD COLUMN {columnName} {sqlType};");
    Debug.WriteLine($"[POIRepository] Added column: POI.{columnName}");
            }
      }

        await _database.ExecuteAsync(@"UPDATE POI
SET descriptionEn = COALESCE(descriptionEn, descriptionText),
    descriptionZh = COALESCE(descriptionZh, descriptionText),
    descriptionJa = COALESCE(descriptionJa, descriptionText),
    descriptionKo = COALESCE(descriptionKo, descriptionText),
    descriptionFr = COALESCE(descriptionFr, descriptionText),
    descriptionRu = COALESCE(descriptionRu, descriptionText),
    ttsScriptEn = COALESCE(ttsScriptEn, ttsScript, descriptionText),
    ttsScriptZh = COALESCE(ttsScriptZh, ttsScript, descriptionText),
    ttsScriptJa = COALESCE(ttsScriptJa, ttsScript, descriptionText),
    ttsScriptKo = COALESCE(ttsScriptKo, ttsScript, descriptionText),
    ttsScriptFr = COALESCE(ttsScriptFr, ttsScript, descriptionText),
    ttsScriptRu = COALESCE(ttsScriptRu, ttsScript, descriptionText)
WHERE descriptionEn IS NULL
   OR descriptionZh IS NULL
   OR descriptionJa IS NULL
   OR descriptionKo IS NULL
   OR descriptionFr IS NULL
   OR descriptionRu IS NULL
   OR ttsScriptEn IS NULL
   OR ttsScriptZh IS NULL
   OR ttsScriptJa IS NULL
   OR ttsScriptKo IS NULL
   OR ttsScriptFr IS NULL
   OR ttsScriptRu IS NULL;");
            }
  catch (Exception ex)
            {
      Debug.WriteLine($"[POIRepository] Translation columns error (continuing): {ex.Message}");
 }
        }

        public async Task<string?> GetCachedTranslationAsync(int poiId, string languageCode, bool isTtsScript)
        {
          await InitializeAsync();
     // ? Don't wait for schema
     var normalized = NormalizeLang(languageCode);
            var entry = await _database!.Table<TranslationCacheEntry>()
                .Where(x => x.PoiId == poiId && x.LanguageCode == normalized && x.IsTtsScript == (isTtsScript ? 1 : 0))
   .OrderByDescending(x => x.UpdatedAt)
       .FirstOrDefaultAsync();

          return entry?.TranslatedText;
        }

        public async Task UpsertCachedTranslationAsync(int poiId, string languageCode, bool isTtsScript, string translatedText, bool isDownloadedPack = false)
   {
   await InitializeAsync();
     // ? Don't wait for schema
  var normalized = NormalizeLang(languageCode);
    var flag = isTtsScript ? 1 : 0;

   var existing = await _database!.Table<TranslationCacheEntry>()
      .Where(x => x.PoiId == poiId && x.LanguageCode == normalized && x.IsTtsScript == flag)
     .FirstOrDefaultAsync();

    if (existing is null)
         {
    await _database.InsertAsync(new TranslationCacheEntry
        {
     PoiId = poiId,
        LanguageCode = normalized,
        IsTtsScript = flag,
        TranslatedText = translatedText,
     IsDownloadedPack = isDownloadedPack ? 1 : 0,
     UpdatedAt = DateTime.UtcNow
       });
       return;
      }

   existing.TranslatedText = translatedText;
   existing.IsDownloadedPack = isDownloadedPack ? 1 : existing.IsDownloadedPack;
            existing.UpdatedAt = DateTime.UtcNow;

     await _database.UpdateAsync(existing);
}

        public async Task<bool> HasDownloadedLanguagePackAsync(string languageCode)
        {
   await InitializeAsync();
    // ? Don't wait for schema
         var normalized = NormalizeLang(languageCode);
     var count = await _database!.Table<TranslationCacheEntry>()
            .Where(x => x.LanguageCode == normalized && x.IsDownloadedPack == 1)
      .CountAsync();

 return count > 0;
   }

        /// <summary>
  /// Clear all cached translations (run when language changes).
        /// </summary>
        public async Task ClearCachedTranslationsAsync()
    {
 try
{
   await InitializeAsync();
   // ? Don't wait for schema
    await _database!.DeleteAllAsync<TranslationCacheEntry>();
           Debug.WriteLine("[POIRepository] ? All cached translations cleared");
      }
       catch (Exception ex)
{
       Debug.WriteLine($"[POIRepository] Error clearing cache: {ex.Message}");
       }
 }

        private static List<POIImage> ParsePoiImagesFromPayload(string? payload)
        {
            var result = new List<POIImage>();
            if (string.IsNullOrWhiteSpace(payload))
                return result;

            try
{
   using var document = JsonDocument.Parse(payload);
   var root = document.RootElement;

          // ? Ch�nh x�c: JSON c� "POIImage" (s? �t) kh�ng ph?i "POIImages"
     if (!TryGetPropertyIgnoreCase(root, "POIImage", out var imagesElement))
     {
        Debug.WriteLine("[POIRepository] [AVATAR] No POIImage key in payload");
     return result;
       }

 if (imagesElement.ValueKind != JsonValueKind.Array)
                {
      Debug.WriteLine($"[POIRepository] [AVATAR] POIImage is not array. Kind={imagesElement.ValueKind}");
             return result;
    }

    Debug.WriteLine($"[POIRepository] [AVATAR] POIImage array found with {imagesElement.GetArrayLength()} items");

    foreach (var item in imagesElement.EnumerateArray())
          {
       // Skip null items (Firebase JSON c� th? c� null entries)
          if (item.ValueKind != JsonValueKind.Object)
               {
   Debug.WriteLine("[POIRepository] [AVATAR] Skipping non-object item in POIImage array");
              continue;
       }

      var mapped = MapJsonToPOIImage(item);
           if (mapped is not null)
           {
      result.Add(mapped);
       Debug.WriteLine($"[POIRepository] [AVATAR] Mapped POIImage: POI={mapped.POIId}, Type={mapped.Type}, URL={mapped.ImageUrl}");
        }
     }

   Debug.WriteLine($"[POIRepository] [AVATAR] ParsePoiImagesFromPayload: {result.Count} images mapped successfully");
            }
      catch (Exception ex)
            {
   Debug.WriteLine($"[POIRepository] [AVATAR] ParsePoiImagesFromPayload error: {ex.Message}");
   }

   return result;
        }

        private static POIImage? MapJsonToPOIImage(JsonElement item)
        {
      var poiId = GetInt(item, "poiId", "poi_id");
     var imageUrl = GetString(item, "imageUrl", "image_url");
            var imageType = GetString(item, "imageType", "image_type", "type") ?? string.Empty;
    var sortOrder = GetIntOrDefault(item, 0, "sortOrder", "sort_order", "displayOrder", "display_order");

         if (poiId <= 0)
   {
     Debug.WriteLine("[POIRepository] [AVATAR] Skipping image: poiId is 0 or negative");
    return null;
      }

       if (string.IsNullOrWhiteSpace(imageUrl))
     {
    Debug.WriteLine($"[POIRepository] [AVATAR] Skipping image for POI {poiId}: imageUrl is empty");
    return null;
            }

      if (string.IsNullOrWhiteSpace(imageType))
            {
                Debug.WriteLine($"[POIRepository] [AVATAR] Skipping image for POI {poiId}: imageType is empty");
    return null;
  }

        return new POIImage
     {
       POIId = poiId,
  ImageUrl = imageUrl.Trim(),
  Type = imageType.Trim().ToLowerInvariant(),
    DisplayOrder = sortOrder,
          CreatedAt = DateTime.Now
            };
        }

      private static string NormalizeLang(string? code)
        {
     if (string.IsNullOrWhiteSpace(code)) return "vi";
      var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();
            var dash = normalized.IndexOf('-');
     return dash > 0 ? normalized[..dash] : normalized;
        }

    private static string NormalizeImageUrls(JsonElement item)
        {
            if (item.ValueKind != JsonValueKind.Object)
                return "[]";

            try
            {
                var urls = new List<string>();

                // Check for common array properties for image URLs
                foreach (var property in item.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String && Uri.IsWellFormedUriString(property.Value.GetString(), UriKind.Absolute))
                    {
                        urls.Add(property.Value.GetString());
                    }

                    // Check for array elements (Firebase might return image URLs as an array)
                    else if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var arrayItem in property.Value.EnumerateArray())
                        {
                            if (arrayItem.ValueKind == JsonValueKind.String && Uri.IsWellFormedUriString(arrayItem.GetString(), UriKind.Absolute))
                            {
                                urls.Add(arrayItem.GetString());
                            }
                        }
                    }
                }

                // Remove duplicates and return as JSON array string
                return JsonSerializer.Serialize(urls.Distinct().ToList());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Error normalizing image URLs: {ex.Message}");
                return "[]";
            }
        }

        private class PragmaTableInfo
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }

        private class TableInfo
        {
            [Column("name")]
            public string name { get; set; } = string.Empty;
        }
    }
}
