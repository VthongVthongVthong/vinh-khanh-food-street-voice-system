using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel;

namespace VinhKhanhstreetfoods.Services;

public class PresenceTrackerService
{
    private readonly HttpClient _httpClient;
    private readonly string _deviceId;
    private readonly string _sessionId;

    // Đã thay đổi: Trỏ đến URL Vercel Worker của bạn
    private const string VercelWorkerUrl = "https://vinh-khanh-worker.vercel.app/api/updatePresence";

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
}