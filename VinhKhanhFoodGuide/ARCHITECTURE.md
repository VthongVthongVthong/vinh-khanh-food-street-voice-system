# Architecture Overview - Vinh Khanh Food Guide

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                       PRESENTATION LAYER                         │
│  (XAML Pages + MVVM ViewModels)                                 │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌─────────────────┐  ┌──────────────┐       │
│  │  HomePage    │  │ POIDetailPage   │  │SettingsPage  │       │
│  │              │  │                 │  │              │       │
│  │  + Start Btn │  │ + Play Btn      │  │ + Language   │       │
│  │  + Stop Btn  │  │ + Language Sel  │  │ + TTS Toggle │       │
│  │  + POI List  │  │ + Description   │  │ + Update Inv │       │
│  └──────┬───────┘  └────────┬────────┘  └──────┬───────┘       │
│         │                   │                   │                │
│  ┌──────┴───────────────────┴───────────────────┴───────┐       │
│  │  HomeViewModel     POIDetailVM    SettingsViewModel  │       │
│  │  (Logic / State)                                     │       │
│  └──────┬────────────────────────────────────────────┬──┘       │
│         │           (Binds data)                     │          │
└─────────┼─────────────────────────────────────────────┼──────────┘
          │                                             │
          ▼                                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER                               │
│  (Business Logic + State Management)                            │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐ │
│  │LocationService   │  │ GeofenceEngine   │  │ AudioManager  │ │
│  │                  │  │                  │  │               │ │
│  │✓ Geolocation API │  │✓ Haversine calc  │  │✓ TTS support  │ │
│  │✓ Real-time track │  │✓ Distance check  │  │✓ File queue   │ │
│  │✓ Speed adaptive  │  │✓ Debounce 3s    │  │✓ Cooldown mgmt│
│  │✓ Event emission  │  │✓ Cooldown check │  │✓ Lock prevent │ │
│  └─────────────────┘  └──────────────────┘  └───────────────┘ │
│         │                      │                      │         │
│  Events │ LocationChanged      │ GeofenceTriggered  │ Playing  │
│         │                      │                    │ Status   │
└─────────┼──────────────────────┼────────────────────┼─────────┄┘
          │                      │                    │
          ▼                      ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DATA LAYER                                 │
│  (Persistence + Repository Pattern)                             │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────┐                           │
│  │  IPoiRepository Interface        │                           │
│  │  (Abstraction)                   │                           │
│  │  • GetAllPoisAsync()             │                           │
│  │  • GetPoiByIdAsync()             │                           │
│  │  • GetPoiContentAsync()          │                           │
│  │  • InsertPoiAsync()              │                           │
│  │  • QueryMethods + CRUD           │                           │
│  └──────────┬───────────────────────┘                           │
│             │ (implements)                                       │
│  ┌──────────▼───────────────────────┐                           │
│  │  PoiRepository                   │                           │
│  │  (SQLite Implementation)         │                           │
│  │  • Manages DB connection         │                           │
│  │  • CRUD operations               │                           │
│  │  • Seeds demo data               │                           │
│  └──────────┬───────────────────────┘                           │
│             │                                                    │
│  ┌──────────▼───────────────────────┐                           │
│  │  SQLite Database                 │                           │
│  │  (offline-first storage)         │                           │
│  │  • POI table (5 locations)       │                           │
│  │  • POIContent table (10 content) │                           │
│  │  • Multi-language support        │                           │
│  └──────────────────────────────────┘                           │
└─────────────────────────────────────────────────────────────────┘
```

## Component Interaction Flow

### Flow 1: Location Tracking & Geofence Trigger

```
User taps START
     │
     ▼
HomeViewModel.StartTrackingAsync()
     │
     ▼
LocationService.StartTrackingAsync()
     │
     ├─ Request Permissions
     ├─ Set IsTracking = true
     └─ Launch TrackLocationAsync() background task
     │
     ▼ (every 2-5 seconds based on speed)
Get GPS Location from Device
     │
     ▼
LocationService.LocationChanged event fires
     │
     ├─ HomeViewModel listens → updates CurrentLocation
     │
     └─ GeofenceEngine.UpdateLocation()
        │
        ├─ Calculate distance to each POI (Haversine)
        ├─ Check: distance ≤ POI radius?
        ├─ Check: 3 sec since last check? (debounce)
        ├─ Check: cooldown expired? (POI-specific)
        └─ Check: audio not playing?
        │
        ▼ (all checks pass)
        GeofenceEngine.GeofenceTriggered event fires
        │
        └─ HomeViewModel listens
           │
           ├─ Set SelectedPoi
           ├─ Update StatusMessage
           └─ Call PlayPoiAudioAsync()
              │
              ▼
              Fetch POI content from database
              │
              ▼
              AudioManager.PlayTextToSpeechAsync()
              │
              ├─ Convert text to speech
              ├─ Play audio on device speaker
              └─ Set IsPlaying flag
