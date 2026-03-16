namespace VinhKhanhstreetfoods.Models
{
    public class UserLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public double? Speed { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
