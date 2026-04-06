using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

public interface IPOIRepository
{
    event EventHandler<int>? POIsSynced;

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

    // Avatar image
    Task<string?> GetPOIAvatarImageAsync(int poiId);
    Task<int> UpsertPOIImageAsync(POIImage image);
    Task<Dictionary<int, string>> GetAllAvatarImagesAsync();

    // Admin online sync (offline-first: safe to fail and continue with local DB)
    Task<int> SyncPOIsFromAdminAsync(bool force = false, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastAdminSyncTimeUtcAsync();
}