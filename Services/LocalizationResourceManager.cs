using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace VinhKhanhstreetfoods.Services
{
    public class LocalizationResourceManager
    {
        private static LocalizationResourceManager? _instance;
        private readonly object _syncRoot = new();
        private Dictionary<string, string> _currentResources = new();
        private string _currentLanguage = "vi";
        private readonly Dictionary<string, Dictionary<string, string>> _resourceCache = new();
        private readonly ConcurrentDictionary<string, Task> _inflightLoads = new();
        private int _isWarmupQueued;
        private int _isPrefetching;
        private int _isCachingAllLanguages;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly Assembly ThisAssembly = typeof(LocalizationResourceManager).Assembly;
        private static readonly Dictionary<string, string> EmbeddedResourceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = "VinhKhanhstreetfoods.Resources.Localization.strings-vi.json",
            ["en"] = "VinhKhanhstreetfoods.Resources.Localization.strings-en.json",
            ["zh"] = "VinhKhanhstreetfoods.Resources.Localization.strings-zh.json",
            ["ja"] = "VinhKhanhstreetfoods.Resources.Localization.strings-ja.json",
            ["ko"] = "VinhKhanhstreetfoods.Resources.Localization.strings-ko.json",
            ["fr"] = "VinhKhanhstreetfoods.Resources.Localization.strings-fr.json",
            ["ru"] = "VinhKhanhstreetfoods.Resources.Localization.strings-ru.json"
        };

        public static LocalizationResourceManager Instance => _instance ??= new LocalizationResourceManager();

        public LocalizationResourceManager()
        {
            try
            {
                var preferred = Preferences.Get("app_language", "vi");
                _currentLanguage = NormalizeLanguage(preferred);
            }
            catch
            {
                _currentLanguage = "vi";
            }
        }

        /// <summary>
        /// Warm cache only. Does NOT change active language.
        /// </summary>
        public void WarmCacheWithDefaultLanguage()
        {
            _ = EnsureLanguageLoadedAsync("vi");
        }

        /// <summary>
        /// Prefetch + activate preferred language.
        /// </summary>
        public async Task PrefetchPreferredLanguageAsync(string preferredLanguage)
        {
            if (Interlocked.CompareExchange(ref _isPrefetching, 1, 0) != 0)
                return;

            try
            {
                await SetActiveLanguageAsync(preferredLanguage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationResourceManager] Prefetch error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isPrefetching, 0);
            }
        }

        /// <summary>
        /// Cache all supported languages in background. Does NOT change active language.
        /// </summary>
        public async Task CacheAllLanguagesAsync()
        {
            if (Interlocked.CompareExchange(ref _isCachingAllLanguages, 1, 0) != 0)
                return;

            try
            {
                var languages = new[] { "vi", "en", "zh", "ja", "ko", "fr", "ru" };
                foreach (var lang in languages)
                {
                    await EnsureLanguageLoadedAsync(lang).ConfigureAwait(false);
                    // Keep this conservative to reduce startup CPU/disk bursts on Android
                    await Task.Delay(120).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationResourceManager] CacheAllLanguages error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isCachingAllLanguages, 0);
            }
        }

        /// <summary>
        /// Legacy compatibility method: set active language resources.
        /// </summary>
        public void LoadResourcesForLanguage(string languageCode)
        {
            if (MainThread.IsMainThread)
            {
                _ = SetActiveLanguageAsync(languageCode);
                return;
            }

            SetActiveLanguageAsync(languageCode).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensure resources are loaded and set as active language.
        /// </summary>
        public async Task SetActiveLanguageAsync(string languageCode)
        {
            var normalizedLanguage = NormalizeLanguage(languageCode);
            await EnsureLanguageLoadedAsync(normalizedLanguage).ConfigureAwait(false);

            lock (_syncRoot)
            {
                if (_resourceCache.TryGetValue(normalizedLanguage, out var cached))
                {
                    _currentResources = cached;
                    _currentLanguage = normalizedLanguage;
                }
            }
        }

        /// <summary>
        /// Ensure resources are loaded into cache only.
        /// </summary>
        public async Task EnsureLanguageLoadedAsync(string languageCode)
        {
            var normalizedLanguage = NormalizeLanguage(languageCode);

            lock (_syncRoot)
            {
                if (_resourceCache.ContainsKey(normalizedLanguage))
                {
                    return;
                }
            }

            var loadTask = _inflightLoads.GetOrAdd(normalizedLanguage, _ => LoadAndCacheLanguageAsync(normalizedLanguage));
            try
            {
                await loadTask.ConfigureAwait(false);
            }
            finally
            {
                _inflightLoads.TryRemove(normalizedLanguage, out _);
            }
        }

        private async Task LoadAndCacheLanguageAsync(string normalizedLanguage)
        {
            try
            {
                var resourceText = await LoadJsonFileAsync(normalizedLanguage).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(resourceText))
                {
                    if (normalizedLanguage != "vi")
                    {
                        await EnsureLanguageLoadedAsync("vi").ConfigureAwait(false);
                    }
                    return;
                }

                // Keep deserialize off UI thread.
                var resources = await Task.Run(() =>
                    JsonSerializer.Deserialize<Dictionary<string, string>>(resourceText, JsonOptions)
                    ?? new Dictionary<string, string>()).ConfigureAwait(false);

                lock (_syncRoot)
                {
                    _resourceCache[normalizedLanguage] = resources;

                    // First load only: provide immediate active resources
                    if (_currentResources.Count == 0)
                    {
                        _currentResources = resources;
                        _currentLanguage = normalizedLanguage;
                    }
                }

                Debug.WriteLine($"[LocalizationResourceManager] Loaded resources for: {normalizedLanguage} ({resources.Count} keys)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationResourceManager] Error loading resources for '{normalizedLanguage}': {ex.Message}");
            }
        }

        public string GetString(string key)
        {
            try
            {
                if (_currentResources.Count == 0)
                {
                    if (Interlocked.CompareExchange(ref _isWarmupQueued, 1, 0) == 0)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SetActiveLanguageAsync(_currentLanguage).ConfigureAwait(false);
                            }
                            finally
                            {
                                Interlocked.Exchange(ref _isWarmupQueued, 0);
                            }
                        });
                    }

                    return key;
                }

                if (_currentResources.TryGetValue(key, out var value))
                    return value;

                return key;
            }
            catch
            {
                return key;
            }
        }

        public string GetString(string key, params object?[] args)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, GetString(key), args);
            }
            catch
            {
                return key;
            }
        }

        public string CurrentLanguage => _currentLanguage;

        public bool IsLanguageCached(string languageCode)
        {
            var normalized = NormalizeLanguage(languageCode);
            lock (_syncRoot)
            {
                return _resourceCache.ContainsKey(normalized);
            }
        }

        public int GetCachedLanguageCount()
        {
            lock (_syncRoot)
            {
                return _resourceCache.Count;
            }
        }

        private static string NormalizeLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return "vi";

            var normalized = languageCode.Trim().ToLowerInvariant().Replace('_', '-');
            var shortCode = normalized.Contains('-') ? normalized[..normalized.IndexOf('-')] : normalized;

            return shortCode switch
            {
                "vi" or "vn" => "vi",
                "en" => "en",
                "zh" => "zh",
                "ja" => "ja",
                "ko" => "ko",
                "fr" => "fr",
                "ru" => "ru",
                _ => "vi"
            };
        }

        private static async Task<string> LoadJsonFileAsync(string normalizedLanguage)
        {
            // 1) EmbeddedResource first
            var embedded = await TryLoadFromEmbeddedResourceAsync(normalizedLanguage).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(embedded))
                return embedded;

            // 2) MauiAsset fallback
            var candidates = new[]
            {
                $"Resources/Localization/strings-{normalizedLanguage}.json",
                $"Localization/strings-{normalizedLanguage}.json",
                $"strings-{normalizedLanguage}.json"
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    await using var stream = await FileSystem.OpenAppPackageFileAsync(candidate).ConfigureAwait(false);
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var content = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        Debug.WriteLine($"[LocalizationResourceManager] Loaded JSON from MauiAsset: {candidate}");
                        return content;
                    }
                }
                catch
                {
                    // try next candidate
                }
            }

            return string.Empty;
        }

        private static async Task<string> TryLoadFromEmbeddedResourceAsync(string normalizedLanguage)
        {
            try
            {
                if (!EmbeddedResourceNames.TryGetValue(normalizedLanguage, out var resourceName))
                    return string.Empty;

                await using var stream = ThisAssembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                    return string.Empty;

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    Debug.WriteLine($"[LocalizationResourceManager] Loaded JSON from EmbeddedResource: {resourceName}");
                    return content;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationResourceManager] EmbeddedResource load error ({normalizedLanguage}): {ex.Message}");
            }

            return string.Empty;
        }
    }
}
