using System;
using SQLite;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

public class TourRepository : ITourRepository
{
    private readonly string _databasePath;
    private SQLiteAsyncConnection? _database;

    public TourRepository()
    {
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
    }

    public async Task<List<Tour>> GetAllToursAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Tour>().ToListAsync();
    }

    public async Task<Tour?> GetTourByIdAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Tour>().FirstOrDefaultAsync(t => t.Id == id);
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
}
