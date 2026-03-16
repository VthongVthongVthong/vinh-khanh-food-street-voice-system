namespace VinhKhanhstreetfoods.Services
{
    public class MapService
    {
        public string GoogleMapsApiKey { get; set; }

        public MapService(string apiKey = "YOUR_GOOGLE_MAPS_API_KEY")
        {
            GoogleMapsApiKey = apiKey;
        }

        public string GetGoogleMapsUrl(double latitude, double longitude, int zoomLevel = 15)
        {
            return $"https://www.google.com/maps/search/?api=1&query={latitude},{longitude}&zoom={zoomLevel}";
        }

        public string GetDirectionsUrl(double userLat, double userLon, double destLat, double destLon)
        {
            return $"https://www.google.com/maps/dir/?api=1&origin={userLat},{userLon}&destination={destLat},{destLon}";
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
