using System.Diagnostics;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Manages audio playback queue with multilingual support.
    /// - Fetches TTS script from DB (original language)
    /// - Translates if user selected different language
    /// - Caches translation in-memory (5 min TTL)
    /// - Plays via TextToSpeechService
    /// </summary>
    public class AudioManager
    {
        private readonly Queue<POI> _audioQueue = new();
        private readonly TextToSpeechService _ttsService;
        private readonly ITranslationService _translationService;
        private readonly SettingsService _settingsService;
        private readonly SemaphoreSlim _queueLock = new(1, 1);

        private bool _isPlaying;
        private POI? _currentPOI;

        public event EventHandler<POI>? AudioStarted;
        public event EventHandler<POI>? AudioCompleted;

        public AudioManager(TextToSpeechService ttsService, ITranslationService translationService, SettingsService settingsService)
        {
            _ttsService = ttsService;
            _translationService = translationService;
            _settingsService = settingsService;
        }

        public void AddToQueue(POI poi)
        {
            _audioQueue.Enqueue(poi);
            _ = ProcessQueueAsync();
        }

        private async Task ProcessQueueAsync()
        {
            await _queueLock.WaitAsync();
            try
            {
                if (_isPlaying || _audioQueue.Count == 0)
                    return;

                _isPlaying = true;
            }
            finally
            {
                _queueLock.Release();
            }

            while (true)
            {
                POI? poi;

                await _queueLock.WaitAsync();
                try
                {
                    if (_audioQueue.Count == 0)
                    {
                        _isPlaying = false;
                        _currentPOI = null;
                        return;
                    }

                    poi = _audioQueue.Dequeue();
                    _currentPOI = poi;
                }
                finally
                {
                    _queueLock.Release();
                }

                if (poi is null)
                    continue;

                AudioStarted?.Invoke(this, poi);

                try
                {
                    if (!string.IsNullOrEmpty(poi.AudioFile) && File.Exists(poi.AudioFile))
                    {
                        await PlayAudioFile(poi.AudioFile);
                    }
                    else
                    {
                        // Get text to speak
                        var textToSpeak = string.IsNullOrEmpty(poi.TtsScript) ? poi.DescriptionText : poi.TtsScript;

                        // Get user's selected narration language
                        var userLanguage = _settingsService.PreferredLanguage;

                        // Translate if needed
                        var finalText = await GetTextForLanguageAsync(poi, textToSpeak, userLanguage);

                        // Speak with user's selected language
                        await _ttsService.SpeakAsync(finalText, userLanguage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Audio playback error: {ex}");
                }
                finally
                {
                    AudioCompleted?.Invoke(this, poi);
                }
            }
        }

        /// <summary>
        /// Get text in the requested language (with caching)
        /// </summary>
        private async Task<string> GetTextForLanguageAsync(POI poi, string originalText, string targetLanguage)
        {
            var normalizedTarget = NormalizeLang(targetLanguage);
            var sourceLang = NormalizeLang(poi.TtsLanguage);

            // If target is POI's default language or Vietnamese, use original
            if (string.Equals(sourceLang, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedTarget, "vi", StringComparison.OrdinalIgnoreCase))
            {
                return originalText;
            }

            // Check cache (in-memory, 5 min TTL)
            if (!string.IsNullOrEmpty(poi.CachedTranslatedTtsScript))
            {
                if (DateTime.UtcNow - poi.CachedTranslationTime < TimeSpan.FromMinutes(5))
                {
                    Debug.WriteLine($"[AudioManager] Using cached translation for {poi.Name}");
                    return poi.CachedTranslatedTtsScript;
                }
            }

            // Translate via API
            try
            {
                var translatedText = await _translationService.TranslateAsync(
                    originalText,
                    sourceLang,
                    normalizedTarget
                );

                // Cache in-memory
                poi.CachedTranslatedTtsScript = translatedText;
                poi.CachedTranslationTime = DateTime.UtcNow;

                Debug.WriteLine($"[AudioManager] Translated '{poi.Name}' to {normalizedTarget}");
                return translatedText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Translation failed, using original: {ex.Message}");
                return originalText; // Fallback to original
            }
        }

        private static string NormalizeLang(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi";
            var trimmed = code.Trim();
            var dashIndex = trimmed.IndexOf('-');
            return dashIndex > 0 ? trimmed[..dashIndex] : trimmed;
        }

        private static async Task PlayAudioFile(string filePath)
        {
            // TODO: replace with real player
            await Task.Delay(2000);
        }

        public void StopCurrent()
        {
            _ttsService.Stop();
            _audioQueue.Clear();
            _isPlaying = false;
            _currentPOI = null;
        }

        public void ClearQueue() => _audioQueue.Clear();

        public bool IsPlaying => _isPlaying;
        public POI? CurrentPOI => _currentPOI;
    }
}
