using VinhKhanhstreetfoods.Components.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class RestaurantService
    {
        private readonly POIRepository _poiRepository;
        private static readonly List<Language> _languages = new()
        {
            new Language("vi", "Vietnamese", "Tiếng Việt", "🇻🇳"),
            new Language("en", "English", "English", "🇬🇧"),
            new Language("zh", "Chinese", "中文", "🇨🇳"),
            new Language("ko", "Korean", "한국어", "🇰🇷")
        };

        public RestaurantService(POIRepository poiRepository)
        {
            _poiRepository = poiRepository;
        }

        /// <summary>
        /// Gets all restaurants/POIs
        /// </summary>
        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            var pois = await _poiRepository.GetAllPOIsAsync();
            
            return pois.Select((poi, index) => new Restaurant
            {
                Id = poi.Id,
                Name = poi.Name,
                Description = poi.DescriptionText,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                Category = "Restaurant",
                Rating = 4.5m,
                ImageUrl = poi.ImageUrls,
                Address = $"Vĩnh Khánh Street, District 1, HCMC",
                Images = new List<RestaurantImage>
                {
                    new RestaurantImage
                    {
                        Id = 1,
                        RestaurantId = poi.Id,
                        ImageType = "banner",
                        ImageUrl = $"https://via.placeholder.com/500x300?text={Uri.EscapeDataString(poi.Name)}",
                        DisplayOrder = 0,
                        Caption = $"{poi.Name} - Banner"
                    },
                    new RestaurantImage
                    {
                        Id = 2,
                        RestaurantId = poi.Id,
                        ImageType = "gallery",
                        ImageUrl = $"https://via.placeholder.com/300x300?text=Gallery",
                        DisplayOrder = 1,
                        Caption = "Interior view"
                    }
                },
                IsActive = poi.IsActive,
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
            }).ToList();
        }

        /// <summary>
        /// Gets available languages for the app
        /// </summary>
        public async Task<List<Language>> GetLanguagesAsync()
        {
            // Return predefined list of supported languages
            return await Task.FromResult(_languages);
        }

        /// <summary>
        /// Gets a single restaurant by ID
        /// </summary>
        public async Task<Restaurant> GetRestaurantByIdAsync(int id)
        {
            var poi = await _poiRepository.GetPOIByIdAsync(id);
            
            if (poi == null)
                return null;

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
                Address = $"Vĩnh Khánh Street, District 1, HCMC",
                Images = new List<RestaurantImage>
                {
                    new RestaurantImage
                    {
                        Id = 1,
                        RestaurantId = poi.Id,
                        ImageType = "banner",
                        ImageUrl = $"https://via.placeholder.com/500x300?text={Uri.EscapeDataString(poi.Name)}",
                        DisplayOrder = 0,
                        Caption = $"{poi.Name} - Banner"
                    },
                    new RestaurantImage
                    {
                        Id = 2,
                        RestaurantId = poi.Id,
                        ImageType = "gallery",
                        ImageUrl = $"https://via.placeholder.com/300x300?text=Gallery",
                        DisplayOrder = 1,
                        Caption = "Interior view"
                    }
                },
                IsActive = poi.IsActive,
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

        /// <summary>
        /// Gets restaurants near a location
        /// </summary>
        public async Task<List<Restaurant>> GetNearbyRestaurantsAsync(double latitude, double longitude, double radiusKm = 1.0)
        {
            var restaurants = await GetRestaurantsAsync();
            
            return restaurants
                .Where(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
                .OrderBy(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude))
                .ToList();
        }

        /// <summary>
        /// Searches restaurants by name or description
        /// </summary>
        public async Task<List<Restaurant>> SearchRestaurantsAsync(string query)
        {
            var restaurants = await GetRestaurantsAsync();
            
            if (string.IsNullOrWhiteSpace(query))
                return restaurants;

            var searchQuery = query.ToLower();
            return restaurants
                .Where(r => r.Name.ToLower().Contains(searchQuery) || 
                           r.Description.ToLower().Contains(searchQuery))
                .ToList();
        }

        /// <summary>
        /// Filters restaurants by category
        /// </summary>
        public async Task<List<Restaurant>> GetRestaurantsByCategoryAsync(string category)
        {
            var restaurants = await GetRestaurantsAsync();
            
            return restaurants
                .Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets the translated name for a restaurant
        /// </summary>
        public string GetRestaurantNameByLanguage(Restaurant restaurant, string languageCode)
        {
            if (restaurant.NamesByLanguage.TryGetValue(languageCode, out var name))
                return name;
            
            return restaurant.Name;
        }

        /// <summary>
        /// Gets the translated description for a restaurant
        /// </summary>
        public string GetRestaurantDescriptionByLanguage(Restaurant restaurant, string languageCode)
        {
            if (restaurant.DescriptionsByLanguage.TryGetValue(languageCode, out var description))
                return description;
            
            return restaurant.Description;
        }

        /// <summary>
        /// Calculate distance between two coordinates in kilometers (Haversine formula)
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
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
