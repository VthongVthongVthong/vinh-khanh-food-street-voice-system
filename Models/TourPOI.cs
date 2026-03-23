using SQLite;

namespace VinhKhanhstreetfoods.Models;

[Table("TourPOI")]
public class TourPOI
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int TourId { get; set; }

    public int POIId { get; set; }

    public int SortOrder { get; set; } = 0;
}
