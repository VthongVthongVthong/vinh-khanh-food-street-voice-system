using System.Diagnostics;
using GTranslate.Translators;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Translation service using GTranslate (Google) with safe fallbacks.
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

        // Do not translate to Vietnamese (default) or same language
        if (string.Equals(tgt, "vi", StringComparison.OrdinalIgnoreCase) ||
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
            Debug.WriteLine($"[TranslationService] Translation error ({src}->{tgt}): {ex.Message}. Retrying with auto-detect...");

            // Retry once with auto-detect source
            try
            {
                var retry = await _translator.TranslateAsync(text, null, tgt);
                return retry?.Translation ?? text;
            }
            catch (Exception retryEx)
            {
                Debug.WriteLine($"[TranslationService] Retry failed: {retryEx.Message}");
                return text; // Fallback to original
            }
        }
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

    private static string NormalizeLang(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "vi";

        var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();

        return normalized switch
        {
            "vn" or "vi" or "vi-vn" => "vi",
            "en" or "en-us" or "en-gb" => "en",
            "zh" or "zh-cn" or "zh-hans" => "zh-CN",
            "zh-tw" or "zh-hant" => "zh-TW",
            "ko" or "ko-kr" => "ko",
            _ when normalized.Length > 2 => normalized,
            _ => normalized
        };
    }
}