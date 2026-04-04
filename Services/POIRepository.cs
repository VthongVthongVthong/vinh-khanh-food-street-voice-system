using System;
using VinhKhanhstreetfoods.Models;
using SQLite;
using System.Diagnostics;
using System.Linq;

namespace VinhKhanhstreetfoods.Services
{
    public class POIRepository : IPOIRepository
    {
        private readonly string _databasePath;
      private SQLiteAsyncConnection? _database;
      private Task? _schemaInitializationTask;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        public POIRepository()
        {
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
                    return;

     try
 {
         // ? Copy database file asynchronously FIRST
             await EnsureDatabaseFileAsync();

        // ? Create async connection (doesn't block)
               _database = new SQLiteAsyncConnection(_databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

      // ? IMPORTANT: Don't await schema immediately on UI thread
     _schemaInitializationTask ??= InitializeSchemaAsync();

         // ? Give schema 1.2 second to initialize, then continue
            // This prevents UI freeze while still allowing early operations
           _ = _schemaInitializationTask.ContinueWith(t =>
      {
           if (t.IsFaulted)
Debug.WriteLine($"[POIRepository] Schema init failed: {t.Exception?.InnerException}");
        });
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

  /// <summary>
        /// ? PUBLIC: Wait for repository to be fully initialized
        /// Ensures all background schema operations are complete
        /// </summary>
    public async Task EnsureInitializedAsync()
{
         await InitializeAsync();
     await WaitForSchemaReadyAsync(2000); // Wait up to 2 seconds for schema
   }

        private async Task WaitForSchemaReadyAsync(int timeoutMs = 1200)
        {
          var task = _schemaInitializationTask;
    if (task is null)
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
    sortOrder INTEGER NOT NULL DEFAULT 0,
    poiId INTEGER NOT NULL,
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

        private const string createVisitLogSchema = @"CREATE TABLE IF NOT EXISTS VisitLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
 visitTime TEXT NOT NULL DEFAULT (datetime('now')),
    latitude REAL NOT NULL,
    longitude REAL NOT NULL,
    userId INTEGER NOT NULL,
 poiId INTEGER NOT NULL,
 FOREIGN KEY (userId) REFERENCES User(id),
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

     private const string createAudioPlayLogSchema = @"CREATE TABLE IF NOT EXISTS AudioPlayLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
  playTime TEXT NOT NULL DEFAULT (datetime('now')),
    durationListened REAL,
    userId INTEGER NOT NULL,
    poiId INTEGER NOT NULL,
    FOREIGN KEY (userId) REFERENCES User(id),
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

        private const string createTranslationCacheSchema = @"CREATE TABLE IF NOT EXISTS TranslationCache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    poiId INTEGER NOT NULL,
    languageCode TEXT NOT NULL,
    isTtsScript INTEGER NOT NULL DEFAULT 0,
    translatedText TEXT NOT NULL,
    isDownloadedPack INTEGER NOT NULL DEFAULT 0,
    updatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (poiId) REFERENCES POI(id)
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
            // 2?? Insert toàn b? 20 POI
   await _database!.InsertAllAsync(poiDataFromFile);
           Debug.WriteLine($"? Imported {poiDataFromFile.Count} POIs from poi_data_new.sqlite");
        return;
  }

       // 3?? Fallback: n?u file không có, seed m?u
            var initialPOIs = new List<POI>
                {
          new POI
     {
     Id = 1,
    Name = "?c Oanh",
                 Latitude = 10.760866,
             Longitude = 106.682495,
         Address = "534 V?nh Khánh, P.8, Q.4",
            Phone = "0909123001",
          DescriptionText = "Quán ?c lâu ??i n?i ti?ng nh?t trên ph? ?m th?c V?nh Khánh.",
                TtsScript = "Chào m?ng b?n ??n ?c Oanh, quán ?c lâu ??i n?i ti?ng nh?t trên ph? ?m th?c V?nh Khánh, qu?n 4.",
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
            Address = "383 V?nh Khánh, P.8, Q.4",
            Phone = "0388004422",
    DescriptionText = "Quán ?c r?ng rãi, n?i ti?ng v?i các món n??ng và s?t tr?ng mu?i.",
             TtsScript = "Chào m?ng b?n ??n ?c Th?o, quán ?c r?ng rãi n?i ti?ng v?i các món n??ng và s?t tr?ng mu?i, t?i ??a ch? 383 V?nh Khánh.",
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
         // B??c 2: Copy stream thành byte array
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

          // B??c 6: Xóa file t?m
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
  await WaitForSchemaReadyAsync();
       var first = await _database!.Table<POI>().FirstOrDefaultAsync();
   return first != null;
        }

        public async Task<List<POI>> GetAllPOIsAsync()
 {
            await InitializeAsync();
       await WaitForSchemaReadyAsync();
     return await _database!.Table<POI>().ToListAsync();
        }

        public async Task<List<POI>> GetActivePOIsAsync()
        {
            await InitializeAsync();
         await WaitForSchemaReadyAsync();
   return await _database!.Table<POI>().Where(p => p.IsActive == 1).ToListAsync();
     }

        public async Task<POI?> GetPOIByIdAsync(int id)
        {
  await InitializeAsync();
       await WaitForSchemaReadyAsync();
return await _database!.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

    public async Task<int> AddPOIAsync(POI poi)
        {
  await InitializeAsync();
            await WaitForSchemaReadyAsync();
    return await _database!.InsertAsync(poi);
        }

        public async Task<int> AddPOIsAsync(List<POI> pois)
     {
   await InitializeAsync();
            await WaitForSchemaReadyAsync();
          return await _database!.InsertAllAsync(pois);
   }

        public async Task<int> UpdatePOIAsync(POI poi)
        {
         await InitializeAsync();
            await WaitForSchemaReadyAsync();
            poi.UpdatedAt = DateTime.Now;
       return await _database!.UpdateAsync(poi);
        }

        public async Task<int> DeletePOIAsync(POI poi)
  {
            await InitializeAsync();
     await WaitForSchemaReadyAsync();
       return await _database!.DeleteAsync(poi);
        }

    public async Task ClearAllPOIsAsync()
    {
            await InitializeAsync();
            await WaitForSchemaReadyAsync();
            await _database!.DeleteAllAsync<POI>();
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
     await WaitForSchemaReadyAsync();

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
            await WaitForSchemaReadyAsync();

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
       await WaitForSchemaReadyAsync();

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
        await WaitForSchemaReadyAsync();
    await _database!.DeleteAllAsync<TranslationCacheEntry>();
           Debug.WriteLine("[POIRepository] ? All cached translations cleared");
      }
         catch (Exception ex)
{
           Debug.WriteLine($"[POIRepository] Error clearing cache: {ex.Message}");
            }
        }

        private static string NormalizeLang(string? code)
        {
       if (string.IsNullOrWhiteSpace(code)) return "vi";
      var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();
            var dash = normalized.IndexOf('-');
     return dash > 0 ? normalized[..dash] : normalized;
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
