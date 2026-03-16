namespace VinhKhanhstreetfoods.Components.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Category { get; set; }
        public decimal Rating { get; set; }
        public string ImageUrl { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public List<RestaurantImage> Images { get; set; } = new();
        public Dictionary<string, string> NamesByLanguage { get; set; } = new();
        public Dictionary<string, string> DescriptionsByLanguage { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