```

### Flow 2: User Manual Audio Playback

```
User navigates to POI Detail Page
     │
     ▼
POIDetailViewModel.LoadPoiAsync(poiId)
     │
     ├─ Fetch POI from repository
     ├─ Fetch POIContent for current language
     └─ Update UI labels
     │
     ▼
User selects language from dropdown
     │
     ▼
POIDetailViewModel.SelectedLanguage = "vi" (or "en")
     │
     ▼
LoadContentForLanguageAsync() triggered
     │
     ├─ Query database for language-specific content
     ├─ Update CurrentContent binding
     └─ UI shows description in selected language
     │
     ▼
User taps PLAY button
     │
     ▼
POIDetailViewModel.PlayAudioAsync()
     │
     ├─ Check if audio file exists
     │
     ├─ If yes: AudioManager.PlayAudioFileAsync(path)
     ├─ If no: AudioManager.PlayTextToSpeechAsync(text)
     │
     └─ IsPlaying = true
        │
        ▼ (audio plays)
        │
        ▼ (audio finishes)
        │
        └─ IsPlaying = false
```

### Flow 3: Settings Persistence

```
User taps Settings tab
     │
     ▼
SettingsPage displays current preferences
     │
     ├─ Load from Preferences.Default
     ├─ Display in UI controls
     │
     ▼
User changes Language dropdown
     │
     └─ Event handler → SettingsViewModel.SelectedLanguage = "vi"
     │
     ▼
User toggles TTS switch
     │
     └─ Event handler → SettingsViewModel.IsTtsEnabled = false
     │
     ▼
User adjusts Slider
     │
     └─ Event handler → SettingsViewModel.UpdateIntervalSeconds = 10
     │
     ▼
User taps SAVE button
     │
     ▼
SettingsViewModel.SaveSettings()
     │
     ├─ Preferences.Default.Set("app_language", "vi")
     ├─ Preferences.Default.Set("tts_enabled", false)
     └─ Preferences.Default.Set("update_interval", 10)
     │
     ▼
Settings persisted to device storage
```

## Geofence Algorithm (Haversine Formula)

```
When: LocationService emits LocationChanged event
Then: GeofenceEngine.CheckGeofences() executes

For each POI in database:
   │
   ├─ distance = CalculateDistance(
   │                userLat, userLon,
   │                poi.Latitude, poi.Longitude)
   │
   │     R = 6371000 (meters)
   │     dLat = (lat2 - lat1) * π/180
   │     dLon = (lon2 - lon1) * π/180
   │     a = sin²(dLat/2) + cos(lat1) * cos(lat2) * sin²(dLon/2)
   │     c = 2 * atan2(√a, √(1-a))
   │     distance = R * c
   │
   ├─ IF distance ≤ poi.Radius THEN
   │     check cooldown timer
   │     check audio lock
   │     IF all checks pass:
   │         fire GeofenceTriggered event
   │         record lastTriggerTime[poi.Id]
   │         play audio
   │
   └─ ELSE continue if more POIs

Result: Only one POI triggers per location check
```

## Data Models

### POI Table Schema
```sql
CREATE TABLE POI (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    Name                TEXT NOT NULL,
    Latitude            REAL NOT NULL,
    Longitude           REAL NOT NULL,
    Radius              REAL NOT NULL,      -- meters
    Priority            INTEGER NOT NULL,   -- higher = more important
    CooldownMinutes     INTEGER NOT NULL,   -- min between triggers
    ImagePath           TEXT,               -- local file path
    Category            TEXT                -- Restaurant, Dessert, etc
);

-- Indexes for performance
CREATE INDEX idx_poi_priority ON POI(Priority DESC);
```

### POIContent Table Schema
```sql
CREATE TABLE POIContent (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    PoiId               INTEGER NOT NULL,   -- FK to POI
    LanguageCode        TEXT NOT NULL,      -- "vi", "en", "fr"
    TextContent         TEXT NOT NULL,      -- Description
    AudioPath           TEXT,               -- Optional: audio file path
    UseTextToSpeech     BOOLEAN NOT NULL    -- true = use TTS if no file

    FOREIGN KEY(PoiId) REFERENCES POI(Id)
);

-- Index for language queries
CREATE INDEX idx_poi_content_lang ON POIContent(PoiId, LanguageCode);
```

## Dependency Injection (MauiProgram.cs)

```csharp
// Service Registration
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddSingleton<IAudioManager, AudioManager>();
builder.Services.AddSingleton<IPoiRepository, PoiRepository>();
builder.Services.AddSingleton<IGeofenceEngine, GeofenceEngine>();

// ViewModel Registration
builder.Services.AddSingleton<HomeViewModel>();
builder.Services.AddSingleton<POIDetailViewModel>();
builder.Services.AddSingleton<SettingsViewModel>();

