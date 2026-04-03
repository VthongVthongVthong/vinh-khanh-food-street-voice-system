using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Manages configuration from secure sources (env folder, secrets, environment variables)
/// Never hardcode API keys - always load from this service!
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
 }

    /// <summary>
    /// Get Track Asia API Key from secure configuration
    /// </summary>
    public string GetTrackAsiaApiKey()
    {
        var key = _configuration["TrackAsiaApiKey"];
        
      if (string.IsNullOrEmpty(key))
        {
Debug.WriteLine("?? [ConfigurationService] TrackAsiaApiKey not found in configuration");
            return string.Empty;
        }

        // Log only first 8 chars for security
    Debug.WriteLine($"? [ConfigurationService] Loaded TrackAsiaApiKey: {key.Substring(0, Math.Min(8, key.Length))}...");
        return key;
    }

    /// <summary>
    /// Get Google Maps API Key from secure configuration
    /// </summary>
    public string GetGoogleMapsApiKey()
    {
    var key = _configuration["GoogleMapsApiKey"];
        if (string.IsNullOrEmpty(key))
 {
            Debug.WriteLine("?? [ConfigurationService] GoogleMapsApiKey not found");
     return string.Empty;
        }
        return key;
    }

    /// <summary>
    /// Get Translation Service Key from secure configuration
    /// </summary>
    public string GetTranslationServiceKey()
    {
        var key = _configuration["TranslationServiceKey"];
        if (string.IsNullOrEmpty(key))
        {
            Debug.WriteLine("?? [ConfigurationService] TranslationServiceKey not found");
       return string.Empty;
   }
        return key;
    }

    /// <summary>
    /// Check if running in development mode
    /// </summary>
  public bool IsDevelopment()
    {
        return _configuration["Environment"] == "Development";
    }

    /// <summary>
    /// Get any configuration value by key
  /// </summary>
    public string GetValue(string key, string defaultValue = "")
    {
        return _configuration[key] ?? defaultValue;
    }
}
