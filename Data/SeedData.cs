using System.Text.Json;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(POIRepository repository)
        {
            if (await repository.HasAnyPOIAsync())
                return;

            var pois = new List<POI>
            {
                new POI
                {
                    Name = "Bún Mắm Vĩnh Khánh",
                    Latitude = 10.757123,
                    Longitude = 106.705321,
                    TriggerRadius = 20,
                    Priority = 1,
                    DescriptionText = "Nhà hàng bún mắm nổi tiếng với nước lèo đậm đà từ mắm cá linh, thịt heo, tôm và mực tươi.",
                    TtsScript = "Chào mừng đến với Bún Mắm Vĩnh Khánh. Đây là quán bún mắm nổi tiếng nhất đường Vĩnh Khánh.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "bunmam1.jpg", "bunmam2.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example1",
                    IsActive = true
                },
                new POI
                {
                    Name = "Ốc Nóng Vĩnh Khánh",
                    Latitude = 10.757225,
                    Longitude = 106.705450,
                    TriggerRadius = 20,
                    Priority = 2,
                    DescriptionText = "Quán ốc nổi tiếng với các món ốc hấp, ốc xào me, ốc nướng tiêu xanh.",
                    TtsScript = "Đây là quán Ốc Nóng Vĩnh Khánh, nổi tiếng với các món ốc hấp sả và ốc xào me chua ngọt.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "ocnong1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example2",
                    IsActive = true
                },
                new POI
                {
                    Name = "Lẩu Cá Kèo Vĩnh Khánh",
                    Latitude = 10.757350,
                    Longitude = 106.705600,
                    TriggerRadius = 20,
                    Priority = 3,
                    DescriptionText = "Đặc sản lẩu cá kèo nấu với rau đắng, bông súng, ăn kèm bún tươi.",
                    TtsScript = "Chào mừng đến với quán Lẩu Cá Kèo. Đặc sản ở đây là lẩu cá kèo nấu với rau đắng.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "laucakeo1.jpg", "laucakeo2.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example3",
                    IsActive = true
                },
                new POI
                {
                    Name = "Bánh Mì Nóng Vĩnh Khánh",
                    Latitude = 10.757450,
                    Longitude = 106.705750,
                    TriggerRadius = 20,
                    Priority = 4,
                    DescriptionText = "Bánh mì nóng giòn với nhân pâté, chả cốt dừa, rau sống tươi.",
                    TtsScript = "Quán Bánh Mì Nóng Vĩnh Khánh phục vụ bánh mì giòn ngon với nhân đa dạng.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "banhmi1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example4",
                    IsActive = true
                },
                new POI
                {
                    Name = "Cơm Tấm Sườn Nướng",
                    Latitude = 10.757550,
                    Longitude = 106.705850,
                    TriggerRadius = 20,
                    Priority = 5,
                    DescriptionText = "Cơm tấm với sườn nướng, trứng ốp la, cá chiên giòn rụm.",
                    TtsScript = "Đây là quán Cơm Tấm Sườn Nướng, phục vụ cơm tấm cơm cháy thơm ngon.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "comtam1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example5",
                    IsActive = true
                },
                new POI
                {
                    Name = "Hủ Tiếu - Bánh Canh Tôm Cua",
                    Latitude = 10.757650,
                    Longitude = 106.705950,
                    TriggerRadius = 20,
                    Priority = 6,
                    DescriptionText = "Hủ tiếu nước trong, bánh canh tôm cua với nước lèo đậm hương.",
                    TtsScript = "Quán Hủ Tiếu Bánh Canh nổi tiếng với hủ tiếu nước trong và bánh canh tôm cua.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "hutieu1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example6",
                    IsActive = true
                },
                new POI
                {
                    Name = "Chế Biến Cá Linh Ngon",
                    Latitude = 10.757750,
                    Longitude = 106.706050,
                    TriggerRadius = 20,
                    Priority = 7,
                    DescriptionText = "Cá linh chiên giòn, cá linh nướng mắm, cá linh hấp dưa cài.",
                    TtsScript = "Đây là quán chế biến cá linh, cá linh chiên và nướng rất ngon.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "calinh1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example7",
                    IsActive = true
                },
                new POI
                {
                    Name = "Nước Lều Mắm Cá Chua Cộng",
                    Latitude = 10.757850,
                    Longitude = 106.706150,
                    TriggerRadius = 20,
                    Priority = 8,
                    DescriptionText = "Nước lều mắm cá với thịt, tôm, mực, tàu hủ và rau sống.",
                    TtsScript = "Quán Nước Lều Mắm Cá Chua Cộng phục vụ nước lều đậm đà với đủ loại topping.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "nuocleumam1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example8",
                    IsActive = true
                },
                new POI
                {
                    Name = "Canh Cua - Canh Tôm Chua Cay",
                    Latitude = 10.757950,
                    Longitude = 106.706250,
                    TriggerRadius = 20,
                    Priority = 9,
                    DescriptionText = "Canh cua chua cay, canh tôm chua cay với mùi chua thanh.",
                    TtsScript = "Quán Canh Cua nổi tiếng với canh cua và tôm chua cay hấp dẫn.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "canhcua1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example9",
                    IsActive = true
                },
                new POI
                {
                    Name = "Chả Ốc Nước",
                    Latitude = 10.758050,
                    Longitude = 106.706350,
                    TriggerRadius = 20,
                    Priority = 10,
                    DescriptionText = "Chả ốc nước với sốt chua cay, vị đậm nước ốc tươi.",
                    TtsScript = "Đây là quán Chả Ốc Nước, phục vụ chả ốc tươi ngon với sốt đặc biệt.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "chaoc1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example10",
                    IsActive = true
                },
                new POI
                {
                    Name = "Bọ Dừa Nướng",
                    Latitude = 10.758150,
                    Longitude = 106.706450,
                    TriggerRadius = 20,
                    Priority = 11,
                    DescriptionText = "Bọ dừa nướng, bọ dừa hấp với tương ớt, dầu ăn.",
                    TtsScript = "Quán Bọ Dừa nổi tiếng với bọ dừa nướng thơm lừng.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "boduong1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example11",
                    IsActive = true
                },
                new POI
                {
                    Name = "Tôm Chiên Giòn",
                    Latitude = 10.758250,
                    Longitude = 106.706550,
                    TriggerRadius = 20,
                    Priority = 12,
                    DescriptionText = "Tôm chiên giòn vàng ươm, tôm nướng tiêu xanh.",
                    TtsScript = "Đây là quán Tôm Chiên, tôm chiên giòn rụm thơm ngon.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "tomchien1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example12",
                    IsActive = true
                },
                new POI
                {
                    Name = "Mực Chiên Tằm",
                    Latitude = 10.758350,
                    Longitude = 106.706650,
                    TriggerRadius = 20,
                    Priority = 13,
                    DescriptionText = "Mực chiên tằm, mực hấp xả, mực nướng ớt.",
                    TtsScript = "Quán Mực Chiên nrophục vụ mực chiên giòn với các cách chế biến đa dạng.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "mucchien1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example13",
                    IsActive = true
                },
                new POI
                {
                    Name = "Cơm Lươn Thúng",
                    Latitude = 10.758450,
                    Longitude = 106.706750,
                    TriggerRadius = 20,
                    Priority = 14,
                    DescriptionText = "Cơm lươn thúng, lươn nấu cơm, lươn chiên giòn.",
                    TtsScript = "Đây là quán Cơm Lươn, phục vụ cơm lươn thúng hương vị đặc sắc.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "comluon1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example14",
                    IsActive = true
                },
                new POI
                {
                    Name = "Nộm Cá Trích",
                    Latitude = 10.758550,
                    Longitude = 106.706850,
                    TriggerRadius = 20,
                    Priority = 15,
                    DescriptionText = "Nộm cá trích, gỏi cá trích tươi ngon với rau thơm.",
                    TtsScript = "Quán Nộm Cá Trích nổi tiếng với các món nộm cá tươi.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "nomcatrich1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example15",
                    IsActive = true
                },
                new POI
                {
                    Name = "Thốt Nốt Vĩnh Khánh",
                    Latitude = 10.758650,
                    Longitude = 106.706950,
                    TriggerRadius = 20,
                    Priority = 16,
                    DescriptionText = "Thốt nốt - món ăn đặc trưng của miền Tây với tôm, cá cảnh.",
                    TtsScript = "Đây là quán Thốt Nốt, phục vụ thốt nốt đặc trưng miền Tây.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "thotnot1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example16",
                    IsActive = true
                },
                new POI
                {
                    Name = "Bánh Hỏi - Nem Cua Bé",
                    Latitude = 10.758750,
                    Longitude = 106.707050,
                    TriggerRadius = 20,
                    Priority = 17,
                    DescriptionText = "Bánh hỏi tươi, nem cua bé, bánh chưng ngon.",
                    TtsScript = "Quán Bánh Hỏi phục vụ bánh hỏi tươi mỗi ngày với nem cua bé.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "banhappoi1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example17",
                    IsActive = true
                },
                new POI
                {
                    Name = "Trà Chanh Và Đồ Ăn Vặt",
                    Latitude = 10.758850,
                    Longitude = 106.707150,
                    TriggerRadius = 20,
                    Priority = 18,
                    DescriptionText = "Trà chanh tươi, nước mía ép, bánh ngoại cứng, bánh mứt.",
                    TtsScript = "Đây là quán Trà Chanh, phục vụ trà chanh tươi và các đồ ăn vặt.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "trachanh1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example18",
                    IsActive = true
                },
                new POI
                {
                    Name = "Quần Vịt Quay Vĩnh Khánh",
                    Latitude = 10.758950,
                    Longitude = 106.707250,
                    TriggerRadius = 20,
                    Priority = 19,
                    DescriptionText = "Vịt quay giòn da, vịt nấu canh, vịt kho gừng.",
                    TtsScript = "Quán Vịt Quay Vĩnh Khánh nổi tiếng với vịt quay giòn da ơi.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "vitquay1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example19",
                    IsActive = true
                },
                new POI
                {
                    Name = "Chợ Nội Vĩnh Khánh - Trung Tâm Ẩm Thực",
                    Latitude = 10.759050,
                    Longitude = 106.707350,
                    TriggerRadius = 20,
                    Priority = 20,
                    DescriptionText = "Chợ nội là trung tâm ẩm thực với hàng chục quán ăn nhỏ.",
                    TtsScript = "Chào mừng đến với Chợ Nội Vĩnh Khánh, trung tâm ẩm thực sôi động.",
                    Language = "vi-VN",
                    ImageUrls = JsonSerializer.Serialize(new List<string> { "chopnoi1.jpg" }),
                    MapLink = "https://maps.app.goo.gl/example20",
                    IsActive = true
                }
            };

            await repository.AddPOIsAsync(pois);
        }
    }
}
