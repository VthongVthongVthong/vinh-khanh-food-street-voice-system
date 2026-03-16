using System.Diagnostics;
using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class AudioManager
    {
        private readonly Queue<POI> _audioQueue = new();
        private readonly TextToSpeechService _ttsService;
        private readonly SemaphoreSlim _queueLock = new(1, 1);

        private bool _isPlaying;
        private POI? _currentPOI;

        public event EventHandler<POI>? AudioStarted;
        public event EventHandler<POI>? AudioCompleted;

        public AudioManager(TextToSpeechService ttsService)
        {
            _ttsService = ttsService;
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
                        await PlayAudioFile(poi.AudioFile);
                    else
                    {
                        var text = string.IsNullOrEmpty(poi.TtsScript) ? poi.DescriptionText : poi.TtsScript;
                        await _ttsService.SpeakAsync(text, poi.Language);
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

        private static async Task PlayAudioFile(string filePath)
        {
            // TODO: replace with real player later
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
