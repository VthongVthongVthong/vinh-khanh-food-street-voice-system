using VinhKhanhstreetfoods.Models;
using SQLite;
using System.Diagnostics;

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
                _database = new SQLiteAsyncConnection(_databasePath);
                
                // Create the POI table
                await _database.CreateTableAsync<POI>();
                
                // Check if table has data
                var count = await _database.Table<POI>().CountAsync();
                
                Debug.WriteLine($"[POIRepository] Database initialized. POI table has {count} records.");
                
                // If empty, seed with initial data
                if (count == 0)
                {
                    await SeedInitialDataAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task SeedInitialDataAsync()
        {
            try
            {
                // Create sample POI data
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
                        ImageUrls = "[\"https://cdn.vinhkhanh.vn/img/poi1-avatar.jpg\", \"https://cdn.vinhkhanh.vn/img/poi1-banner.jpg\"]",
                        Language = "vi",
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
                        ImageUrls = "[\"https://cdn.vinhkhanh.vn/img/poi2-avatar.jpg\", \"https://cdn.vinhkhanh.vn/img/poi2-banner.jpg\"]",
                        Language = "vi",
                        MapLink = "https://maps.app.goo.gl/oc-thao",
                        TriggerRadius = 20,
                        IsActive = 1,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        OwnerId = 2
                    }
                };

                await _database!.InsertAllAsync(initialPOIs);
                Debug.WriteLine($"[POIRepository] Seeded {initialPOIs.Count} initial POIs");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[POIRepository] Error seeding data: {ex.Message}");
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
    }
}
