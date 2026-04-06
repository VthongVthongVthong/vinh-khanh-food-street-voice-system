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
                var key = l.Language.ToLowerInvariant();
                if (!_localeCache.ContainsKey(key))
                {
                    _localeCache[key] = l;
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

  var normalized = languageCode.ToLowerInvariant();
     var langPrefix = normalized.Split('-')[0];

// ? OPTIMIZATION: Use cached dictionary for O(1) lookup
   if (_localeCache.TryGetValue(normalized, out var exactMatch))
       {
      return exactMatch;
    }

     // Match by language prefix (e.g., en)
     var prefixMatch = _localeCache.Values.FirstOrDefault(l => l.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase));
if (prefixMatch is not null)
   return prefixMatch;

        // Fallback: use first available locale
return _localeCache.Values.FirstOrDefault();
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
