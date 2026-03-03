using SQLite;
using VinhKhanhFoodGuide.Models;

namespace VinhKhanhFoodGuide.Data;

public class PoiRepository : IPoiRepository
{
    private readonly SQLiteAsyncConnection _database;
    private const string DbName = "VinhKhanhFoodGuide.db";
    private string DbPath => Path.Combine(FileSystem.AppDataDirectory, DbName);

    public PoiRepository()
    {
        _database = new SQLiteAsyncConnection(DbPath);
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            if (!File.Exists(DbPath))
            {
                // Create tables
                await _database.CreateTableAsync<POI>();
                await _database.CreateTableAsync<POIContent>();

                // Seed demo data
                await SeedDemoDataAsync();
                Debug.WriteLine("Database initialized and seeded with demo data");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<POI>> GetAllPoisAsync()
    {
        return await _database.Table<POI>().ToListAsync();
    }

    public async Task<POI> GetPoiByIdAsync(int id)
    {
        return await _database.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<POIContent>> GetPoiContentAsync(int poiId)
    {
        return await _database.Table<POIContent>()
            .Where(pc => pc.PoiId == poiId)
            .ToListAsync();
    }

    public async Task<POIContent> GetPoiContentByLanguageAsync(int poiId, string languageCode)
    {
        return await _database.Table<POIContent>()
            .Where(pc => pc.PoiId == poiId && pc.LanguageCode == languageCode)
            .FirstOrDefaultAsync();
    }

    public async Task InsertPoiAsync(POI poi)
    {
        await _database.InsertAsync(poi);
    }

    public async Task InsertPoiContentAsync(POIContent content)
    {
        await _database.InsertAsync(content);
    }

    public async Task UpdatePoiAsync(POI poi)
    {
        await _database.UpdateAsync(poi);
    }

    public async Task DeletePoiAsync(int id)
    {
        await _database.DeleteAsync<POI>(id);
    }

    private async Task SeedDemoDataAsync()
    {
        try
        {
            // Sample POIs for Vinh Khanh Food Street
            var pois = new List<POI>
            {
                new POI
                {
                    Name = "Bánh Mì Tươi",
                    Latitude = 10.77695,
                    Longitude = 106.67895,
                    Radius = 50, // 50 meters
                    Priority = 5,
                    CooldownMinutes = 30,
                    Category = "Bread",
                    ImagePath = "banh_mi.jpg"
                },
                new POI
                {
                    Name = "Cơm Tấm Sài Gòn",
                    Latitude = 10.77705,
                    Longitude = 106.67915,
                    Radius = 40,
                    Priority = 4,
                    CooldownMinutes = 30,
                    Category = "Rice",
                    ImagePath = "com_tam.jpg"
                },
                new POI
                {
                    Name = "Phở Hương Liệu",
                    Latitude = 10.77715,
                    Longitude = 106.67835,
                    Radius = 45,
                    Priority = 5,
                    CooldownMinutes = 25,
                    Category = "Noodles",
                    ImagePath = "pho.jpg"
                },
                new POI
                {
                    Name = "Kem Tươi Tây Ninh",
                    Latitude = 10.77685,
                    Longitude = 106.67955,
                    Radius = 35,
                    Priority = 3,
                    CooldownMinutes = 20,
                    Category = "Dessert",
                    ImagePath = "kem.jpg"
                },
                new POI
                {
                    Name = "Nước Mía Minh Châu",
                    Latitude = 10.77675,
                    Longitude = 106.67875,
                    Radius = 30,
                    Priority = 2,
                    CooldownMinutes = 15,
                    Category = "Drink",
                    ImagePath = "nuoc_mia.jpg"
                }
            };

            // Insert POIs
            foreach (var poi in pois)
            {
                await _database.InsertAsync(poi);
            }

            // Seed POI content (Vietnamese and English)
            var contents = new List<POIContent>
            {
                // Bánh Mì Tươi - Vietnamese
                new POIContent
                {
                    PoiId = 1,
                    LanguageCode = "vi",
                    TextContent = "Bánh mì tươi được làm từ bột lúa mì cao cấp, nướng tươi mỗi ngày. Dùng ăn kèm với pâté, chả lua, dưa chuột và rau tây. Đây là một trong những bánh mì ngon nhất tại Sài Gòn.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Bánh Mì Tươi - English
                new POIContent
                {
                    PoiId = 1,
                    LanguageCode = "en",
                    TextContent = "Fresh Vietnamese banh mi made with premium wheat flour, baked daily. Served with pâté, cold cuts, pickled cucumber and vegetables. One of the most delicious banh mi in Saigon.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Cơm Tấm Sài Gòn - Vietnamese
                new POIContent
                {
                    PoiId = 2,
                    LanguageCode = "vi",
                    TextContent = "Cơm tấm là một đặc sản của Sài Gòn. Cơm tấm nơi đây được đồng hay ăn kèm với thịt nướng, trứng, tôm và rau sống. Hương vị đặc trưng không thể quên.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Cơm Tấm Sài Gòn - English
                new POIContent
                {
                    PoiId = 2,
                    LanguageCode = "en",
                    TextContent = "Com tam is a famous specialty of Saigon. This restaurant serves broken rice with grilled pork, egg, shrimp, and fresh vegetables. An unforgettable local flavor.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Phở Hương Liệu - Vietnamese
                new POIContent
                {
                    PoiId = 3,
                    LanguageCode = "vi",
                    TextContent = "Phở là món ăn truyền thống của Việt Nam. Nước dùng được nấu từ xương bò và gia vị trong 12 tiếng. Phục vụ buổi sáng sớm, tươi mới mỗi ngày.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Phở Hương Liệu - English
                new POIContent
                {
                    PoiId = 3,
                    LanguageCode = "en",
                    TextContent = "Pho is a traditional Vietnamese soup. The broth is simmered from beef bones and spices for 12 hours. Served early morning, fresh everyday.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Kem Tươi Tây Ninh - Vietnamese
                new POIContent
                {
                    PoiId = 4,
                    LanguageCode = "vi",
                    TextContent = "Kem tươi được làm từ sữa tươi nguyên chất, không chứa kem nhân tạo. Nhiều hương vị lựa chọn như vani, chocolate, matcha và trà đen.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Kem Tươi Tây Ninh - English
                new POIContent
                {
                    PoiId = 4,
                    LanguageCode = "en",
                    TextContent = "Fresh cream ice cream made with pure milk, no artificial ingredients. Multiple flavors available including vanilla, chocolate, matcha, and black tea.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Nước Mía Minh Châu - Vietnamese
                new POIContent
                {
                    PoiId = 5,
                    LanguageCode = "vi",
                    TextContent = "Nước mía tươi được ép từ mía nguyên chất. Có thể để nguyên hoặc trộn với chanh, muối, ớt và các loại rau thơm khác.",
                    UseTextToSpeech = true,
                    AudioPath = null
                },
                // Nước Mía Minh Châu - English
                new POIContent
                {
                    PoiId = 5,
                    LanguageCode = "en",
                    TextContent = "Fresh sugarcane juice pressed from pure cane. Can be served plain or mixed with lime, salt, chili, and other aromatic herbs.",
                    UseTextToSpeech = true,
                    AudioPath = null
                }
            };

            foreach (var content in contents)
            {
                await _database.InsertAsync(content);
            }

            Debug.WriteLine("Demo data seeded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Seeding error: {ex.Message}");
            throw;
        }
    }
}
