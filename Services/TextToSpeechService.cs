namespace VinhKhanhstreetfoods.Services
{
    public class TextToSpeechService
    {
        private bool _isPlaying = false;

        public async Task SpeakAsync(string text, string language = "vi-VN")
        {
            if (string.IsNullOrEmpty(text))
                return;

            _isPlaying = true;

            try
            {
                var settings = new SpeechOptions()
                {
                    Volume = 1.0f,
                    Pitch = 1.0f
                };

                await TextToSpeech.SpeakAsync(text, settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Text-to-Speech error: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
            }
        }

        public void Stop()
        {
            // TextToSpeech doesn't have a built-in stop method in MAUI
            // This would need to be implemented via platform-specific code
            _isPlaying = false;
        }

        public bool IsPlaying => _isPlaying;
    }
}
