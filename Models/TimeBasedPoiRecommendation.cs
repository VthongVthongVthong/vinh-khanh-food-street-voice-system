using System;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Models
{
    public class TimeBasedPoiRecommendation
    {
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string HighlightTag { get; set; } = string.Empty;
        public double AverageCompletionRate { get; set; }
        public int VisitCount { get; set; }
        public double AverageDurationStayed { get; set; }
    }
}
