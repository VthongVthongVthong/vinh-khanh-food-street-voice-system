using VinhKhanhFoodGuide.Models;

namespace VinhKhanhFoodGuide.Data;

public interface IPoiRepository
{
    Task<IEnumerable<POI>> GetAllPoisAsync();
    Task<POI> GetPoiByIdAsync(int id);
    Task<IEnumerable<POIContent>> GetPoiContentAsync(int poiId);
    Task<POIContent> GetPoiContentByLanguageAsync(int poiId, string languageCode);
    Task InsertPoiAsync(POI poi);
    Task InsertPoiContentAsync(POIContent content);
    Task UpdatePoiAsync(POI poi);
    Task DeletePoiAsync(int id);
    Task InitializeDatabaseAsync();
}
