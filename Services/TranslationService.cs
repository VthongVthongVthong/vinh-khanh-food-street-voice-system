using System.Diagnostics;
using GTranslate.Translators;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Legacy translation service.
/// Kept for compatibility; HybridTranslationService is preferred.
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly GoogleTranslator _translator = new();

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var src = NormalizeLang(sourceLanguage);
        var tgt = NormalizeLang(targetLanguage);

        if (string.IsNullOrWhiteSpace(tgt) ||
            string.Equals(tgt, "vi", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(src, tgt, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        try
        {
            var result = await _translator.TranslateAsync(text, src, tgt);
            return result?.Translation ?? text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TranslationService] Translation error ({src}->{tgt}): {ex.Message}");
            return text;
        }
    }

    public async Task<string> ResolveNarrationTextAsync(POI poi, string targetLanguage, bool preferTtsScript = true)
    {
        var target = NormalizeLang(targetLanguage);

        // OFFLINE-FIRST: always try POI DB multilingual columns before API
        var offline = preferTtsScript
            ? poi.GetTtsScriptByLanguage(target)
            : poi.GetDescriptionByLanguage(target);

        if (!string.IsNullOrWhiteSpace(offline))
            return offline;

        var original = preferTtsScript
            ? (!string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.TtsScript! : poi.DescriptionText)
            : poi.DescriptionText;

        var source = NormalizeLang(poi.TtsLanguage);

        if (target == "vi" || target == source)
            return original;

        return await TranslateAsync(original, source, target);
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var result = await _translator.TranslateAsync("ping", "en", "vi");
            return !string.IsNullOrWhiteSpace(result?.Translation);
        }
        catch
        {
            return false;
        }
    }

    public Task<int> DownloadLanguagePackAsync(
        string languageCode,
        IProgress<LanguagePackProgress>? progress = null,
    CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public IReadOnlyList<string> GetOfflineBaseLanguages() => ["vi"];

    public IReadOnlyList<string> GetOnlineLanguages() => ["en", "zh", "ja", "ko", "fr", "ru"];

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