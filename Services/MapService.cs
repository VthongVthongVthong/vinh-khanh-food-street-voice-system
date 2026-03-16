namespace VinhKhanhstreetfoods.Services
{
    public class MapService
    {
        public string TrackAsiaStyleUrl { get; set; }

        public MapService(string apiKey = "bca01773651908dcc9bc6320f7c16973ce")
        {
            // Track Asia Style URL - sử dụng style Streets v2
            TrackAsiaStyleUrl = $"https://maps.track-asia.com/styles/v2/streets.json?key={apiKey}";
        }

        public string GetMapUrl(double latitude, double longitude, int zoomLevel = 15)
        {
            // Trả về URL để sử dụng Track Asia với vị trí được chỉ định
            return $"https://maps.track-asia.com/?lat={latitude}&lng={longitude}&zoom={zoomLevel}";
        }

        public string GetDirectionsUrl(double userLat, double userLon, double destLat, double destLon)
        {
            // Track Asia directions URL
            return $"https://maps.track-asia.com/?origin={userLat},{userLon}&destination={destLat},{destLon}";
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula - returns distance in kilometers
            const double R = 6371; // Earth radius in km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
