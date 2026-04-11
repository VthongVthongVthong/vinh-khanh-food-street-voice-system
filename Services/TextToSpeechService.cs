using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace VinhKhanhstreetfoods.Services
{
  /// <summary>
  /// Wrapper for MAUI TextToSpeech with language support.
    /// OPTIMIZATIONS:
    /// - Cache locale lookups
  /// - Configurable pitch/volume for TTS customization
    /// - Deferred TTS initialization to prevent ANR
    /// </summary>
    public class TextToSpeechService
    {
  private bool _isPlaying = false;
    private CancellationTokenSource? _cts;

   // ? OPTIMIZATION: Cache locales for faster lookup
       private Dictionary<string, Locale>? _localeCache;
 private bool _localesCached = false;
 private bool _ttsInitialized = false;

    // ? OPTIMIZATION: TTS pitch control
   public float Pitch { get; set; } = 1.0f;
    public float Volume { get; set; } = 1.0f;

      public async Task SpeakAsync(string text, string language = "vi-VN", CancellationToken? token = null)
   {
       if (string.IsNullOrEmpty(text))
      return;

 _isPlaying = true;
  _cts = CancellationTokenSource.CreateLinkedTokenSource(token ?? CancellationToken.None);

      try
   {
      var localeCode = NormalizeToLocaleCode(language);
       
// ? OPTIMIZATION: Initialize TTS in background if needed
      if (!_ttsInitialized)
      {
     _ttsInitialized = true;
 // Warm up TTS in background (don't await on main thread)
     _ = Task.Run(() => GetLocaleOptimizedAsync(localeCode));
      }

// ? OPTIMIZATION: Use cached locale if available
 var locale = await GetLocaleOptimizedAsync(localeCode);
         var settings = new SpeechOptions()
        {
  Volume = Volume,
   Pitch = Pitch,
          Locale = locale
   };

     Debug.WriteLine($"[TextToSpeechService] Speaking '{text.Substring(0, Math.Min(30, text.Length))}...' in {localeCode}");
           await TextToSpeech.SpeakAsync(text, settings, _cts.Token);
        }
      catch (OperationCanceledException)
 {
 Debug.WriteLine("[TextToSpeechService] Playback canceled");
  }
     catch (Exception ex)
       {
 Debug.WriteLine($"[TextToSpeechService] Error: {ex.Message}");
     }
     finally
   {
 _isPlaying = false;
     _cts?.Dispose();
        _cts = null;
     }
        }

        public void Stop()
        {
 _isPlaying = false;
   _cts?.Cancel();
 }

  public bool IsPlaying => _isPlaying;

        /// <summary>
       /// ? OPTIMIZATION: Find a matching locale from installed voices
    /// Uses caching to avoid repeated device queries
    /// </summary>
      private async Task<Locale?> GetLocaleOptimizedAsync(string languageCode)
  {
  try
        {
  // ? OPTIMIZATION: Initialize cache once on first call
  if (!_localesCached)
     {
    try
    {
        var allLocales = await TextToSpeech.GetLocalesAsync();
        _localeCache = new Dictionary<string, Locale>();
        if (allLocales != null)
        {
            foreach (var l in allLocales)
            {
                // Cache by multiple possible keys to increase match rate
                var key = l.Language?.ToLowerInvariant() ?? "";
                if (!string.IsNullOrEmpty(key) && !_localeCache.ContainsKey(key))
                {
                    _localeCache[key] = l;
                }

                // Also try to cache by language-country combination (e.g., "en-us")
                var combinedKey = $"{l.Language}-{l.Country}".ToLowerInvariant();
                if (!string.IsNullOrEmpty(l.Country) && !_localeCache.ContainsKey(combinedKey))
                {
                    _localeCache[combinedKey] = l;
                }
            }
        }
        _localesCached = true;
        Debug.WriteLine($"[TextToSpeechService] Cached {_localeCache.Count} locales");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[TextToSpeechService] Failed to cache locales: {ex.Message}");
        _localeCache = new();
  _localesCached = true;
    }
   }

    if (_localeCache == null || _localeCache.Count == 0)
     return null;

    var normalized = languageCode.ToLowerInvariant(); // e.g., "en-us" or "ja-jp"
    var langPrefix = normalized.Split('-')[0]; // e.g., "en" or "ja"
    var cachedLocales = _localeCache.Values;

    // 1. Exact match by Id (Android/iOS often use full tags in Id)
    var match = cachedLocales.FirstOrDefault(l => l.Id != null && l.Id.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

    // 2. Match by Language + Country precise combo
    if (match == null && normalized.Contains('-'))
    {
        var parts = normalized.Split('-');
        match = cachedLocales.FirstOrDefault(l => 
            l.Language != null && l.Language.Equals(parts[0], StringComparison.OrdinalIgnoreCase) &&
            l.Country != null && l.Country.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
    }

    // 3. Match by Language code prefix (2-letter)
    if (match == null)
    {
        match = cachedLocales.FirstOrDefault(l => 
            (l.Language != null && l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase)) ||
            (l.Id != null && l.Id.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase)));
    }

    // 4. Match by 3-letter ISO code (Android TTS engines often return 3-letter codes like 'eng', 'vie', 'jpn')
    if (match == null)
    {
        string[] isoCodes = langPrefix switch
        {
            "en" => new[] { "eng" },
            "vi" => new[] { "vie" },
            "ja" => new[] { "jpn" },
            "ko" => new[] { "kor" },
            "zh" => new[] { "zho", "chi" }, // Chinese can use zho or chi
            "fr" => new[] { "fra", "fre" }, // French can use fra or fre
            "ru" => new[] { "rus" },
            _ => new[] { langPrefix }
        };

        match = cachedLocales.FirstOrDefault(l => 
            l.Language != null && isoCodes.Any(iso => l.Language.StartsWith(iso, StringComparison.OrdinalIgnoreCase)));
    }

    // 5. Broadest text search across all properties (Handles custom OEM TTS engines like Samsung/Xiaomi/Oppo)
    if (match == null)
    {
        string[] searchTerms = langPrefix switch
        {
            "en" => new[] { "en", "eng", "english", "us", "uk" },
            "vi" => new[] { "vi", "vie", "vietnamese", "vn" },
            "ja" => new[] { "ja", "jpn", "japanese", "jp" },
            "ko" => new[] { "ko", "kor", "korean", "kr" },
            "zh" => new[] { "zh", "zho", "chi", "chinese", "cn", "tw" },
            "fr" => new[] { "fr", "fra", "fre", "french" },
            "ru" => new[] { "ru", "rus", "russian" },
            _ => new[] { langPrefix }
        };

        match = cachedLocales.FirstOrDefault(l => 
            searchTerms.Any(term => 
                (l.Language != null && l.Language.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (l.Name != null && l.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (l.Id != null && l.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
            ));
    }

    // Return the matched locale or fallback to the first available
    var finalLocale = match ?? cachedLocales.FirstOrDefault();
    Debug.WriteLine($"[TextToSpeechService] Final Locale Selected: Id={finalLocale?.Id}, Lang={finalLocale?.Language}, Name={finalLocale?.Name}");
    return finalLocale;
     }
  catch (Exception ex)
   {
       Debug.WriteLine($"[TextToSpeechService] GetLocaleOptimized error: {ex.Message}");
       return null;
}
        }

  private static string NormalizeToLocaleCode(string code)
    {
      if (string.IsNullOrWhiteSpace(code)) return "vi-VN";

 var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();

     return normalized switch
     {
          "vn" or "vi" or "vi-vn" => "vi-VN",
     "en" or "en-us" => "en-US",
    "en-gb" => "en-GB",
        "zh" or "zh-cn" or "zh-hans" => "zh-CN",
    "zh-tw" or "zh-hant" => "zh-TW",
         "ja" or "ja-jp" => "ja-JP",
       "ko" or "ko-kr" => "ko-KR",
    "fr" or "fr-fr" => "fr-FR",
    "ru" or "ru-ru" => "ru-RU",
    _ when normalized.Length == 2 => $"{normalized}-{normalized.ToUpperInvariant()}",
   _ => normalized
    };
     }
    }
}
