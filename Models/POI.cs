using SQLite;

namespace VinhKhanhstreetfoods.Models
{
    [Table("POIs")]
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public double Latitude { get; set; }

        [NotNull]
        public double Longitude { get; set; }

        public double TriggerRadius { get; set; } = 20; // meters
        public int Priority { get; set; } = 1;

        [NotNull]
        public string DescriptionText { get; set; }

        public string TtsScript { get; set; }

        // local file path (offline)
        public string AudioFile { get; set; }

        [NotNull]
        public string ImageUrls { get; set; } // JSON array of URLs

        // currently stored like: vi-VN
        [NotNull]
        public string Language { get; set; } = "vi-VN";

        public string MapLink { get; set; }
        public DateTime LastTriggered { get; set; }
        public bool IsActive { get; set; } = true;

        // ===== server-ready fields (not stored in SQLite) =====
        [Ignore]
        public string AudioUrl { get; set; }

        [Ignore]
        public string NormalizedLanguageCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Language))
                    return "vi";

                // vi-VN -> vi, en-US -> en
                var idx = Language.IndexOf('-');
                return idx > 0 ? Language[..idx] : Language;
            }
        }

        // Helper properties (not stored in DB)
        [Ignore]
        public double DistanceFromUser { get; set; }

        [Ignore]
        public List<string> ImageUrlList =>
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImageUrls ?? "[]") ?? new();
    }
}
