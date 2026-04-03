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

    Task<string?> GetCachedTranslationAsync(int poiId, string languageCode, bool isTtsScript);
    Task UpsertCachedTranslationAsync(int poiId, string languageCode, bool isTtsScript, string translatedText, bool isDownloadedPack = false);
    Task<bool> HasDownloadedLanguagePackAsync(string languageCode);
    Task ClearCachedTranslationsAsync();
}