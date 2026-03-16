namespace VinhKhanhstreetfoods.Components.Models
{
    public class RestaurantImage
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string ImageUrl { get; set; }
        public string ImageType { get; set; } // banner, gallery, menu, etc.
        public int DisplayOrder { get; set; }
        public string Caption { get; set; }
    }
}
