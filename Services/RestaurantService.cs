using System.Diagnostics;
using System.Linq;
using VinhKhanhstreetfoods.Components.Models;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class RestaurantService
    {
        private readonly IPOIRepository _poiRepository;
        private List<Restaurant>? _restaurantCache;
        private DateTime _cacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        private static readonly List<Language> _languages = new()
        {
            new Language("vi", "Vietnamese", "Tiếng Việt", "🇻🇳"),
            new Language("en", "English", "English", "🇬🇧"),
            new Language("zh", "Chinese", "中文", "🇨🇳"),
            new Language("ko", "Korean", "한국어", "🇰🇷")
        };

        public RestaurantService(IPOIRepository poiRepository)
        {
            _poiRepository = poiRepository;
        }

        /// <summary>
        /// Gets all restaurants/POIs with 5-minute caching
        /// </summary>
        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            // Check cache validity
            if (_restaurantCache is not null && DateTime.UtcNow - _cacheTime < _cacheDuration)
            {
                Debug.WriteLine($"[RestaurantService] Returning cached restaurants: {_restaurantCache.Count}");
                return _restaurantCache;
            }

            Debug.WriteLine("[RestaurantService] Fetching restaurants from repository...");
            var pois = await _poiRepository.GetActivePOIsAsync();
            _restaurantCache = pois.Select(POIToRestaurant).ToList();
            _cacheTime = DateTime.UtcNow;

            Debug.WriteLine($"[RestaurantService] Loaded {_restaurantCache.Count} restaurants");
            return _restaurantCache;
        }

        public Task<List<Language>> GetLanguagesAsync() => Task.FromResult(_languages);

        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            var poi = await _poiRepository.GetPOIByIdAsync(id);
            return poi is null ? null : POIToRestaurant(poi);
        }

        public async Task<List<Restaurant>> GetNearbyRestaurantsAsync(double latitude, double longitude, double radiusKm = 1.0)
        {
            var restaurants = await GetRestaurantsAsync();

            return restaurants
                .Where(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
                .OrderBy(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude))
                .ToList();
        }

        public async Task<List<Restaurant>> SearchRestaurantsAsync(string query)
        {
            var restaurants = await GetRestaurantsAsync();

            if (string.IsNullOrWhiteSpace(query))
                return restaurants;

            return restaurants
                .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            r.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<List<Restaurant>> GetRestaurantsByCategoryAsync(string category)
        {
            var restaurants = await GetRestaurantsAsync();

            return restaurants
                .Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public string GetRestaurantNameByLanguage(Restaurant restaurant, string languageCode)
            => restaurant.NamesByLanguage.TryGetValue(languageCode, out var name) ? name : restaurant.Name;

        public string GetRestaurantDescriptionByLanguage(Restaurant restaurant, string languageCode)
            => restaurant.DescriptionsByLanguage.TryGetValue(languageCode, out var description) ? description : restaurant.Description;

        /// <summary>
        /// Converts POI to Restaurant (extracted to avoid code duplication)
        /// </summary>
        private static Restaurant POIToRestaurant(VinhKhanhstreetfoods.Models.POI poi)
        {
            var escapedName = Uri.EscapeDataString(poi.Name);

            return new Restaurant
            {
                Id = poi.Id,
                Name = poi.Name,
                Description = poi.DescriptionText,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                Category = "Restaurant",
                Rating = 4.5m,
                ImageUrl = poi.ImageUrls,
                Address = poi.Address ?? "Vĩnh Khánh Street, District 4, HCMC",
                Images = new List<RestaurantImage>
                {
                    new RestaurantImage
                    {
                        Id = 1,
                        RestaurantId = poi.Id,
                        ImageType = "banner",
                        ImageUrl = $"https://via.placeholder.com/500x300?text={escapedName}",
                        DisplayOrder = 0,
                        Caption = $"{poi.Name} - Banner"
                    },
                    new RestaurantImage
                    {
                        Id = 2,
                        RestaurantId = poi.Id,
                        ImageType = "gallery",
                        ImageUrl = "https://via.placeholder.com/300x300?text=Gallery",
                        DisplayOrder = 1,
                        Caption = "Interior view"
                    }
                },
                IsActive = poi.IsActive == 1,
                NamesByLanguage = new Dictionary<string, string>
                {
                    { "vi", poi.Name },
                    { "en", poi.Name }
                },
                DescriptionsByLanguage = new Dictionary<string, string>
                {
                    { "vi", poi.DescriptionText },
                    { "en", poi.DescriptionText }
                }
            };
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
