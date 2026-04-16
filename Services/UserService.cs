using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanhstreetfoods.Models;
using BCrypt.Net;

namespace VinhKhanhstreetfoods.Services;

public class UserService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/User.json";

    public UserService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<User?> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            var response = await _httpClient.GetAsync(BaseUrl);
            if (!response.IsSuccessStatusCode) return null;

            var jsonString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(jsonString) || jsonString == "null") return null;

            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Null) continue;
                    
                    var user = JsonSerializer.Deserialize<User>(element.GetRawText());
                    if (user != null && 
                        (user.Username == usernameOrEmail || user.Email == usernameOrEmail) &&
                        VerifyPassword(password, user.PasswordHash))
                    {
                        return user;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Null) continue;

                    var user = JsonSerializer.Deserialize<User>(property.Value.GetRawText());
                    if (user != null && 
                        (user.Username == usernameOrEmail || user.Email == usernameOrEmail) &&
                        VerifyPassword(password, user.PasswordHash))
                    {
                        return user;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Log exception
        }

        return null;
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            // The password hash in the database is from PHP's password_hash(PASSWORD_DEFAULT)
            // which produces a bcrypt hash. BCrypt.Net can verify it.
            // Some PHP bcrypt hashes start with $2y$ which BCrypt.Net might not fully support natively without minor replace,
            // but standard BCrypt.Net-Next handles $2y$ just fine.
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
