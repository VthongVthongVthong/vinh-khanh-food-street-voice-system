using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel;

namespace VinhKhanhstreetfoods.Services;

public class PresenceTrackerService
{
    private readonly HttpClient _httpClient;
    private readonly string _deviceId;
    private readonly string _sessionId;

    private const string VercelWorkerUrl = "https://vinh-khanh-worker.vercel.app/api/updatePresence";
    private const string VercelAudioLogUrl = "https://vinh-khanh-worker.vercel.app/api/logAudioPlay";
    private const string VercelVisitLogUrl = "https://vinh-khanh-worker.vercel.app/api/logVisit";
    private const string VercelTourLogUrl = "https://vinh-khanh-worker.vercel.app/api/logTour";

    // Track active Tour sequence
    public int? ActiveTourId { get; private set; }
    public List<int> ActiveTourPoiIds { get; private set; } = new();
    public int VisitedPoisCount { get; private set; }
    public string ActiveTourLanguage { get; private set; } = "vi";

    public PresenceTrackerService()
    {
        _httpClient = new HttpClient();

        // 1. Tạo hoặc tải Device UUID (Định danh thiết bị vĩnh viễn cho khách)
        var storedDeviceId = Preferences.Get("App_DeviceId", string.Empty);
        if (string.IsNullOrEmpty(storedDeviceId))
        {
            storedDeviceId = Guid.NewGuid().ToString();
            Preferences.Set("App_DeviceId", storedDeviceId);
        }
        _deviceId = storedDeviceId;

        // 2. Tạo Session ID (Chỉ tồn tại trong 1 lần mở app)
        _sessionId = "sess-" + Guid.NewGuid().ToString().Substring(0, 8);
    }

    public async Task SendPresenceAsync(double latitude, double longitude, int? currentPoiId = null)
    {
        try
        {
            // Lấy thông tin đăng nhập từ Preferences
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            
            // Lấy User ID thực tế nếu đã đăng nhập, ngược lại là null (Guest)
            // Lưu ý: Đảm bảo bạn có lưu "LoggedInUserId" khi user login thành công nhé
            int? userId = isLoggedIn ? Preferences.Get("LoggedInUserId", 0) : null; 
            
            // Nếu LoggedInUserId = 0 do lỗi lấy data, ép về null để tính là Guest
            if (userId == 0) userId = null;

            var payload = new
            {
                sessionId = _sessionId,
                userId = userId, 
                deviceId = _deviceId,
                poiId = currentPoiId,
                latitude = latitude,
                longitude = longitude,
                platform = DeviceInfo.Platform.ToString(),
                appVersion = AppInfo.VersionString
            };

            // Gửi dữ liệu lên Vercel Worker (Fire and forget)
            var response = await _httpClient.PostAsJsonAsync(VercelWorkerUrl, payload);
            
            // Log ra console để dễ debug trên Visual Studio
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Presence] Đã gửi vị trí thành công: {latitude}, {longitude}");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Presence] Lỗi gửi vị trí: {response.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Presence] Exception khi gửi vị trí: {ex.Message}");
        }
    }

    public async Task LogAudioPlayAsync(int poiId, string language, DateTime playTime, double durationListened, double completionRate)
    {
        try
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            int? userId = isLoggedIn ? Preferences.Get("LoggedInUserId", 0) : null;
            if (userId == 0) userId = null;

            var payload = new
            {
                sessionId = _sessionId,
                userId = userId,
                deviceId = _deviceId,
                poiId = poiId,
                language = language,
                playTime = playTime.ToString("yyyy-MM-dd HH:mm:ss"),
                durationListened = durationListened,
                completionRate = completionRate,
                platform = DeviceInfo.Platform.ToString(),
                appVersion = AppInfo.VersionString
            };

            var response = await _httpClient.PostAsJsonAsync(VercelAudioLogUrl, payload);
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioLog] Gửi dữ liệu audio thành công cho POI: {poiId}");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AudioLog] Lỗi gửi log audio: {response.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioLog] Exception: {ex.Message}");
        }
    }

    public async Task LogVisitAsync(int poiId, DateTime visitTime, DateTime exitTime, double durationStayed, double latitude, double longitude, string triggerType = "AUTO")
    {
        try
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            int? userId = isLoggedIn ? Preferences.Get("LoggedInUserId", 0) : null;
            if (userId == 0) userId = null;

            var payload = new
            {
                sessionId = _sessionId,
                userId = userId,
                deviceId = _deviceId,
                poiId = poiId,
                visitTime = visitTime.ToString("yyyy-MM-dd HH:mm:ss"),
                exitTime = exitTime.ToString("yyyy-MM-dd HH:mm:ss"),
                durationStayed = durationStayed,
                latitude = latitude,
                longitude = longitude,
                triggerType = triggerType,
                platform = DeviceInfo.Platform.ToString(),
                appVersion = AppInfo.VersionString
            };

            var response = await _httpClient.PostAsJsonAsync(VercelVisitLogUrl, payload);
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[VisitLog] Gửi log ghé thăm thành công cho POI: {poiId}");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[VisitLog] Lỗi gửi log ghé thăm: {response.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VisitLog] Exception: {ex.Message}");
        }
    }

    public async Task StartTourLogAsync(int tourId, List<int> poiIds, string language)
    {
        ActiveTourId = tourId;
        ActiveTourPoiIds = new List<int>(poiIds);
        VisitedPoisCount = 0;
        ActiveTourLanguage = language;
        
        await SyncTourLogToVercelAsync("ongoing");
    }

    public async Task MarkPoiVisitedInTourAsync(int poiId)
    {
        if (ActiveTourId != null && ActiveTourPoiIds.Contains(poiId))
        {
            VisitedPoisCount++;
            
            // Check if completed
            string status = VisitedPoisCount >= ActiveTourPoiIds.Count ? "completed" : "ongoing";
            await SyncTourLogToVercelAsync(status);
            
            if (status == "completed")
            {
                ActiveTourId = null;
                ActiveTourPoiIds.Clear();
            }
        }
    }

    private async Task SyncTourLogToVercelAsync(string status)
    {
        if (ActiveTourId == null) return;

        try
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            int? userId = isLoggedIn ? Preferences.Get("LoggedInUserId", 0) : null;
            if (userId == 0) userId = null;

            var payload = new
            {
                sessionId = _sessionId,
                userId = userId,
                deviceId = _deviceId,
                tourId = ActiveTourId.Value,
                language = ActiveTourLanguage,
                status = status,
                totalPOIs = ActiveTourPoiIds.Count,
                visitedPOIs = VisitedPoisCount
            };

            var response = await _httpClient.PostAsJsonAsync(VercelTourLogUrl, payload);
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[TourLog] Đã đồng bộ TourLog thành công (Status: {status})");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[TourLog] Lỗi gửi TourLog: {response.StatusCode} - {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TourLog] Exception khi gửi TourLog: {ex.Message}");
        }
    }
}