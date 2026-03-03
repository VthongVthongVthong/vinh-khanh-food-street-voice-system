namespace VinhKhanhFoodGuide.Services;

public interface IAudioManager
{
    Task PlayAudioFileAsync(string filePath);
    Task PlayTextToSpeechAsync(string text, string languageCode = "en");
    Task StopAsync();
    Task PauseAsync();
    Task ResumeAsync();
    bool IsPlaying { get; }
}

public class AudioManager : IAudioManager
{
    private TaskCompletionSource<bool> _playbackTask;
    private bool _isPlaying = false;
    private Queue<Func<Task>> _audioQueue = new();
    private bool _isProcessingQueue = false;

    public bool IsPlaying => _isPlaying;

    public async Task PlayAudioFileAsync(string filePath)
    {
        _audioQueue.Enqueue(async () => await _PlayAudioFileInternalAsync(filePath));
        await ProcessQueueAsync();
    }

    public async Task PlayTextToSpeechAsync(string text, string languageCode = "en")
    {
        _audioQueue.Enqueue(async () => await _PlayTextToSpeechInternalAsync(text, languageCode));
        await ProcessQueueAsync();
    }

    public async Task StopAsync()
    {
        _isPlaying = false;
        _audioQueue.Clear();
        
        try
        {
            await TextToSpeech.StopAsync();
        }
        catch { }
    }

    public async Task PauseAsync()
    {
        // Note: MAUI TextToSpeech doesn't natively support pause,
        // so we'll stop and user can resume from queue
        await StopAsync();
    }

    public async Task ResumeAsync()
    {
        // Resume is implicit when next item is played
        await ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessingQueue) return;

        _isProcessingQueue = true;

        while (_audioQueue.Count > 0 && !_isPlaying)
        {
            var audioAction = _audioQueue.Dequeue();
            try
            {
                await audioAction();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio playback error: {ex.Message}");
            }
        }

        _isProcessingQueue = false;
    }

    private async Task _PlayAudioFileInternalAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Audio file not found: {filePath}");
            }

            _isPlaying = true;
            _playbackTask = new TaskCompletionSource<bool>();

            // For now, use basic file playback - extended implementation would use MediaElement
            // This is a placeholder demonstrating the architecture
            Debug.WriteLine($"Playing audio file: {filePath}");
            
            // Simulate playback duration
            await Task.Delay(3000);
            _isPlaying = false;
            _playbackTask.SetResult(true);
        }
        catch (Exception ex)
        {
            _isPlaying = false;
            Debug.WriteLine($"Audio file playback error: {ex.Message}");
        }
    }

    private async Task _PlayTextToSpeechInternalAsync(string text, string languageCode = "en")
    {
        try
        {
            _isPlaying = true;
            
            var settings = new SpeechOptions
            {
                Volume = 1.0f,
                Pitch = 1.0f,
                Locale = GetLocale(languageCode)
            };

            await TextToSpeech.SpeakAsync(text, settings);
            _isPlaying = false;
        }
        catch (Exception ex)
        {
            _isPlaying = false;
            Debug.WriteLine($"TTS error: {ex.Message}");
        }
    }

    private string GetLocale(string languageCode)
    {
        return languageCode switch
        {
            "vi" => "vi-VN",
            "en" => "en-US",
            "fr" => "fr-FR",
            "zh" => "zh-CN",
            _ => "en-US"
        };
    }
}
