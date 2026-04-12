using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VinhKhanhstreetfoods.Models;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly LocationService _locationService;
        private readonly GeofenceEngine _geofenceEngine;
        private readonly IPOIRepository _poiRepository;
        private readonly AudioManager _audioManager;

        private readonly List<POI> _allPOIs = new();
        private ObservableCollection<POI> _nearbyPOIs;
        private string _statusMessage;
        private bool _isLocationServiceRunning;
        private double _userLatitude;
        private double _userLongitude;
        private POI? _selectedPOI;
        private bool _isNavigating;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private int _isSyncingFromAdmin;
        private bool _isRefreshing;

        public event PropertyChangedEventHandler? PropertyChanged;

        public HomeViewModel(
            LocationService locationService,
            GeofenceEngine geofenceEngine,
            IPOIRepository poiRepository,
            AudioManager audioManager)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _geofenceEngine = geofenceEngine ?? throw new ArgumentNullException(nameof(geofenceEngine));
            _poiRepository = poiRepository ?? throw new ArgumentNullException(nameof(poiRepository));
            _audioManager = audioManager ?? throw new ArgumentNullException(nameof(audioManager));

            NearbyPOIs = new ObservableCollection<POI>();
            StatusMessage = LocalizationService.GetString("Home_Status_Ready") ?? "App ready. Press START to track location.";
            IsLoading = false;

            StartLocationServiceCommand = new Command(async () => await StartLocationService());
            StopLocationServiceCommand = new Command(async () => await StopLocationService());
            OpenDetailCommand = new Command<POI>(async poi => await OpenDetailAsync(poi));
            RefreshDataCommand = new Command(async () => await RefreshDataAsync(), () => !IsRefreshing);
            RefreshAppCommand = new Command(async () => await RefreshAppAsync(), () => !IsRefreshing);

            _locationService.LocationUpdated += OnLocationUpdated;
            _geofenceEngine.POITriggered += OnPOITriggered;
            _audioManager.AudioStarted += OnAudioStarted;
            _audioManager.AudioCompleted += OnAudioCompleted;
            _poiRepository.POIsSynced += OnRepositoryPoisSynced;
        }

        public ObservableCollection<POI> NearbyPOIs
        {
            get => _nearbyPOIs;
            set { _nearbyPOIs = value; OnPropertyChanged(); }
        }

        public POI? SelectedPOI
        {
            get => _selectedPOI;
            set
            {
                if (Equals(_selectedPOI, value))
                    return;

                _selectedPOI = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLocationServiceRunning
        {
            get => _isLocationServiceRunning;
            set { _isLocationServiceRunning = value; OnPropertyChanged(); }
        }

        public double UserLatitude
        {
            get => _userLatitude;
            set { _userLatitude = value; OnPropertyChanged(); }
        }

        public double UserLongitude
        {
            get => _userLongitude;
            set { _userLongitude = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing == value)
                    return;

                _isRefreshing = value;
                OnPropertyChanged();
                if (RefreshDataCommand is Command command)
                    command.ChangeCanExecute();
            }
        }

        public ICommand StartLocationServiceCommand { get; }
        public ICommand StopLocationServiceCommand { get; }
        public ICommand OpenDetailCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand RefreshAppCommand { get; }

        public async Task EnsureInitialDataLoadedAsync()
        {
            if (_allPOIs.Count > 0 || IsLoading)
                return;

            await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = LocalizationService.GetString("Home_Status_Loading") ?? "Loading data...";

                if (_poiRepository == null)
                {
                    StatusMessage = LocalizationService.GetString("Home_Status_Error_NoData") ?? "Error: Repository not initialized";
                    IsLoading = false;
                    return;
                }

                var allPOIs = await Task.Run(async () =>
                {
                    if (_poiRepository is POIRepository poiRepo)
                    {
                        await poiRepo.InitializeAsync();
                    }
                    return await _poiRepository.GetActivePOIsAsync();
                });

                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Loaded {allPOIs?.Count ?? 0} active POIs from database");

                if (allPOIs == null || allPOIs.Count == 0)
                {
                    StatusMessage = LocalizationService.GetString("Home_Status_Error_NoData") ?? "No restaurants. Check database data.";
                    IsLoading = false;
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allPOIs.Clear();
                    _allPOIs.AddRange(allPOIs);
                    ApplyFilter();
                    StatusMessage = LocalizationService.GetString("Home_Status_Loaded")?.Replace("{0}", allPOIs.Count.ToString())
                        ?? $"Loaded {allPOIs.Count} restaurants. Press START to begin.";
                });

                _ = TrySyncFromAdminInBackgroundAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Error loading initial data: {ex.Message}\n{ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    StatusMessage = $"Error loading data: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task TrySyncFromAdminInBackgroundAsync()
        {
            if (Interlocked.Exchange(ref _isSyncingFromAdmin, 1) == 1)
                return;

            try
            {
                var updatedCount = await _poiRepository.SyncPOIsFromAdminAsync();
                if (updatedCount <= 0)
                    return;

                var refreshed = await _poiRepository.GetActivePOIsAsync();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allPOIs.Clear();
                    _allPOIs.AddRange(refreshed);
                    ApplyFilter();
                    StatusMessage = LocalizationService.GetString("Home_Status_Synced")?.Replace("{0}", updatedCount.ToString())
                        ?? $"Synced {updatedCount} new locations from server.";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Admin sync skipped/error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isSyncingFromAdmin, 0);
            }
        }

        private async Task RefreshDataAsync()
        {
            if (IsRefreshing)
                return;

            try
            {
                IsRefreshing = true;
                StatusMessage = LocalizationService.GetString("Home_Status_Loading") ?? "Refreshing...";

                var updatedCount = await _poiRepository.SyncPOIsFromAdminAsync(force: true);
                var refreshed = await _poiRepository.GetActivePOIsAsync();

                await MainThread.InvokeOnMainThreadAsync(() => { if (updatedCount > 0) { _allPOIs.Clear(); _allPOIs.AddRange(refreshed); ApplyFilter(); } StatusMessage = updatedCount > 0 ? LocalizationService.GetString("Home_Status_Synced")?.Replace("{0}", updatedCount.ToString()) ?? $"Updated {updatedCount} locations." : LocalizationService.GetString("Home_Status_NoNew") ?? "No new data."; });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Manual refresh error: {ex.Message}");
                StatusMessage = $"Lỗi: {ex.Message}";
                await Application.Current!.MainPage!.DisplayAlert("Lỗi Đồng Bộ", ex.Message, "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RefreshAppAsync()
        {
            if (IsRefreshing)
                return;

            try
            {
                IsRefreshing = true;
                StatusMessage = LocalizationService.GetString("Home_Status_Loading") ?? "Refreshing app data...";

                // Force data sync
                var updatedCount = await _poiRepository.SyncPOIsFromAdminAsync(force: true);
                var refreshed = await _poiRepository.GetActivePOIsAsync();

                await MainThread.InvokeOnMainThreadAsync(() => { if (updatedCount > 0) { _allPOIs.Clear(); _allPOIs.AddRange(refreshed); ApplyFilter(); } StatusMessage = updatedCount > 0 ? LocalizationService.GetString("Home_Status_Synced")?.Replace("{0}", updatedCount.ToString()) ?? $"Updated {updatedCount} locations." : LocalizationService.GetString("Home_Status_NoNew") ?? "No new data."; });

                // Refresh UI components if needed
                await RefreshUIComponentsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] App refresh error: {ex.Message}");
                StatusMessage = LocalizationService.GetString("Home_Status_RefreshFailed") ?? "Refresh failed. Check network.";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task RefreshUIComponentsAsync()
        {
            // Implement any additional UI component refresh logic here
            // For now, we will just simulate a delay
            await Task.Delay(500);
        }

        private void ApplyFilter()
        {
            if (_allPOIs.Count == 0)
                return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allPOIs
                : _allPOIs.Where(p =>
                        (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.DescriptionText) && p.DescriptionText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.Address) && p.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                NearbyPOIs.Clear();
                foreach (var poi in filtered)
                {
                    NearbyPOIs.Add(poi);
                }
            });

            // ✅ Load avatars in background after UI update
            _ = Task.Run(() => LoadAvatarsForPOIsAsync(filtered));
        }

        /// <summary>
        /// ✅ NEW: Bulk load all avatars at once (prevent N+1 queries)
        /// </summary>
        private async Task LoadAvatarsForPOIsAsync(List<POI> pois)
        {
            if (pois.Count == 0)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] 🎯 AVATAR: Bulk loading avatars for {pois.Count} POIs");

                var avatarDict = await _poiRepository.GetAllAvatarImagesAsync();

                var loadedCount = 0;
                var fallbackCount = 0;
                foreach (var poi in pois)
                {
                    if (avatarDict.TryGetValue(poi.Id, out var avatarUrl) && !string.IsNullOrWhiteSpace(avatarUrl))
                    {
                        poi.AvatarImageUrl = avatarUrl;
                        loadedCount++;
                        continue;
                    }

                    // Fallback from POI.ImageUrls first item
                    var firstImage = poi.ImageUrlList.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                    if (!string.IsNullOrWhiteSpace(firstImage))
                    {
                        poi.AvatarImageUrl = firstImage;
                        fallbackCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[HomeViewModel] ⚠️ AVATAR: No image for POI {poi.Id} ({poi.Name}) -> UI placeholder");
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var current = NearbyPOIs.ToList();
                    NearbyPOIs.Clear();
                    foreach (var p in current)
                        NearbyPOIs.Add(p);

                    System.Diagnostics.Debug.WriteLine($"[HomeViewModel] 📊 AVATAR: Loaded={loadedCount}, Fallback={fallbackCount}, Total={pois.Count}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] ❌ AVATAR: Load error: {ex.Message}");
            }
        }

        private async Task OpenDetailAsync(POI? poi)
        {
            if (poi is null || _isNavigating)
                return;

            try
            {
                _isNavigating = true;
                await Shell.Current.GoToAsync($"detail?poiId={poi.Id}", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Navigation error: {ex}");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                SelectedPOI = null;
                _isNavigating = false;
            }
        }

        private async Task StartLocationService()
        {
            var permission = await _locationService.CheckAndRequestLocationPermission();
            if (permission != PermissionStatus.Granted)
            {
                StatusMessage = LocalizationService.GetString("Home_Status_Permission_Denied") ?? "Location permission denied";
                return;
            }

            await _locationService.StartListening();
            IsLocationServiceRunning = true;
            StatusMessage = LocalizationService.GetString("Home_Status_LocationLoading") ?? "Tracking location...";

            // Do an immediate check when starting to catch POIs if the device is already in a zone and static
            try
            {
                var loc = await _locationService.GetCurrentLocation();
                if (loc != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => OnLocationUpdated(this, loc));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Initial location check failed: {ex.Message}");
            }
        }

        private async Task StopLocationService()
        {
            await _locationService.StopListening();
            IsLocationServiceRunning = false;
            StatusMessage = LocalizationService.GetString("Home_Status_LocationStopped") ?? "Location tracking stopped";
        }

        private void OnLocationUpdated(object sender, Location location)
        {
            UserLatitude = location.Latitude;
            UserLongitude = location.Longitude;
            var msgFormat = LocalizationService.GetString("Home_Status_LocationUpdated") ?? "Location: {0:F5}, {1:F5}";
            StatusMessage = string.Format(msgFormat, location.Latitude, location.Longitude);

            _ = _geofenceEngine.CheckPOIs(location);
        }

        private void OnPOITriggered(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var msgFormat = LocalizationService.GetString("Home_Status_POITriggered") ?? "Triggered: {0}";
                StatusMessage = string.Format(msgFormat, poi.Name);

                if (!NearbyPOIs.Contains(poi))
                    NearbyPOIs.Add(poi);

                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] POI triggered and added: {poi.Name}");
            });
        }

        private void OnAudioStarted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var msgFormat = LocalizationService.GetString("Home_Status_AudioStarted") ?? "Playing: {0}";
                StatusMessage = string.Format(msgFormat, poi.Name);
            });
        }

        private void OnAudioCompleted(object sender, POI poi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = LocalizationService.GetString("Home_Status_AudioCompleted") ?? "Audio playback completed";
            });
        }

        private async void OnRepositoryPoisSynced(object? sender, int syncedCount)
        {
            try
            {
                var refreshed = await _poiRepository.GetActivePOIsAsync();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allPOIs.Clear();
                    _allPOIs.AddRange(refreshed);
                    ApplyFilter();
                    var msgFormat = LocalizationService.GetString("Home_Status_AutoUpdated") ?? "Data auto-updated ({0} POI).";
                    StatusMessage = string.Format(msgFormat, syncedCount);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeViewModel] Auto-refresh after sync error: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

