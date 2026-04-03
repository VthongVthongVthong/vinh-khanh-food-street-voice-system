using System.Collections.Concurrent;
using System.Diagnostics;
using GTranslate.Translators;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

/// <summary>
/// Hybrid translation strategy with OPTIMIZATIONS:
/// 1) Offline base languages: vi, en, zh
/// 2) Online languages: ja, ko, fr, ru (when internet + API key is configured)
/// 3) Fallback: DB cache, then original text
/// 4) PERFORMANCE: Concurrent requests, request pooling, smart caching
/// </summary>
public sealed class HybridTranslationService : ITranslationService
{
    private static readonly string[] OfflineBaseLanguages = ["vi", "en", "zh"];
    private static readonly string[] OnlineLanguages = ["ja", "ko", "fr", "ru"];

    private readonly IPOIRepository _poiRepository;
    private readonly ConfigurationService _configurationService;
    private readonly GoogleTranslator _translator = new();

    // ? OPTIMIZATION: Concurrent memory cache with better TTL management
    private readonly ConcurrentDictionary<string, CachedTranslation> _memoryCache = new();
    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(15);  // Extended to 15 min

    // ? OPTIMIZATION: Limit concurrent API requests
    private readonly SemaphoreSlim _apiLimiter = new(3);  // Max 3 concurrent API calls
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(30);  // Aggressive timeout

    public HybridTranslationService(IPOIRepository poiRepository, ConfigurationService configurationService)
    {
        _poiRepository = poiRepository;
        _configurationService = configurationService;
    }

    public IReadOnlyList<string> GetOfflineBaseLanguages() => OfflineBaseLanguages;

    public IReadOnlyList<string> GetOnlineLanguages() => OnlineLanguages;

