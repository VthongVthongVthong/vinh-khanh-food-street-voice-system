using VinhKhanhstreetfoods.Models;
using SQLite;

namespace VinhKhanhstreetfoods.Services
{
    public class POIRepository : IPOIRepository
    {
        private readonly string _databasePath;
        private SQLiteAsyncConnection _database;

        public POIRepository()
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _databasePath = Path.Combine(folderPath, "VinhKhanhFoodGuide.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_databasePath);
            await _database.CreateTableAsync<POI>();
        }

        public async Task<bool> HasAnyPOIAsync()
        {
            await InitializeAsync();
            var first = await _database.Table<POI>().FirstOrDefaultAsync();
            return first != null;
        }

        public async Task<List<POI>> GetAllPOIsAsync()
        {
            await InitializeAsync();
            return await _database.Table<POI>().ToListAsync();
        }

        public async Task<List<POI>> GetActivePOIsAsync()
        {
            await InitializeAsync();
            return await _database.Table<POI>().Where(p => p.IsActive).ToListAsync();
        }

        public async Task<POI?> GetPOIByIdAsync(int id)
        {
            await InitializeAsync();
            return await _database.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> AddPOIAsync(POI poi)
        {
            await InitializeAsync();
            return await _database.InsertAsync(poi);
        }

        public async Task<int> AddPOIsAsync(List<POI> pois)
        {
            await InitializeAsync();
            return await _database.InsertAllAsync(pois);
        }

        public async Task<int> UpdatePOIAsync(POI poi)
        {
            await InitializeAsync();
            return await _database.UpdateAsync(poi);
        }

        public async Task<int> DeletePOIAsync(POI poi)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(poi);
        }

        public async Task ClearAllPOIsAsync()
        {
            await InitializeAsync();
            await _database.DeleteAllAsync<POI>();
        }
    }
}
