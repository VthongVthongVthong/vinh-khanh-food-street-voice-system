using SQLite;

namespace VinhKhanhstreetfoods.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; } = string.Empty;

        [NotNull]
        public double Latitude { get; set; }

        [NotNull]
        public double Longitude { get; set; }

        public string? Address { get; set; }
        public string? Phone { get; set; }

        [NotNull]
        public string DescriptionText { get; set; } = string.Empty;

        /// <summary>
        /// Offline English description (hybrid translation mode)
        /// </summary>
        public string? DescriptionEn { get; set; }

        /// <summary>
        /// Offline Simplified Chinese description (hybrid translation mode)
        /// </summary>
        public string? DescriptionZh { get; set; }

        /// <summary>
        /// Offline Japanese description
        /// </summary>
        public string? DescriptionJa { get; set; }

        /// <summary>
        /// Offline Korean description
        /// </summary>
        public string? DescriptionKo { get; set; }

        /// <summary>
        /// Offline French description
        /// </summary>
        public string? DescriptionFr { get; set; }

        /// <summary>
        /// Offline Russian description
        /// </summary>
        public string? DescriptionRu { get; set; }

        /// <summary>
        /// Default TTS script (usually in Vietnamese)
        /// </summary>
        public string? TtsScript { get; set; }

        /// <summary>
        /// Offline English TTS script (hybrid translation mode)
        /// </summary>
        public string? TtsScriptEn { get; set; }

        /// <summary>
        /// Offline Simplified Chinese TTS script (hybrid translation mode)
        /// </summary>
        public string? TtsScriptZh { get; set; }

        /// <summary>
        /// Offline Japanese TTS script
        /// </summary>
        public string? TtsScriptJa { get; set; }

        /// <summary>
        /// Offline Korean TTS script
        /// </summary>
        public string? TtsScriptKo { get; set; }

        /// <summary>
        /// Offline French TTS script
        /// </summary>
        public string? TtsScriptFr { get; set; }

        /// <summary>
        /// Offline Russian TTS script
        /// </summary>
        public string? TtsScriptRu { get; set; }

        /// <summary>
        /// POI's default language for TTS (e.g., "vi-VN", "en-US")
        /// Will be translated if user selects different language
        /// </summary>
        [NotNull]
        public string TtsLanguage { get; set; } = "vi";

        public string? AudioFile { get; set; }

        [NotNull]
        public string ImageUrls { get; set; } = string.Empty;

        public string? MapLink { get; set; }

        [Column("triggerRadiusMeters")]
        public int TriggerRadius { get; set; } = 20;

        public int Priority { get; set; } = 1;

        public int IsActive { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int OwnerId { get; set; }

        // ===== Runtime fields (not in DB) =====
        [Ignore]
        public string? AudioUrl { get; set; }

        [Ignore]
        public DateTime LastTriggered { get; set; }

        [Ignore]
        public double DistanceFromUser { get; set; }

        [Ignore]
        public List<string> ImageUrlList =>
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImageUrls ?? "[]") ?? new();

        /// <summary>
        /// Cached translated TTS script (in-memory, 5 min TTL)
        /// </summary>
        [Ignore]
        public string? CachedTranslatedTtsScript { get; set; }

        [Ignore]
        public DateTime CachedTranslationTime { get; set; }

        /// <summary>
        /// Avatar image URL (loaded from POIImages table with type='avatar')
        /// </summary>
        [Ignore]
        public string? AvatarImageUrl { get; set; }

        /// <summary>
        /// Banner image URL (loaded from POIImages table with type='banner')
        /// </summary>
        [Ignore]
        public string? BannerImageUrl { get; set; }

        // ===== MULTILINGUAL HELPERS =====
        /// <summary>
        /// Get description in specific language from offline DB columns
        /// Priority: ExactLanguage > Fallback to Vietnamese
        /// </summary>
        public string GetDescriptionByLanguage(string languageCode)
        {
            var normalized = NormalizeLang(languageCode);

            return normalized switch
            {
                "en" => DescriptionEn ?? DescriptionText,      // English
                "zh" => DescriptionZh ?? DescriptionText,      // Simplified Chinese
                "ja" => DescriptionJa ?? DescriptionText,        // Japanese (not in DB yet, fallback to VI)
                "ko" => DescriptionKo ?? DescriptionText,  // Korean (not in DB yet, fallback to VI)
                "fr" => DescriptionFr ?? DescriptionText,                // French (not in DB yet, fallback to VI)
                "ru" => DescriptionRu ?? DescriptionText,         // Russian (not in DB yet, fallback to VI)
                _ => DescriptionText   // Default: Vietnamese
            };
        }

        /// <summary>
        /// Get TTS script in specific language from offline DB columns
        /// Priority: ExactLanguage > Fallback to Vietnamese TtsScript
        /// </summary>
        public string GetTtsScriptByLanguage(string languageCode)
        {
            var normalized = NormalizeLang(languageCode);

            return normalized switch
            {
                "en" => TtsScriptEn ?? DescriptionEn ?? TtsScript ?? DescriptionText,
                "zh" => TtsScriptZh ?? DescriptionZh ?? TtsScript ?? DescriptionText,
                "ja" => TtsScriptJa ?? DescriptionJa ?? TtsScript ?? DescriptionText,
                "ko" => TtsScriptKo ?? DescriptionKo ?? TtsScript ?? DescriptionText,
                "fr" => TtsScriptFr ?? DescriptionFr ?? TtsScript ?? DescriptionText,
                "ru" => TtsScriptRu ?? DescriptionRu ?? TtsScript ?? DescriptionText,
                _ => TtsScript ?? DescriptionText       // Default: Vietnamese
            };
        }

        /// <summary>
        /// Normalize language code: "en-US" → "en", "vi-VN" → "vi"
        /// </summary>
        private static string NormalizeLang(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi";
            var trimmed = code.Trim().ToLowerInvariant();
            var dashIndex = trimmed.IndexOf('-');
            return dashIndex > 0 ? trimmed[..dashIndex] : trimmed;
        }
    }
}
