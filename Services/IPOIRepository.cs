using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

public interface IPOIRepository
{
    Task InitializeAsync();
    Task<bool> HasAnyPOIAsync();
    Task<List<POI>> GetAllPOIsAsync();
    Task<List<POI>> GetActivePOIsAsync();
    Task<POI?> GetPOIByIdAsync(int id);
    Task<int> AddPOIAsync(POI poi);
    Task<int> AddPOIsAsync(List<POI> pois);
    Task<int> UpdatePOIAsync(POI poi);
    Task<int> DeletePOIAsync(POI poi);
    Task ClearAllPOIsAsync();
}