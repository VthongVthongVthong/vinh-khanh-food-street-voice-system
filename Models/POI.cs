using SQLite;

namespace VinhKhanhstreetfoods.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey, AutoIncrement]
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
        /// Default TTS script (usually in Vietnamese)
        /// </summary>
        public string? TtsScript { get; set; }

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
    }
}
