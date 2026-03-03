# Vinh Khanh Food Guide - MAUI Mobile App

An automatic multilingual audio guide application for Vinh Khanh Food Street (District 4, Ho Chi Minh City, Vietnam).

## Overview

When users enter a predefined POI (Point of Interest) geofence area, the app automatically displays images and plays an audio description (Text-to-Speech or audio files) about that food location.

## Features

### 1. GPS Tracking
- Real-time location tracking with MAUI Geolocation API
- Adjustable update interval based on user speed (2s when moving, 5s when stationary)
- Background tracking support (Android)
- Battery-optimized location updates

### 2. Geofence Engine
- Calculates distance using Haversine formula
- Triggers when user enters POI radius
- Debounce protection (3 seconds) prevents spam
- Cooldown system (configurable per POI)
- Prevents triggering while audio is playing

### 3. Audio Manager
- Queue-based audio playback system
- Supports:
  - Local audio file playback
  - Text-to-Speech (MAUI TextToSpeech API)
- Single-audio enforcement (one audio at a time)
- Play, pause, and stop controls

### 4. Map View
- Shows user's current location
- Displays POI markers
- Highlights nearest POI
- Interactive POI details

### 5. Offline-First
- POI data stored in SQLite
- App functions without internet
- Designed for future API sync capability

## Architecture

### 3-Layer Structure

```
VinhKhanhFoodGuide/
├── Models/                          # Data models
│   ├── POI.cs                       # Point of Interest
│   ├── POIContent.cs                # Multilingual content
│   ├── LocationData.cs              # Location info
│   └── GeofenceEvent.cs             # Event data
│
├── Services/                        # Business logic
│   ├── LocationService.cs           # GPS tracking
│   ├── GeofenceEngine.cs            # Geofence logic
│   └── AudioManager.cs              # Audio playback
│
├── Data/                            # Data persistence
│   ├── IPoiRepository.cs            # Repository interface
│   └── PoiRepository.cs             # SQLite implementation
│
├── ViewModels/                      # MVVM ViewModels
│   ├── HomeViewModel.cs             # Home page logic
│   ├── POIDetailViewModel.cs        # Detail page logic
│   └── SettingsViewModel.cs         # Settings logic
│
├── Pages/                           # UI Pages (XAML)
│   ├── HomePage.xaml/.cs            # Main map view
│   ├── POIDetailPage.xaml/.cs       # POI details
│   └── SettingsPage.xaml/.cs        # App settings
│
├── Platforms/
│   └── Android/                     # Android-specific
│       ├── AndroidManifest.xml      # Permissions
│       └── LocationService.cs       # Background service
│
└── Resources/
    └── Images/                      # POI images
```

## Data Model

### POI (Point of Interest)
```csharp
public class POI
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }             // meters
    public int Priority { get; set; }              // priority level
    public int CooldownMinutes { get; set; }       // min between triggers
    public string ImagePath { get; set; }
    public string Category { get; set; }
}
```

### POIContent (Multilingual)
```csharp
public class POIContent
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string LanguageCode { get; set; }       // "vi", "en", "fr", etc.
    public string TextContent { get; set; }        // Description
    public string AudioPath { get; set; }          // Optional audio file
    public bool UseTextToSpeech { get; set; }      // Use TTS if no audio
}
```

## Installation & Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Android SDK (for Android development)
- MAUI workload installed

### Clone and Build

```bash
cd VinhKhanhFoodGuide
dotnet workload restore
dotnet build -f net8.0-android
```

### Run on Android Device/Emulator

```bash
dotnet run -f net8.0-android
```

## Database Initialization

The app automatically:
1. Creates SQLite database on first run
2. Creates POI and POIContent tables
3. Seeds 5 sample POI locations with multilingual content

### Sample POIs
- **Bánh Mì Tươi** - Fresh Vietnamese bread (10.77695°N, 106.67895°E)
- **Cơm Tấm Sài Gòn** - Broken rice with grilled pork (10.77705°N, 106.67915°E)
- **Phở Hương Liệu** - Traditional beef noodle soup (10.77715°N, 106.67835°E)
- **Kem Tươi Tây Ninh** - Fresh ice cream (10.77685°N, 106.67955°E)
- **Nước Mía Minh Châu** - Fresh sugarcane juice (10.77675°N, 106.67875°E)

Each has content in both Vietnamese and English.

## Usage

### Home Page
1. **Start Tracking** - Begins GPS location updates
2. **Stop Tracking** - Stops GPS tracking
3. View current location and nearest POI
4. See list of all POIs

### POI Details
- View POI name, category, and image
- Switch between Vietnamese and English
- Read description
- Play audio (TTS or file)
- Stop/pause audio

### Settings
- Change app language
- Toggle Text-to-Speech
- Adjust location update interval
- View app information

## Key Technologies

