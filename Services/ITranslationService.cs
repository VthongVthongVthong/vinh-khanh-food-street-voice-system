namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Handles text translation to different languages.
/// Uses external API (Google Translate, Azure, etc.)
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate text from source to target language
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="sourceLanguage">Source language code (e.g., "vi")</param>
    /// <param name="targetLanguage">Target language code (e.g., "en")</param>
    /// <returns>Translated text</returns>
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);

    /// <summary>
    /// Check if translation service is available
    /// </summary>
    Task<bool> IsAvailableAsync();
}