// Page Registration
builder.Services.AddSingleton<HomePage>();
builder.Services.AddSingleton<POIDetailPage>();
builder.Services.AddSingleton<SettingsPage>();
builder.Services.AddSingleton<AppShell>();

// Result:
// Constructor injection automatically resolves dependencies
// Example: HomeViewModel(ILocationService loc, IGeofenceEngine geo, ...)
```

## Performance Characteristics

### Location Updates
- **Frequency**: Adaptive debounce
  - Fast (2 sec) when speed > 1 m/s
  - Slow (5 sec) when stationary
- **Accuracy**: High (Best - MAUI default)
- **Battery**: ~15-20% per hour continuous tracking

### Geofence Checks
- **Debounce**: 3 seconds minimum between checks
- **Haversine Calc**: O(1) per check
- **POIs Scanned**: All POIs per check (scalable to 100+)
- **Trigger Cooldown**: Per-POI (default 30 min)

### Audio Playback
- **Queue**: Sequential processing
- **TTS**: Device-dependent (usually 1-5 seconds per sentence)
- **Audio Files**: Instant playback
- **Lock**: Prevents concurrent audio playback

### Database
- **Type**: SQLite (optimized for mobile)
- **Size**: ~50KB with 5 POIs + content
- **Queries**: Indexed for fast lookups
- **Concurrent Access**: Safe (SQLite handles locking)

## State Management

### HomeViewModel State
```csharp
StatusMessage           // User-facing status text
CurrentLocation         // Current GPS coordinates
SelectedPoi             // Currently selected POI
IsTracking              // Tracking active flag
NearestPoiName          // Name of nearest POI
NearestPoiDistance      // Distance to nearest POI
AllPois                 // ObservableCollection of all POIs
```

### GeofenceEngine State
```csharp
_currentLocation        // Latest location
_pois                   // All POIs from database
_lastTriggerTime[]      // Cooldown tracking per POI
_lastDebounceTime       // Last geofence check time
_isAudioPlaying         // Audio lock flag
```

### AudioManager State
```csharp
_isPlaying              // Playing flag
_audioQueue             // Queue<Func<Task>> for sequential playback
_isProcessingQueue      // Queue lock to prevent concurrent processing
```

## Event Flow Summary

| Event | Fired By | Listeners | Action |
|-------|----------|-----------|--------|
| `LocationChanged` | LocationService | HomeViewModel, GeofenceEngine | Update position, check geofence |
| `GeofenceTriggered` | GeofenceEngine | HomeViewModel | Select POI, play audio |
| `PropertyChanged` | All ViewModels | UI Bindings | Update display |
| `Toggled` | TTS Switch | SettingsViewModel | Save preference |
| `ValueChanged` | Slider | SettingsViewModel | Update interval |
| `SelectedIndexChanged` | Picker | SettingsViewModel | Change language |

## Thread Safety

- **LocationChanged**: Fired on background thread → `MainThread.BeginInvokeOnMainThread()`
- **GeofenceTriggered**: Fired on background thread → `MainThread.BeginInvokeOnMainThread()`
- **sqlite-net-pcl**: Uses `SQLiteAsyncConnection` for thread-safe DB access
- **UI Binding**: Single-threaded UI framework (XAML)

## Error Handling Strategy

| Layer | Error Type | Handling |
|-------|-----------|----------|
| Location | Permission Denied | Show dialog, fallback to default location |
| Location | No GPS Signal | Retry with adjusted timeout |
| Geofence | Distance Calc Error | Log debug, skip check, continue |
| Audio | TTS Not Available | Use audio file, fallback to text display |
| Audio | File Not Found | Log error, skip playback |
| Database | Table Creation Failed | App crash with error log |
| Database | Query Timeout | Retry with longer timeout |

## Security Considerations

- **Permissions**: Request at runtime (Android 6+)
- **Location**: Only accessed while tracking enabled
- **Database**: SQLiteAsyncConnection handles parameterized queries (SQL injection safe)
- **Files**: App data directory isolation
- **Audio**: Uses system TTS engine (safe)

## Scalability Limits

- **POIs**: Tested with 5, scalable to 100+ (Haversine O(1) per POI)
- **Languages**: 10+ supported (language code is just a string)
- **Content**: Text length unlimited (stored in TEXT column)
- **Users**: Single-device app (no user management)
- **Concurrent Tracking**: Single location stream (one user)

## Future Architecture Improvements

1. **API Integration**
   - CloudPoiService → Replace/sync local POIs
   - LocationHistoryService → Track user movement

2. **Caching Layer**
   - In-memory POI cache
   - Reduce database queries
   - Instant geofence checks

3. **Analytics Service**
   - Track POI triggers
   - User engagement metrics
   - Heat maps

4. **Map Service**
   - Replace list with MapView
   - Show POI markers
   - Route planning

5. **Media Service**
   - Extended audio management
   - Video support
   - Streaming audio

---

**Architecture Version**: 1.0  
**Last Updated**: 2026-03-03  
**Framework**: .NET MAUI 8.0