    public async Task<string> ResolveNarrationTextAsync(POI poi, string targetLanguage, bool preferTtsScript = true)
    {
    var target = NormalizeLang(targetLanguage);

        // ? PRIORITY 1: Check offline DB columns first (NO API, instant)
    var offlineText = preferTtsScript 
       ? poi.GetTtsScriptByLanguage(target)
: poi.GetDescriptionByLanguage(target);

        if (!string.IsNullOrWhiteSpace(offlineText))
        {
   Debug.WriteLine($"[HybridTranslationService] ? Found offline {target} text for POI {poi.Id}");
      return offlineText;
        }

  // ? PRIORITY 2: Check memory cache (instant)
        var memoryKey = BuildCacheKey(poi.Id, target, preferTtsScript);
        if (_memoryCache.TryGetValue(memoryKey, out var memory) && DateTime.UtcNow - memory.CachedAt < MemoryTtl)
        {
           Debug.WriteLine($"[HybridTranslationService] Using memory cache for POI {poi.Id} ({target})");
          return memory.Value;
    }

        // ? PRIORITY 3: Check DB cache (fast, from previous API calls)
        var cached = await _poiRepository.GetCachedTranslationAsync(poi.Id, target, preferTtsScript);
        if (!string.IsNullOrWhiteSpace(cached))
        {
    _memoryCache[memoryKey] = new CachedTranslation { Value = cached, CachedAt = DateTime.UtcNow };
    Debug.WriteLine($"[HybridTranslationService] Using DB cache for POI {poi.Id} ({target})");
           return cached;
        }

        // ? PRIORITY 4: Try API translation (only if needed)
  if (!CanUseOnlineTranslation() || !IsInternetAvailable())
        {
     // No API available, return Vietnamese fallback
    var fallbackText = preferTtsScript ? poi.TtsScript ?? poi.DescriptionText : poi.DescriptionText;
    Debug.WriteLine($"[HybridTranslationService] No API available for {target}, using fallback (VI)");
        return fallbackText;
  }

        // ? Acquire semaphore to limit concurrent API calls
        try
   {
            await _apiLimiter.WaitAsync();

    // Run translation off-thread to prevent UI blocking
        var sourceText = preferTtsScript ? poi.TtsScript ?? poi.DescriptionText : poi.DescriptionText;
     var sourceLanguage = NormalizeLang(poi.TtsLanguage);

   var translated = await Task.Run(async () =>
        {
      return await TranslateCoreWithTimeoutAsync(sourceText, sourceLanguage, target);
         });

      // Only cache if translation was successful and different from source
 if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, sourceText, StringComparison.Ordinal))
      {
   _memoryCache[memoryKey] = new CachedTranslation { Value = translated, CachedAt = DateTime.UtcNow };
      
    // Cache to DB without blocking
    _ = Task.Run(async () =>
      {
         await _poiRepository.UpsertCachedTranslationAsync(poi.Id, target, preferTtsScript, translated, isDownloadedPack: false);
        Debug.WriteLine($"[HybridTranslationService] Cached translation for POI {poi.Id} ({target})");
         });
    
      return translated;
 }
      }
       finally
        {
    _apiLimiter.Release();
       }

 // Fallback to original text if API call failed
  var fallback = preferTtsScript ? poi.TtsScript ?? poi.DescriptionText : poi.DescriptionText;
    Debug.WriteLine($"[HybridTranslationService] API translation failed for POI {poi.Id} ({target}), using fallback");
      return fallback;
   }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
    if (string.IsNullOrWhiteSpace(text))
        return text;

        var src = NormalizeLang(sourceLanguage);
        var tgt = NormalizeLang(targetLanguage);

        if (src == tgt || tgt == "vi")
      return text;

        // ? No API needed for offline languages
        if (!CanUseOnlineTranslation() || !IsInternetAvailable())
 return text;

        try
   {
      await _apiLimiter.WaitAsync();
     return await TranslateCoreWithTimeoutAsync(text, src, tgt);
     }
  finally
        {
 _apiLimiter.Release();
   }
 }

    public async Task<bool> IsAvailableAsync()
  {
     // Hybrid mode is still usable offline due to base languages.
        if (!CanUseOnlineTranslation() || !IsInternetAvailable())
  return true;

     try
        {
   await _apiLimiter.WaitAsync();
            
  using var cts = new CancellationTokenSource(ApiTimeout);
   var result = await _translator.TranslateAsync("ping", "en", "vi");
            return !string.IsNullOrWhiteSpace(result?.Translation);
        }
  catch (OperationCanceledException)
        {
        Debug.WriteLine("?? [HybridTranslationService] API availability check timeout");
 return false;
        }
        catch
        {
     return false;
        }
     finally
        {
 _apiLimiter.Release();
        }
  }

    public async Task<int> DownloadLanguagePackAsync(string languageCode, CancellationToken cancellationToken = default)
    {
        var target = NormalizeLang(languageCode);
        if (!OnlineLanguages.Contains(target))
     return 0;

        if (!CanUseOnlineTranslation() || !IsInternetAvailable())
            return 0;

        var pois = await _poiRepository.GetActivePOIsAsync();
        var count = 0;

        // ? OPTIMIZATION: Batch process with semaphore limiting
  var tasks = new List<Task>();
        
        foreach (var poi in pois)
      {
    cancellationToken.ThrowIfCancellationRequested();

       var sourceDesc = poi.DescriptionText;
            var sourceTts = !string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.TtsScript! : poi.DescriptionText;

        // Queue translation tasks with semaphore protection
            var task = ProcessTranslationAsync(poi.Id, sourceDesc, sourceTts, target, cancellationToken);
            tasks.Add(task);
        }

        // ? OPTIMIZATION: Process in batches of 5 to avoid overloading
        for (int i = 0; i < tasks.Count; i += 5)
        {
            var batch = tasks.Skip(i).Take(5);
   await Task.WhenAll(batch);
            count += 2 * (int)Math.Min(5, tasks.Count - i);  // Each POI = 2 translations (desc + tts)
        }

   Debug.WriteLine($"[HybridTranslationService] Downloaded pack '{target}' entries: {count}");
    return count;
    }

  private async Task ProcessTranslationAsync(int poiId, string desc, string tts, string target, CancellationToken ct)
 {
        try
        {
  await _apiLimiter.WaitAsync(ct);
  
       var descTranslated = await TranslateCoreWithTimeoutAsync(desc, "vi", target, ct);
       if (!string.IsNullOrWhiteSpace(descTranslated))
            {
        await _poiRepository.UpsertCachedTranslationAsync(poiId, target, isTtsScript: false, descTranslated, isDownloadedPack: true);
            }

     var ttsTranslated = await TranslateCoreWithTimeoutAsync(tts, "vi", target, ct);
    if (!string.IsNullOrWhiteSpace(ttsTranslated))
            {
         await _poiRepository.UpsertCachedTranslationAsync(poiId, target, isTtsScript: true, ttsTranslated, isDownloadedPack: true);
         }
        }
      finally
        {
            _apiLimiter.Release();
        }
    }

    // ? OPTIMIZATION: Add timeout to prevent hanging API calls
    private async Task<string> TranslateCoreWithTimeoutAsync(
        string text, 
        string sourceLanguage, 
        string targetLanguage,
     CancellationToken? cancellationToken = null)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken ?? CancellationToken.None);
            cts.CancelAfter(ApiTimeout);

    var result = await _translator.TranslateAsync(text, sourceLanguage, targetLanguage);
 if (!string.IsNullOrWhiteSpace(result?.Translation))
             return result.Translation;
 }
        catch (OperationCanceledException)
     {
Debug.WriteLine($"?? [HybridTranslationService] Translation timeout ({sourceLanguage}->{targetLanguage})");
   return text;
}
        catch (Exception ex)
        {
            Debug.WriteLine($"[HybridTranslationService] Translate error ({sourceLanguage}->{targetLanguage}): {ex.Message}");
    }

      try
     {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken ?? CancellationToken.None);
            cts.CancelAfter(ApiTimeout);

        var retry = await _translator.TranslateAsync(text, null, targetLanguage);
     if (!string.IsNullOrWhiteSpace(retry?.Translation))
          return retry.Translation;
        }
        catch (OperationCanceledException)
        {
   Debug.WriteLine($"?? [HybridTranslationService] Retry translation timeout");
        }
 catch (Exception retryEx)
  {
          Debug.WriteLine($"[HybridTranslationService] Retry failed: {retryEx.Message}");
        }

        return text;
    }

    private bool CanUseOnlineTranslation()
    {
        var key = _configurationService.GetTranslationServiceKey();
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return !string.Equals(key, "YOUR_TRANSLATION_SERVICE_KEY_HERE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInternetAvailable()
    {
        try
        {
       return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildCacheKey(int poiId, string languageCode, bool isTtsScript)
        => $"{poiId}:{languageCode}:{(isTtsScript ? 1 : 0)}";

  private static string NormalizeLang(string? code)
    {
    if (string.IsNullOrWhiteSpace(code)) return "vi";

        var normalized = code.Trim().Replace('_', '-').ToLowerInvariant();

        return normalized switch
        {
            "vn" or "vi" or "vi-vn" => "vi",
        "en" or "en-us" or "en-gb" => "en",
       "zh" or "zh-cn" or "zh-hans" => "zh",
            "zh-tw" or "zh-hant" => "zh",
            "ja" or "ja-jp" => "ja",
  "ko" or "ko-kr" => "ko",
  "fr" or "fr-fr" => "fr",
            "ru" or "ru-ru" => "ru",
  _ when normalized.Length >= 2 && normalized.Contains('-') => normalized[..normalized.IndexOf('-')],
     _ => normalized
        };
    }

    private record CachedTranslation
    {
        public string Value { get; set; } = string.Empty;
   public DateTime CachedAt { get; set; }
    }
}
