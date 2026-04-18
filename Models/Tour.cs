using SQLite;

namespace VinhKhanhstreetfoods.Models;

[Table("Tour")]
public class Tour
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Mô t? ?a ngôn ng?
    public string? DescriptionEn { get; set; }

    public string? DescriptionZh { get; set; }

    public string? DescriptionJa { get; set; }

    public string? DescriptionKo { get; set; }

    public string? DescriptionFr { get; set; }

    public string? DescriptionRu { get; set; }

    public int IsActive { get; set; } = 1;

    // Th?i gian ??c tính hoàn thành tour (phút)
    public int? EstimatedMinutes { get; set; }

    // ?nh bìa tour
    public string? CoverImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
