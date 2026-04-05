using System.Diagnostics;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Offline-first translation service.
/// Third-party unofficial runtime translation is disabled to keep startup/runtime stable.
/// </summary>
public class TranslationService : ITranslationService
{
    public Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        // No external translator call: keep original text.
        return Task.FromResult(text ?? string.Empty);
    }

    public async Task<string> ResolveNarrationTextAsync(POI poi, string targetLanguage, bool preferTtsScript = true)
    {
        var target = NormalizeLang(targetLanguage);

        // OFFLINE-FIRST: always use POI DB multilingual columns.
        var offline = preferTtsScript
            ? poi.GetTtsScriptByLanguage(target)
            : poi.GetDescriptionByLanguage(target);

        if (!string.IsNullOrWhiteSpace(offline))
            return offline;

        var fallback = preferTtsScript
            ? (!string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.TtsScript! : poi.DescriptionText)
            : poi.DescriptionText;

        Debug.WriteLine($"[TranslationService] No offline translation for '{target}', fallback to source text");
        return fallback;
    }

    public Task<bool> IsAvailableAsync()
    {
        // External online translator disabled.
        return Task.FromResult(false);
    }

    public Task<int> DownloadLanguagePackAsync(
        string languageCode,
        IProgress<LanguagePackProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public IReadOnlyList<string> GetOfflineBaseLanguages() => ["vi", "en", "zh", "ja", "ko", "fr", "ru"];

    public IReadOnlyList<string> GetOnlineLanguages() => [];

    private static string NormalizeLang(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "vi";

        var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();

        return normalized switch
        {
            "vn" or "vi" or "vi-vn" => "vi",
            "en" or "en-us" or "en-gb" => "en",
            "zh" or "zh-cn" or "zh-hans" => "zh",
            "zh-tw" or "zh-hant" => "zh",
            "ja" or "ja-jp" => "ja",
            "ko" or "ko-kr" => "ko",
            "fr" or "fr-fr" => "fr",
            "ru" or "ru-ru" => "ru",
            _ when normalized.Length > 2 && normalized.Contains('-') => normalized[..normalized.IndexOf('-')],
            _ => normalized
        };
    }
}