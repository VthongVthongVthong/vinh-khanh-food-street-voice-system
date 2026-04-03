using SQLite;

namespace VinhKhanhstreetfoods.Models;

[Table("TranslationCache")]
public class TranslationCacheEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PoiId { get; set; }

    [Indexed]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// 0 = description, 1 = tts script
    /// </summary>
    public int IsTtsScript { get; set; }

    [NotNull]
    public string TranslatedText { get; set; } = string.Empty;

    public int IsDownloadedPack { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
