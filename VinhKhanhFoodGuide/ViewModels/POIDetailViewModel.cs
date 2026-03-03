using System.ComponentModel;
using System.Runtime.CompilerServices;
using VinhKhanhFoodGuide.Models;
using VinhKhanhFoodGuide.Services;
using VinhKhanhFoodGuide.Data;

namespace VinhKhanhFoodGuide.ViewModels;

public class POIDetailViewModel : BaseViewModel
{
    private readonly IAudioManager _audioManager;
    private readonly IPoiRepository _repository;
    private POI _poi;
    private POIContent _currentContent;
    private bool _isPlaying = false;
    private string _selectedLanguage = "en";

    public POI Poi
    {
        get => _poi;
        set => SetProperty(ref _poi, value);
    }

    public POIContent CurrentContent
    {
        get => _currentContent;
        set => SetProperty(ref _currentContent, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                _ = LoadContentForLanguageAsync(value);
            }
        }
    }

    public POIDetailViewModel(IAudioManager audioManager, IPoiRepository repository)
    {
        _audioManager = audioManager;
        _repository = repository;
    }

    public async Task LoadPoiAsync(int poiId)
    {
        try
        {
            Poi = await _repository.GetPoiByIdAsync(poiId);
            await LoadContentForLanguageAsync(_selectedLanguage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Load POI error: {ex.Message}");
        }
    }

    private async Task LoadContentForLanguageAsync(string languageCode)
    {
        try
        {
            if (Poi == null) return;
            CurrentContent = await _repository.GetPoiContentByLanguageAsync(Poi.Id, languageCode);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Load content error: {ex.Message}");
        }
    }

    public async Task PlayAudioAsync()
    {
        try
        {
            if (CurrentContent == null) return;

            IsPlaying = true;

            if (!string.IsNullOrEmpty(CurrentContent.AudioPath))
            {
                await _audioManager.PlayAudioFileAsync(CurrentContent.AudioPath);
            }
            else if (CurrentContent.UseTextToSpeech)
            {
                await _audioManager.PlayTextToSpeechAsync(CurrentContent.TextContent, CurrentContent.LanguageCode);
            }

            IsPlaying = false;
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            Debug.WriteLine($"Play audio error: {ex.Message}");
        }
    }

    public async Task StopAudioAsync()
    {
        try
        {
            await _audioManager.StopAsync();
            IsPlaying = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Stop audio error: {ex.Message}");
        }
    }
}
