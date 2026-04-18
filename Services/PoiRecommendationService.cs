using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class PoiRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly IPOIRepository _poiRepository;
        private const string FirebaseBaseUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app";

        public PoiRecommendationService(HttpClient httpClient, IPOIRepository poiRepository)
        {
            _httpClient = httpClient;
            _poiRepository = poiRepository;
        }

        public async Task<List<TimeBasedPoiRecommendation>> GetTrendingPoisAsync(DayOfWeek dayOfWeek, string timeBlock)
        {
            string cacheKey = $"TrendingPois_Cache_{dayOfWeek}_{timeBlock}";
            var recommendations = new List<TimeBasedPoiRecommendation>();

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    string cachedData = Preferences.Get(cacheKey, string.Empty);
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        var cachedList = System.Text.Json.JsonSerializer.Deserialize<List<TimeBasedPoiRecommendation>>(cachedData);
                        if (cachedList != null && cachedList.Any())
                        {
                            return cachedList;
                        }
                    }
                    return recommendations;
                }

                // In a real production app, you might want a backend API to aggregate these,
                // but since the requirement is to do it in MAUI, we download the necessary data.
                // Note: For a very large DB, consider doing this aggregation on a Cloud Function.

                // Fetch logs
                var visitLogs = await FetchVisitLogsAsync();
                var audioPlayLogs = await FetchAudioPlayLogsAsync();

                // Filters logs based on DayOfWeek and TimeBlock
                var filteredVisits = visitLogs
                    .Where(v => IsInTimeBlock(v.VisitTime, dayOfWeek, timeBlock))
                    .GroupBy(v => v.PoiId)
                    .Select(g => new
                    {
                        PoiId = g.Key,
                        Count = g.Count(),
                        AvgDuration = g.Average(x => x.DurationStayed)
                    })
                    .OrderByDescending(v => v.Count)
                    .ToDictionary(v => v.PoiId);

                var filteredAudios = audioPlayLogs
                    .Where(a => IsInTimeBlock(a.PlayTime, dayOfWeek, timeBlock))
                    .GroupBy(a => a.PoiId)
                    .Select(g => new
                    {
                        PoiId = g.Key,
                        AvgCompletion = g.DefaultIfEmpty().Average(x => x?.CompletionRate ?? 0)
                    })
                    .ToDictionary(a => a.PoiId);

                // Combine results to score top POIs
                var allPoiIds = filteredVisits.Keys.Union(filteredAudios.Keys).Distinct().ToList();

                // Dữ liệu mẫu (mock DB) rất nhỏ nên có thể Khung giờ hiện tại không có ai check-in.
                // Thêm Fallback: Nếu không có ai trong khung giờ này, lấy danh sách nổi bật TẤT CẢ các khung giờ.
                if (allPoiIds.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[PoiRecommendationService] No exact time match. Applying Fallback block.");
                    
                    filteredVisits = visitLogs
                        .GroupBy(v => v.PoiId)
                        .Select(g => new { PoiId = g.Key, Count = g.Count(), AvgDuration = g.Average(x => x.DurationStayed) })
                        .ToDictionary(v => v.PoiId);

                    filteredAudios = audioPlayLogs
                        .GroupBy(a => a.PoiId)
                        .Select(g => new { PoiId = g.Key, AvgCompletion = g.DefaultIfEmpty().Average(x => x?.CompletionRate ?? 0) })
                        .ToDictionary(a => a.PoiId);

                    allPoiIds = filteredVisits.Keys.Union(filteredAudios.Keys).Distinct().ToList();
                }
                
                var scoredPois = new List<(int PoiId, double Score, int Visits, double AvgDuration, double AvgCompletion)>();

                foreach (var id in allPoiIds)
                {
                    int visits = filteredVisits.TryGetValue(id, out var v) ? v.Count : 0;
                    double avgDuration = filteredVisits.TryGetValue(id, out var v2) ? v2.AvgDuration : 0;
                    double avgCompletion = filteredAudios.TryGetValue(id, out var a) ? a.AvgCompletion : 0;

                    // Example scoring weight: 70% visits, 30% completion rate
                    // Normalized visits (max 10 for basic scoring)
                    double visitScore = Math.Min(visits, 10) * 0.7;
                    double completionScore = (avgCompletion / 100.0) * 0.3 * 10;
                    double totalScore = visitScore + completionScore;

                    scoredPois.Add((id, totalScore, visits, avgDuration, avgCompletion));
                }

                var topPois = scoredPois.OrderByDescending(x => x.Score).Take(5).ToList();
                var dbPois = await _poiRepository.GetAllPOIsAsync(); // Get details locally

                foreach (var item in topPois)
                {
                    var poiDetails = dbPois.FirstOrDefault(p => p.Id == item.PoiId);
                    if (poiDetails == null) continue;

                    string tag = GetHighlightTag(item.Visits, item.AvgCompletion);

                    recommendations.Add(new TimeBasedPoiRecommendation
                    {
                        PoiId = item.PoiId,
                        PoiName = poiDetails.Name,
                        ImageUrl = !string.IsNullOrEmpty(poiDetails.AvatarImageUrl) ? poiDetails.AvatarImageUrl : (poiDetails.ImageUrlList.FirstOrDefault() ?? "placeholder_image.png"),
                        AverageCompletionRate = item.AvgCompletion,
                        VisitCount = item.Visits,
                        AverageDurationStayed = item.AvgDuration,
                        HighlightTag = tag
                    });
                }

                if (recommendations.Any())
                {
                    Preferences.Set(cacheKey, System.Text.Json.JsonSerializer.Serialize(recommendations));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PoiRecommendationService] Error: {ex.Message}");
                string cachedData = Preferences.Get(cacheKey, string.Empty);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var cachedList = System.Text.Json.JsonSerializer.Deserialize<List<TimeBasedPoiRecommendation>>(cachedData);
                    if (cachedList != null)
                    {
                        return cachedList;
                    }
                }
            }

            return recommendations;
        }

        private string GetHighlightTag(int visitCount, double avgCompletion)
        {
            if (visitCount >= 3 && avgCompletion >= 85)
                return "🔥 Nổi bật và đáng trải nghiệm!";
            if (visitCount >= 3)
                return "🔥 Đang được yêu thích lúc này";
            if (avgCompletion >= 90)
                return "💎 Viên ngọc ẩn, rất đáng nghe audio";
            return "Khám phá ngay";
        }

        private bool IsInTimeBlock(DateTime time, DayOfWeek targetDay, string targetBlock)
        {
            if (time.DayOfWeek != targetDay)
                return false;

            int hour = time.Hour;

            // Sáng: 6-11, Trưa: 11-14, Chiều: 14-18, Tối: 18-22
            return targetBlock switch
            {
                "Morning" => hour >= 6 && hour < 11,
                "Noon" => hour >= 11 && hour < 14,
                "Afternoon" => hour >= 14 && hour < 18,
                "Evening" => hour >= 18 && hour < 22,
                "Night" => hour >= 22 || hour < 6,
                _ => true // Default case
            };
        }

        private async Task<List<VisitLogDto>> FetchVisitLogsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FirebaseBaseUrl}/VisitLog.json");
                if (response.IsSuccessStatusCode)
                {
                    // Due to Firebase storing arrays with nulls, it might parse as Dto list or dictionary.
                    // We can deserialize as Dictionary<string, VisitLogDto> or List<VisitLogDto>
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.TrimStart().StartsWith("["))
                    {
                        var arr = System.Text.Json.JsonSerializer.Deserialize<List<VisitLogDto>>(content) ?? new List<VisitLogDto>();
                        return arr.Where(x => x != null && x.PoiId > 0).ToList();
                    }
                    else
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, VisitLogDto>>(content) ?? new Dictionary<string, VisitLogDto>();
                        return dict.Values.Where(x => x != null && x.PoiId > 0).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PoiRecommendationService] VisitLog Fetch Error: {ex.Message}");
            }
            return new List<VisitLogDto>();
        }

        private async Task<List<AudioPlayLogDto>> FetchAudioPlayLogsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FirebaseBaseUrl}/AudioPlayLog.json");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.TrimStart().StartsWith("["))
                    {
                        var arr = System.Text.Json.JsonSerializer.Deserialize<List<AudioPlayLogDto>>(content) ?? new List<AudioPlayLogDto>();
                        return arr.Where(x => x != null && x.PoiId > 0).ToList();
                    }
                    else
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, AudioPlayLogDto>>(content) ?? new Dictionary<string, AudioPlayLogDto>();
                        return dict.Values.Where(x => x != null && x.PoiId > 0).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PoiRecommendationService] AudioPlayLog Fetch Error: {ex.Message}");
            }
            return new List<AudioPlayLogDto>();
        }

        // --- DTOs for Firebase parsing ---

        private class VisitLogDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("poiId")]
            public int PoiId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("visitTime")]
            public string VisitTimeString { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("durationStayed")]
            public double DurationStayed { get; set; }

            public DateTime VisitTime => DateTime.TryParse(VisitTimeString, out var dt) ? dt : DateTime.MinValue;
        }

        private class AudioPlayLogDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("poiId")]
            public int PoiId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("playTime")]
            public string PlayTimeString { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("completionRate")]
            public double CompletionRate { get; set; }

            public DateTime PlayTime => DateTime.TryParse(PlayTimeString, out var dt) ? dt : DateTime.MinValue;
        }
    }
}
