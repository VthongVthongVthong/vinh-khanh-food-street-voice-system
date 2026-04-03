namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Handles text translation to different languages.
/// Uses offline-first hybrid approach with API fallback.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate generic text from source to target language.
    /// </summary>
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);

    /// <summary>
    /// Resolve POI narration text using hybrid strategy:
    /// offline columns (vi/en/zh) first, then online API (ja/ko/fr/ru), then fallback.
    /// </summary>
    Task<string> ResolveNarrationTextAsync(VinhKhanhstreetfoods.Models.POI poi, string targetLanguage, bool preferTtsScript = true);

    /// <summary>
    /// Check if translation service is available.
    /// Offline mode returns true for base languages.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Prefetch and cache translation packs for API languages.
    /// Returns number of cached items.
    /// </summary>
    Task<int> DownloadLanguagePackAsync(string languageCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get language codes available for immediate offline use.
    /// </summary>
    IReadOnlyList<string> GetOfflineBaseLanguages();

    /// <summary>
    /// Get language codes that require online translation unless pre-cached.
    /// </summary>
    IReadOnlyList<string> GetOnlineLanguages();
}