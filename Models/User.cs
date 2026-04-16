using System.Text.Json.Serialization;

namespace VinhKhanhstreetfoods.Models;

public class User
{
    [JsonPropertyName("id")]
    public object? IdObj { get; set; }

    public string Id => IdObj?.ToString() ?? string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Username : FullName;
}