using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public interface IPOIRepository
    {
        Task<List<POI>> GetActivePOIsAsync();
        Task<POI?> GetPOIByIdAsync(int id);

        // dùng cho seed/offline hi?n t?i
        Task<bool> HasAnyPOIAsync();
        Task<int> AddPOIsAsync(List<POI> pois);

        // dùng cho geofence hi?n t?i (update LastTriggered)
        Task<int> UpdatePOIAsync(POI poi);
    }
}