- **Framework**: .NET MAUI (Cross-platform)
- **Language**: C#
- **Database**: SQLite (sqlite-net-pcl)
- **Location**: MAUI Geolocation API
- **Audio**: MAUI TextToSpeech API
- **Architecture**: MVVM + 3-Layer Service Pattern
- **UI**: XAML

## Geofence Algorithm

### Distance Calculation (Haversine Formula)
```csharp
// Calculates great-circle distance between two coordinates
var distance = CalculateDistance(
    currentLat, currentLon,
    poiLat, poiLon
);
```

### Trigger Logic
1. User enters POI radius area → Distance ≤ Radius
2. Debounce check (3 seconds minimum between checks)
3. Cooldown check (POI-specific cooldown period)
4. Audio playback check (no trigger if audio already playing)
5. Fire GeofenceTriggered event → Play audio

## Performance Considerations

- **Battery**: Location updates adjust based on speed
  - Fast updates (2s) when speed > 1 m/s
  - Slow updates (5s) when stationary
- **Debounce**: Prevents spam triggers (3s minimum)
- **Cooldown**: Per-POI cooldown prevents repetition
- **Queue**: Audio queue ensures sequential playback

## Permissions (Android)

**Location:**
- ACCESS_FINE_LOCATION
- ACCESS_COARSE_LOCATION
- ACCESS_BACKGROUND_LOCATION (for background tracking)

**Audio:**
- RECORD_AUDIO
- MODIFY_AUDIO_SETTINGS

**Network:**
- INTERNET (for future API sync)

**Other:**
- ACCESS_NETWORK_STATE
- BATTERY_STATS

## Future Enhancements

- [ ] Cloud API sync for POI data
- [ ] Offline maps integration
- [ ] QR code scanning for POIs
- [ ] Analytics and usage tracking
- [ ] CMS for content management
- [ ] Photo capture and social sharing
- [ ] Multi-language support UI (currently English/Vietnamese selectable in content)
- [ ] Advanced map features (directions, markers)
- [ ] Audio file library management

## File Structure Summary

```
VinhKhanhFoodGuide.csproj          # Project file with NuGet packages
MauiProgram.cs                      # DI container setup
App.xaml(.cs)                       # Application entry point
AppShell.xaml(.cs)                  # Shell navigation structure

Models/
  ├── POI.cs                        # [SQLite] POI data model
  ├── POIContent.cs                 # [SQLite] Content model
  ├── LocationData.cs               # Location info
  └── GeofenceEvent.cs              # Event model

Services/
  ├── LocationService.cs            # ILocationService implementation
  ├── GeofenceEngine.cs             # IGeofenceEngine with Haversine
  └── AudioManager.cs               # IAudioManager with TTS/file support

Data/
  ├── IPoiRepository.cs             # Data access interface
  └── PoiRepository.cs              # SQLite repository + seeding

ViewModels/
  ├── HomeViewModel.cs              # Home page logic (MVVM)
  ├── POIDetailViewModel.cs         # Detail page logic
  └── SettingsViewModel.cs          # Settings preferences

Pages/
  ├── HomePage.xaml(.cs)            # Main tracking UI
  ├── POIDetailPage.xaml(.cs)       # POI details UI
  └── SettingsPage.xaml(.cs)        # Settings UI

Platforms/Android/
  ├── AndroidManifest.xml           # Permissions manifest
  └── LocationService.cs            # Android background service stub

Resources/Images/                    # POI images (placeholder)
```

## Database Schema

### POI Table
```sql
CREATE TABLE POI (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Latitude REAL NOT NULL,
    Longitude REAL NOT NULL,
    Radius REAL NOT NULL,
    Priority INTEGER NOT NULL,
    CooldownMinutes INTEGER NOT NULL,
    ImagePath TEXT,
    Category TEXT
);
```

### POIContent Table
```sql
CREATE TABLE POIContent (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PoiId INTEGER NOT NULL,
    LanguageCode TEXT NOT NULL,
    TextContent TEXT NOT NULL,
    AudioPath TEXT,
    UseTextToSpeech BOOLEAN
);
```

## Troubleshooting

### Location not updating
- Check: Location permissions granted in Android settings
- Check: Geolocation API initialized
- Check: IsTracking flag is true

### Audio not playing
- Check: AudioManager queue not stuck
- Check: TTS engine available on device
- Check: Audio file path exists (if using local files)

### Geofence not triggering
- Check: IsAudioPlaying flag not blocking
- Check: Cooldown period expired
- Check: User actually within POI radius
- Check: POI data loaded from database

### Database errors
- Clear app data: `adb shell pm clear com.vinhkhanh.foodguide`
- Reinstall app
- Check disk space

## License

MIT License - Free to use and modify

## Author

Created for Vinh Khanh Food Street Audio Guide Project
District 4, Ho Chi Minh City, Vietnam

---

**Version**: 1.0  
**Target**: Android 21+  
**Framework**: .NET MAUI on .NET 8
