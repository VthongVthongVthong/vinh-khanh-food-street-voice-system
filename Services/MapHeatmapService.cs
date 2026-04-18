using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace VinhKhanhstreetfoods.Services
{
    public class MapHeatmapService
    {
        private readonly HttpClient _httpClient;
        private const string FirebaseBaseUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app";

        public MapHeatmapService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<int, double>> GetHotScoresAsync(DayOfWeek targetDay, int targetHour)
        {
            var hotScores = new Dictionary<int, double>();

            try
            {
                var visitLogs = await FetchVisitLogsAsync();
                var audioPlayLogs = await FetchAudioPlayLogsAsync();
                var userPresence = await FetchUserPresenceAsync();

                // 1. Real-time Presence
                var realTimePresence = userPresence
                    .Where(p => p.PoiId.HasValue && p.UpdatedAt.DayOfWeek == targetDay && p.UpdatedAt.Hour == targetHour)
                    .GroupBy(p => p.PoiId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 2. Historical Density
                var historicalVisits = visitLogs
                    .Where(v => v.VisitTime.DayOfWeek == targetDay && v.VisitTime.Hour == targetHour)
                    .GroupBy(v => v.PoiId)
                    .ToDictionary(g => g.Key, g => g.Select(v => v.SessionId).Distinct().Count());

                // 3. Quality Multiplier (Audio Completion)
                var qualityScores = audioPlayLogs
                    .Where(a => a.PlayTime.DayOfWeek == targetDay && a.PlayTime.Hour == targetHour)
                    .GroupBy(a => a.PoiId)
                    .ToDictionary(g => g.Key, g => g.Average(a => a.CompletionRate));

                var allPois = historicalVisits.Keys.Union(qualityScores.Keys).Union(realTimePresence.Keys).Distinct();

                foreach (var poiId in allPois)
                {
                    int currentStrangers = realTimePresence.GetValueOrDefault(poiId, 0);
                    int averageHistory = historicalVisits.GetValueOrDefault(poiId, 0);
                    double completionRate = qualityScores.GetValueOrDefault(poiId, 0);

                    // Formula: (UniqueVisitors * 10) + (CompletionRate * 0.3) + (CurrentVisitors * 20)
                    double score = (averageHistory * 10) + (completionRate * 0.3) + (currentStrangers * 20);

                    // Clamp to 0-100
                    hotScores[poiId] = Math.Min(100, Math.Max(0, score));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapHeatmapService] Error: {ex.Message}");
            }

            return hotScores;
        }

        private async Task<List<VisitLogDto>> FetchVisitLogsAsync()
        {
            var response = await _httpClient.GetAsync($"{FirebaseBaseUrl}/VisitLog.json");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<VisitLogDto>>(content) ?? new List<VisitLogDto>();
                    return arr.Where(x => x != null && x.PoiId > 0).ToList();
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, VisitLogDto>>(content) ?? new Dictionary<string, VisitLogDto>();
                    return dict.Values.Where(x => x != null && x.PoiId > 0).ToList();
                }
            }
            return new List<VisitLogDto>();
        }

        private async Task<List<AudioPlayLogDto>> FetchAudioPlayLogsAsync()
        {
            var response = await _httpClient.GetAsync($"{FirebaseBaseUrl}/AudioPlayLog.json");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<AudioPlayLogDto>>(content) ?? new List<AudioPlayLogDto>();
                    return arr.Where(x => x != null && x.PoiId > 0).ToList();
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, AudioPlayLogDto>>(content) ?? new Dictionary<string, AudioPlayLogDto>();
                    return dict.Values.Where(x => x != null && x.PoiId > 0).ToList();
                }
            }
            return new List<AudioPlayLogDto>();
        }

        private async Task<List<UserPresenceDto>> FetchUserPresenceAsync()
        {
            var response = await _httpClient.GetAsync($"{FirebaseBaseUrl}/UserPresence.json");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<UserPresenceDto>>(content) ?? new List<UserPresenceDto>();
                    return arr.Where(x => x != null).ToList();
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, UserPresenceDto>>(content) ?? new Dictionary<string, UserPresenceDto>();
                    return dict.Values.Where(x => x != null).ToList();
                }
            }
            return new List<UserPresenceDto>();
        }

        private class VisitLogDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("poiId")]
            public int PoiId { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("visitTime")]
            public string VisitTimeString { get; set; } = string.Empty;
            [System.Text.Json.Serialization.JsonPropertyName("sessionId")]
            public string SessionId { get; set; } = string.Empty;
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

        private class UserPresenceDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("poiId")]
            public int? PoiId { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
            public string UpdatedAtString { get; set; } = string.Empty;
            public DateTime UpdatedAt => DateTime.TryParse(UpdatedAtString, out var dt) ? dt : DateTime.MinValue;
        }
    }
}
