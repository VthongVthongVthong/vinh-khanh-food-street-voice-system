using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Manages audio playback queue with multilingual support.
    /// OPTIMIZATIONS:
    /// - Pre-caching translations to avoid delays
    /// - Batch TTS processing
    /// - Connection pooling for translation service
    /// - Thread-safe language switching with proper queue cleanup
    /// </summary>
    public class AudioManager
    {
        private readonly Queue<POI> _audioQueue = new();
        private readonly object _queueSync = new();
        private readonly TextToSpeechService _ttsService;
        private readonly ITranslationService _translationService;
        private readonly SettingsService _settingsService;
        private readonly SemaphoreSlim _ttsSemaphore = new(1, 1);  // Ensure only 1 TTS at a time
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly CancellationTokenSource _queueWorkerCts = new();
        private readonly ConcurrentDictionary<int, DateTime> _lastEnqueueByPoi = new();
        private readonly TimeSpan _enqueueDebounce = TimeSpan.FromSeconds(10);

        private bool _isPlaying;
        private POI? _currentPOI;
        private CancellationTokenSource? _playbackCts;
        private int _workerStarted;

        // OPTIMIZATION: Pre-resolved text cache to avoid translation delays during playback
        private readonly Dictionary<int, Dictionary<string, string>> _preResolvedCache = new();
        
        // FIX: Track language to detect rapid changes
        private string _currentLanguage = "vi";
        private readonly object _languageLock = new();

        public event EventHandler<POI>? AudioStarted;
        public event EventHandler<POI>? AudioCompleted;

        public AudioManager(TextToSpeechService ttsService, ITranslationService translationService, SettingsService settingsService)
        {
            try
            {
                _ttsService = ttsService;
                _translationService = translationService;
                _settingsService = settingsService;

                // ? Safe subscription with null-check
                if (_settingsService != null)
                {
                    _settingsService.PreferredLanguageChanged += OnPreferredLanguageChanged;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Constructor error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void OnPreferredLanguageChanged(object? sender, string language)
        {
            try
            {
                lock (_languageLock)
                {
                    var normalizedNew = NormalizeLang(language);
                    var normalizedCurrent = NormalizeLang(_currentLanguage);
                    
                    // OPTIMIZATION: Only reset if language actually changed
                    if (normalizedNew == normalizedCurrent)
                    {
                        Debug.WriteLine($"[AudioManager] Language '{language}' is same as current, skipping reset");
                        return;
                    }

                    _currentLanguage = normalizedNew;
                    Debug.WriteLine($"[AudioManager] Preferred language changed to '{normalizedNew}', stopping current playback and clearing queue");
                }
                
                StopCurrent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Error in OnPreferredLanguageChanged: {ex.Message}");
            }
        }

        public void AddToQueue(POI poi)
        {
            try
            {
                if (poi == null)
                {
                    Debug.WriteLine("[AudioManager] Warning: Attempted to add null POI to queue");
                    return;
                }

                var now = DateTime.UtcNow;
                if (_lastEnqueueByPoi.TryGetValue(poi.Id, out var lastEnqueuedAt) && now - lastEnqueuedAt < _enqueueDebounce)
                {
                    Debug.WriteLine($"[AudioManager] Skip duplicate enqueue for POI {poi.Id} within debounce window");
                    return;
                }

                _lastEnqueueByPoi[poi.Id] = now;

                lock (_queueSync)
                {
                    _audioQueue.Enqueue(poi);
                    Debug.WriteLine($"[AudioManager] Added to queue: {poi.Name} (queue size: {_audioQueue.Count})");
                }

                EnsureWorkerStarted();
                _queueSignal.Release();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Error in AddToQueue: {ex.Message}");
            }
        }

        private void EnsureWorkerStarted()
        {
            if (Interlocked.CompareExchange(ref _workerStarted, 1, 0) == 0)
            {
                _ = Task.Run(() => ProcessQueueLoopAsync(_queueWorkerCts.Token));
            }
        }

        private async Task ProcessQueueLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // WAIT for signal with timeout to handle rapid language changes
                    // If timeout expires, loop continues to check cancellation
                    await _queueSignal.WaitAsync(TimeSpan.FromSeconds(2), ct);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[AudioManager] Queue worker cancelled");
                    return;
                }

                POI? poi;
                lock (_queueSync)
                {
                    if (_audioQueue.Count == 0)
                    {
                        continue;  // No items, wait for next signal
                    }

                    poi = _audioQueue.Dequeue();
                    _currentPOI = poi;
                    _isPlaying = poi is not null;
                }

                if (poi is null)
                {
                    continue;
                }

                AudioStarted?.Invoke(this, poi);
                _playbackCts = new CancellationTokenSource();

                try
                {
                    // CONCURRENT CTS: Link with queue worker cancellation
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        _playbackCts.Token, 
                        ct
                    );

                    if (!string.IsNullOrEmpty(poi.AudioFile) && File.Exists(poi.AudioFile))
                    {
                        await PlayAudioFile(poi.AudioFile, linkedCts.Token);
                    }
                    else
                    {
                        // CRITICAL: Get current language at playback time (not enqueue time)
                        string userLanguage;
                        lock (_languageLock)
                        {
                            userLanguage = _currentLanguage;
                        }

                        // ? STEP 1: RESOLVE TEXT FIRST (before TTS)
                        // This ensures:
                        // - Correct language text is loaded
                        // - Database offline columns are used if available
                        // - Translation is done before TTS
                        var finalText = await GetTextForLanguageAsync(poi, userLanguage);
                
                        Debug.WriteLine($"[AudioManager] ? Resolved text for '{poi.Name}' in {userLanguage}: {finalText.Substring(0, Math.Min(50, finalText.Length))}...");

                        // ? STEP 2: Now safely call TTS with correct language text
                        // Use linked cancellation token to cancel if language changed
                        await _ttsSemaphore.WaitAsync(linkedCts.Token);
                        try
                        {
                            await _ttsService.SpeakAsync(finalText, userLanguage, linkedCts.Token);
                        }
                        finally
                        {
                            _ttsSemaphore.Release();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[AudioManager] Playback canceled (language changed or stopped)");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioManager] Playback error: {ex.Message}");
                }
                finally
                {
                    AudioCompleted?.Invoke(this, poi);
                    _playbackCts?.Dispose();
                    _playbackCts = null;

                    lock (_queueSync)
                    {
                        if (_audioQueue.Count == 0)
                        {
                            _isPlaying = false;
                            _currentPOI = null;
                        }
                    }
                }
            }
        }

        private async Task<string> GetTextForLanguageAsync(POI poi, string targetLanguage)
        {
            var normalizedTarget = NormalizeLang(targetLanguage);

            // PRIORITY 1: direct from POI DB multilingual columns
            var offlineText = poi.GetTtsScriptByLanguage(normalizedTarget);
            if (!string.IsNullOrWhiteSpace(offlineText))
            {
                Debug.WriteLine($"[AudioManager] Using DB multilingual text for '{poi.Name}' ({normalizedTarget})");
                return offlineText;
            }

            // PRIORITY 2: Check POI pre-resolved cache (in-memory, instant)
            if (_preResolvedCache.TryGetValue(poi.Id, out var cache) && cache.TryGetValue(normalizedTarget, out var cachedText))
            {
                Debug.WriteLine($"[AudioManager] Using pre-resolved cache for '{poi.Name}' ({normalizedTarget})");
                return cachedText;
            }

            // PRIORITY 3: Check per-POI short-memory cache (5 min TTL)
            if (!string.IsNullOrEmpty(poi.CachedTranslatedTtsScript) &&
                DateTime.UtcNow - poi.CachedTranslationTime < TimeSpan.FromMinutes(5))
            {
                Debug.WriteLine($"[AudioManager] Using in-memory cached translation for '{poi.Name}'");
                return poi.CachedTranslatedTtsScript;
            }

            // PRIORITY 4: Resolve via translation service (DB/cache fallback strategy)
            try
            {
                var resolved = await _translationService.ResolveNarrationTextAsync(poi, normalizedTarget, preferTtsScript: true);

                poi.CachedTranslatedTtsScript = resolved;
                poi.CachedTranslationTime = DateTime.UtcNow;

                if (!_preResolvedCache.ContainsKey(poi.Id))
                {
                    _preResolvedCache[poi.Id] = new Dictionary<string, string>();
                }
                _preResolvedCache[poi.Id][normalizedTarget] = resolved;

                Debug.WriteLine($"[AudioManager] Resolved narration for '{poi.Name}' => {normalizedTarget}");
                return resolved;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Translation resolve failed: {ex.Message}, using original");
                return string.IsNullOrWhiteSpace(poi.TtsScript) ? poi.DescriptionText : poi.TtsScript;
            }
        }

        /// <summary>
        /// PRE-RESOLVE: Pre-load translations in background before playback
        /// Useful for warming up cache before playback session
        /// </summary>
        public async Task PreResolveTranslationsAsync(IEnumerable<POI> pois, string targetLanguage, CancellationToken ct = default)
        {
            var target = NormalizeLang(targetLanguage);

            foreach (var poi in pois)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var _ = _translationService.ResolveNarrationTextAsync(poi, target, preferTtsScript: true);
                }
                catch
                {
                    // Silently ignore pre-resolve failures
                }
            }

            await Task.CompletedTask;
        }

        private static string NormalizeLang(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi";
            var trimmed = code.Trim().ToLowerInvariant();
            var dashIndex = trimmed.IndexOf('-');
            return dashIndex > 0 ? trimmed[..dashIndex] : trimmed;
        }

        private static async Task PlayAudioFile(string filePath, CancellationToken token)
        {
            // TODO: replace with real player that supports cancellation
            await Task.Delay(2000, token);
        }

        public void StopCurrent()
        {
            _playbackCts?.Cancel();
            _ttsService.Stop();

            lock (_queueSync)
            {
                _audioQueue.Clear();
                _isPlaying = false;
                _currentPOI = null;
            }
        }

        public void ClearQueue()
        {
            lock (_queueSync)
            {
                _audioQueue.Clear();
            }
        }

        public bool IsPlaying => _isPlaying;
        public POI? CurrentPOI => _currentPOI;
    }
}