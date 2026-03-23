using VinhKhanhstreetfoods.Models;

namespace VinhKhanhstreetfoods.Services;

public interface ITourRepository
{
    Task InitializeAsync();
    Task<List<Tour>> GetAllToursAsync();
    Task<Tour?> GetTourByIdAsync(int id);
    Task<List<TourPOI>> GetTourPOIsAsync(int tourId);
    Task<int> AddTourAsync(Tour tour);
    Task<int> AddTourPOIAsync(TourPOI tourPoi);
}
