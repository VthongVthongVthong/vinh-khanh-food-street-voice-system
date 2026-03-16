using SQLite;

namespace VinhKhanhstreetfoods.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; } = string.Empty;  // ✅ Xóa 'required'

        [NotNull]
        public double Latitude { get; set; }

        [NotNull]
        public double Longitude { get; set; }

        public string? Address { get; set; }
        public string? Phone { get; set; }

        [NotNull]
        public string DescriptionText { get; set; } = string.Empty;  // ✅ Xóa 'required'

        public string? TtsScript { get; set; }
        public string? AudioFile { get; set; }

        [NotNull]
        public string ImageUrls { get; set; } = string.Empty;  // ✅ Xóa 'required'

        [NotNull]
        public string Language { get; set; } = "vi-VN";  // ✅ Xóa 'required'

        public string? MapLink { get; set; }

        [Column("triggerRadiusMeters")]
        public int TriggerRadius { get; set; } = 20;

        public int Priority { get; set; } = 1;
        
        public int IsActive { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int OwnerId { get; set; }

        // ===== Ignore fields (not in DB) =====
        [Ignore]
        public string? AudioUrl { get; set; }

        [Ignore]
        public DateTime LastTriggered { get; set; }

        [Ignore]
        public string NormalizedLanguageCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Language))
                    return "vi";
                var idx = Language.IndexOf('-');
                return idx > 0 ? Language[..idx] : Language;
            }
        }

        [Ignore]
        public double DistanceFromUser { get; set; }

        [Ignore]
        public List<string> ImageUrlList =>
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImageUrls ?? "[]") ?? new();
    }
}
