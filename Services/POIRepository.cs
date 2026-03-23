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

        public POIRepository()
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _databasePath = Path.Combine(folderPath, "VinhKhanhFoodGuide.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            try
            {
                await EnsureDatabaseFileAsync(); // đảm bảo file được copy trước

                _database = new SQLiteAsyncConnection(_databasePath);

                await MigrateFromOldSchemaIfNeeded();
                await EnsureSchemaAsync();

                var count = await _database.Table<POI>().CountAsync();
                Debug.WriteLine($"[POIRepository] Database initialized. POI table has {count} records.");

                if (count == 0)
                    await SeedInitialDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureDatabaseFileAsync()
        {
            if (File.Exists(_databasePath))
                return;

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("poi_data_new.sqlite");
                using var fileStream = File.Create(_databasePath);
                await stream.CopyToAsync(fileStream);
                Debug.WriteLine("✅ [POIRepository] Copied packaged poi_data_new.sqlite to local db");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ [POIRepository] Could not copy packaged DB: {ex.Message}");
            }
        }

        private async Task EnsureSchemaAsync()
        {
            await EnsureTableExistsAsync<POI>("POI");
            await EnsureTableExistsAsync<Tour>("Tour");
            await EnsureTableExistsAsync<TourPOI>("TourPOI");

            const string createUser = @"CREATE TABLE IF NOT EXISTS User (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    passwordHash TEXT NOT NULL,
    email TEXT UNIQUE,
    phone TEXT,
    role TEXT NOT NULL CHECK (role IN ('ADMIN','OWNER','CUSTOMER')),
    createdAt TEXT NOT NULL DEFAULT (datetime('now')),
    updatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);";

            const string createPOIImage = @"CREATE TABLE IF NOT EXISTS POIImage (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    imageUrl TEXT NOT NULL,
    caption TEXT,
    imageType TEXT CHECK (imageType IN ('avatar','banner','gallery')),
    sortOrder INTEGER NOT NULL DEFAULT 0,
    poiId INTEGER NOT NULL,
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

            const string createVisitLog = @"CREATE TABLE IF NOT EXISTS VisitLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    visitTime TEXT NOT NULL DEFAULT (datetime('now')),
    latitude REAL NOT NULL,
    longitude REAL NOT NULL,
    userId INTEGER NOT NULL,
    poiId INTEGER NOT NULL,
    FOREIGN KEY (userId) REFERENCES User(id),
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

            const string createAudioPlayLog = @"CREATE TABLE IF NOT EXISTS AudioPlayLog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    playTime TEXT NOT NULL DEFAULT (datetime('now')),
    durationListened REAL,
    userId INTEGER NOT NULL,
    poiId INTEGER NOT NULL,
    FOREIGN KEY (userId) REFERENCES User(id),
    FOREIGN KEY (poiId) REFERENCES POI(id)
);";

            await _database!.ExecuteAsync(createUser);
            await _database.ExecuteAsync(createPOIImage);
            await _database.ExecuteAsync(createVisitLog);
            await _database.ExecuteAsync(createAudioPlayLog);
        }

        private async Task EnsureTableExistsAsync<T>(string tableName) where T : new()
        {
            var exists = await _database!.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=?;",
                tableName);
            if (exists == 0)
                await _database.CreateTableAsync<T>();
        }

        private async Task MigrateFromOldSchemaIfNeeded()
        {
            // Check if POI table exists and has legacy columns
            var tableInfo = await _database!.QueryAsync<PragmaTableInfo>("PRAGMA table_info('POI');");
            var hasLanguageColumn = tableInfo.Any(c => c.Name.Equals("language", StringComparison.OrdinalIgnoreCase));
            var hasTtsScriptColumn = tableInfo.Any(c => c.Name.Equals("ttsScript", StringComparison.OrdinalIgnoreCase));

            if (tableInfo.Count == 0)
            {
                // Table missing entirely; creation will happen later
                return;
            }

            if (hasLanguageColumn || !hasTtsScriptColumn)
            {
                Debug.WriteLine("[POIRepository] Migrating POI table from legacy schema...");

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

                Debug.WriteLine("[POIRepository] Migration completed.");
            }
        }

        private async Task SeedInitialDataAsync()
        {
            try
            {
                // 1️⃣ Thử load từ poi_data_new.sqlite
                var poiDataFromFile = await TryLoadPOIFromEmbeddedDatabaseAsync();
                
                if (poiDataFromFile?.Count > 0)
                {
                    // 2️⃣ Insert toàn bộ 20 POI
                    await _database!.InsertAllAsync(poiDataFromFile);
                    Debug.WriteLine($"✅ Imported {poiDataFromFile.Count} POIs from poi_data_new.sqlite");
                    return;
                }

                // 3️⃣ Fallback: nếu file không có, seed mẫu
                var initialPOIs = new List<POI>
                {
                    new POI
                    {
                        Id = 1,
                        Name = "Ốc Oanh",
                        Latitude = 10.760866,
                        Longitude = 106.682495,
                        Address = "534 Vĩnh Khánh, P.8, Q.4",
                        Phone = "0909123001",
                        DescriptionText = "Quán ốc lâu đời nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh.",
                        TtsScript = "Chào mừng bạn đến Ốc Oanh, quán ốc lâu đời nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh, quận 4.",
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
                        Name = "Ốc Thảo",
                        Latitude = 10.761234,
                        Longitude = 106.682800,
                        Address = "383 Vĩnh Khánh, P.8, Q.4",
                        Phone = "0388004422",
                        DescriptionText = "Quán ốc rộng rãi, nổi tiếng với các món nướng và sốt trứng muối.",
                        TtsScript = "Chào mừng bạn đến Ốc Thảo, quán ốc rộng rãi nổi tiếng với các món nướng và sốt trứng muối, tại địa chỉ 383 Vĩnh Khánh.",
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
                Debug.WriteLine($"⚠️ Seeded {initialPOIs.Count} sample POIs (file not found)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error seeding: {ex.Message}");
            }
        }

        private async Task<List<POI>?> TryLoadPOIFromEmbeddedDatabaseAsync()
        {
            try
            {
                // Bước 1: Mở file poi_data_new.sqlite từ app package
                using (var stream = await FileSystem.OpenAppPackageFileAsync("poi_data_new.sqlite"))
                {
                    // Bước 2: Copy stream thành byte array
                    var bytes = new byte[stream.Length];
                    await stream.ReadAsync(bytes, 0, (int)stream.Length);
                    
                    // Bước 3: Tạo file tạm
                    var tempPath = Path.Combine(Path.GetTempPath(), "poi_temp.db3");
                    await File.WriteAllBytesAsync(tempPath, bytes);
                    
                    // Bước 4: Mở connection tới temp file
                    var sourceDb = new SQLiteAsyncConnection(tempPath);
                    try
                    {
                        // Bước 5: Query tất cả POI
                        var pois = await sourceDb.Table<POI>().ToListAsync();
                        
                        // Bước 6: Xóa file tạm
                        File.Delete(tempPath);
                        
                        return pois;
                    }
                    finally
                    {
                        await sourceDb.CloseAsync();  // ✅ Gọi CloseAsync()
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Could not load from embedded: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> HasAnyPOIAsync()
        {
            await InitializeAsync();
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

        private class PragmaTableInfo
        {
            [Column("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
