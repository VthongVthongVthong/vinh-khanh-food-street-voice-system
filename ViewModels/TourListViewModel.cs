using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels;

public class TourListViewModel : INotifyPropertyChanged
{
    private readonly ITourRepository _tourRepository;
    private readonly LocalizationResourceManager _localizationManager;
    private List<Tour> _tours = new();
    private Tour? _selectedTour;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _isNavigating;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TourListViewModel(ITourRepository tourRepository, LocalizationResourceManager localizationManager)
    {
        _tourRepository = tourRepository;
        _localizationManager = localizationManager;

        LoadToursCommand = new Command(async () => await LoadToursAsync());
        SelectTourCommand = new Command<Tour>(async (tour) => await SelectTourAsync(tour));
    }

    public List<Tour> Tours
    {
        get => _tours;
        set
        {
            if (_tours == value) return;
            _tours = value;
            OnPropertyChanged();
        }
    }

    public Tour? SelectedTour
    {
        get => _selectedTour;
        set
        {
            if (_selectedTour == value) return;
            _selectedTour = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadToursCommand { get; }
    public ICommand SelectTourCommand { get; }

    public async Task LoadToursAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "?ang t?i l? trình...";
            Tours = await _tourRepository.GetAllToursAsync();
            StatusMessage = Tours.Count == 0 ? "Ch?a có l? trình nào" : $"?ã t?i {Tours.Count} l? trình";
            System.Diagnostics.Debug.WriteLine($"[TourListViewModel] Loaded {Tours.Count} tours");
        }
        catch (Exception ex)
        {
            StatusMessage = $"L?i t?i l? trình: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[TourListViewModel] Load error: {ex.Message}");
        }
        finally
        {
            // ? Ensure UI thread + small delay to let CollectionView render before hiding loading
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(200); // Let UI render
                IsLoading = false;
            });
        }
    }

    private async Task SelectTourAsync(Tour tour)
    {
        if (tour == null || _isNavigating) return;

        try
        {
            _isNavigating = true;
            System.Diagnostics.Debug.WriteLine($"[TourListViewModel] Navigating to tour detail: {tour.Id}");

            // ? Clear status message before navigating
            StatusMessage = string.Empty;

            await Shell.Current.GoToAsync($"///tourdetail?tourId={tour.Id}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TourListViewModel] Navigation error: {ex.Message}");
            StatusMessage = $"L?i: {ex.Message}";
        }
        finally
        {
            SelectedTour = null;
            _isNavigating = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}