using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services
{
    public class AudioManager
    {
        private readonly Queue<POI> _audioQueue = new();
        private readonly TextToSpeechService _ttsService;
        private bool _isPlaying = false;
        private POI _currentPOI;

        public event EventHandler<POI> AudioStarted;
        public event EventHandler<POI> AudioCompleted;

        public AudioManager(TextToSpeechService ttsService)
        {
            _ttsService = ttsService;
        }

        public void AddToQueue(POI poi)
        {
            _audioQueue.Enqueue(poi);
            ProcessQueue();
        }

        private async void ProcessQueue()
        {
            if (_isPlaying || _audioQueue.Count == 0)
                return;

            _isPlaying = true;
            _currentPOI = _audioQueue.Dequeue();

            AudioStarted?.Invoke(this, _currentPOI);

            try
            {
                // Check if we have pre-recorded audio
                if (!string.IsNullOrEmpty(_currentPOI.AudioFile) && File.Exists(_currentPOI.AudioFile))
                {
                    // Play MP3 file
                    await PlayAudioFile(_currentPOI.AudioFile);
                }
                else
                {
                    // Use Text-to-Speech
                    var text = string.IsNullOrEmpty(_currentPOI.TtsScript) 
                        ? _currentPOI.DescriptionText 
                        : _currentPOI.TtsScript;

                    await _ttsService.SpeakAsync(text, _currentPOI.Language);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio playback error: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
                AudioCompleted?.Invoke(this, _currentPOI);
                _currentPOI = null;

                // Process next in queue
                ProcessQueue();
            }
        }

        private async Task PlayAudioFile(string filePath)
        {
            // Implement MP3 playback using MediaPlayer
            // This is a simplified version
            await Task.Delay(2000); // Simulate audio playback
        }

        public void StopCurrent()
        {
            _ttsService.Stop();
            _isPlaying = false;
            _audioQueue.Clear();
        }

        public void ClearQueue()
        {
            _audioQueue.Clear();
        }

        public bool IsPlaying => _isPlaying;
        public POI CurrentPOI => _currentPOI;
    }
}
