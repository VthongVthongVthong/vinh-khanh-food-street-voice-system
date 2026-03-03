using SQLite;

namespace VinhKhanhFoodGuide.Models;

[Table("POIContent")]
public class POIContent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, Indexed]
    public int PoiId { get; set; }

    [NotNull]
    public string LanguageCode { get; set; } // e.g., "vi", "en"

    [NotNull]
    public string TextContent { get; set; } // Description text

    public string AudioPath { get; set; } // Optional: path to audio file

    public bool UseTextToSpeech { get; set; } = true; // Use TTS if no audio file
}
