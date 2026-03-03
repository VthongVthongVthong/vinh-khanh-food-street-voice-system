namespace VinhKhanhFoodGuide.Models;

public class GeofenceEvent
{
    public int PoiId { get; set; }
    public string PoiName { get; set; }
    public DateTime TriggerTime { get; set; }
    public double Distance { get; set; } // meters
}
