using SQLite;

namespace VinhKhanhFoodGuide.Models;

[Table("POI")]
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

    [NotNull]
    public double Radius { get; set; } // in meters

    [NotNull]
    public int Priority { get; set; } // Higher = more important

    [NotNull]
    public int CooldownMinutes { get; set; } // Minimum time between triggers

    public string ImagePath { get; set; } // Local image path

    public string Category { get; set; } // e.g., "Restaurant", "Dessert", "Drink"
}
