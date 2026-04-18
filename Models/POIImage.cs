using SQLite;

namespace VinhKhanhstreetfoods.Models
{
    [Table("POIImage")]
    public class POIImage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

   [NotNull]
        public int POIId { get; set; }

        [NotNull]
        public string ImageUrl { get; set; } = string.Empty;

        [NotNull]
        [Column("imageType")]
        public string Type { get; set; } = string.Empty; // "avatar", "gallery", "banner", etc.

        public int DisplayOrder { get; set; } = 0;

   public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
