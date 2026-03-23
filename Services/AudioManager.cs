using System.Diagnostics;
using System.Threading;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Manages audio playback queue with multilingual support.
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
        private CancellationTokenSource? _playbackCts;

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

                _playbackCts = new CancellationTokenSource();

                try
                {
                    if (!string.IsNullOrEmpty(poi.AudioFile) && File.Exists(poi.AudioFile))
                    {
                        await PlayAudioFile(poi.AudioFile, _playbackCts.Token);
                    }
                    else
                    {
                        var textToSpeak = string.IsNullOrEmpty(poi.TtsScript) ? poi.DescriptionText : poi.TtsScript;
                        var userLanguage = _settingsService.PreferredLanguage;
                        var finalText = await GetTextForLanguageAsync(poi, textToSpeak, userLanguage);

                        await _ttsService.SpeakAsync(finalText, userLanguage, _playbackCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("[AudioManager] Playback canceled");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Audio playback error: {ex}");
                }
                finally
                {
                    AudioCompleted?.Invoke(this, poi);
                    _playbackCts?.Dispose();
                    _playbackCts = null;
                }
            }
        }

        private async Task<string> GetTextForLanguageAsync(POI poi, string originalText, string targetLanguage)
        {
            var normalizedTarget = NormalizeLang(targetLanguage);
            var sourceLang = NormalizeLang(poi.TtsLanguage);

            if (string.Equals(sourceLang, normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedTarget, "vi", StringComparison.OrdinalIgnoreCase))
            {
                return originalText;
            }

            if (!string.IsNullOrEmpty(poi.CachedTranslatedTtsScript))
            {
                if (DateTime.UtcNow - poi.CachedTranslationTime < TimeSpan.FromMinutes(5))
                {
                    Debug.WriteLine($"[AudioManager] Using cached translation for {poi.Name}");
                    return poi.CachedTranslatedTtsScript;
                }
            }

            try
            {
                var translatedText = await _translationService.TranslateAsync(
                    originalText,
                    sourceLang,
                    normalizedTarget
                );

                poi.CachedTranslatedTtsScript = translatedText;
                poi.CachedTranslationTime = DateTime.UtcNow;

                Debug.WriteLine($"[AudioManager] Translated '{poi.Name}' to {normalizedTarget}");
                return translatedText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioManager] Translation failed, using original: {ex.Message}");
                return originalText;
            }
        }

        private static string NormalizeLang(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "vi";
            var trimmed = code.Trim();
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
            _audioQueue.Clear();
            _isPlaying = false;
            _currentPOI = null;
        }

        public void ClearQueue() => _audioQueue.Clear();

        public bool IsPlaying => _isPlaying;
        public POI? CurrentPOI => _currentPOI;
    }
}
