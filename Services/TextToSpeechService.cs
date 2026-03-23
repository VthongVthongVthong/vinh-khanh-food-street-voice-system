using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Wrapper for MAUI TextToSpeech with language support
    /// </summary>
    public class TextToSpeechService
    {
        private bool _isPlaying = false;

        public async Task SpeakAsync(string text, string language = "vi-VN")
        {
            if (string.IsNullOrEmpty(text))
                return;

            _isPlaying = true;

            try
            {
                var localeCode = NormalizeToLocaleCode(language);
                var locale = await GetLocaleAsync(localeCode);
                var settings = new SpeechOptions()
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = locale
                };

                await TextToSpeech.SpeakAsync(text, settings);

                Debug.WriteLine($"[TextToSpeechService] Speaking in language: {localeCode}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TextToSpeechService] Error: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        public void Stop()
        {
            _isPlaying = false;
            // Platform-specific stop would go here
        }

        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Find a matching locale from installed voices; fall back to requested code
        /// </summary>
        private static async Task<Locale?> GetLocaleAsync(string languageCode)
        {
            try
            {
                var locales = await TextToSpeech.GetLocalesAsync();
                var normalized = languageCode.ToLowerInvariant();
                var langPrefix = normalized.Split('-')[0];

                // Exact match first (e.g., en-us)
                var exact = locales?.FirstOrDefault(l => l.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                    return exact;

                // Match by language prefix (e.g., en)
                var prefixMatch = locales?.FirstOrDefault(l => l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
                if (prefixMatch is not null)
                    return prefixMatch;

                // Fallback: use any available locale
                return locales?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeToLocaleCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi-VN";

            var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();

            return normalized switch
            {
                "vn" or "vi" or "vi-vn" => "vi-VN",
                "en" or "en-us" => "en-US",
                "en-gb" => "en-GB",
                "zh" or "zh-cn" or "zh-hans" => "zh-CN",
                "zh-tw" or "zh-hant" => "zh-TW",
                "ko" or "ko-kr" => "ko-KR",
                _ when normalized.Length == 2 => $"{normalized}-{normalized.ToUpperInvariant()}",
                _ => normalized
            };
        }
    }
}
