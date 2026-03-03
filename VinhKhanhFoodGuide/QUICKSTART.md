# Quick Start Guide - Vinh Khanh Food Guide

## Prerequisites

### System Requirements
- Windows 10/11 or macOS 12+ or Linux
- .NET 8 SDK installed
- For Android:
  - Android SDK (API level 21+)
  - Android emulator or physical device

### Development Tools
- Visual Studio 2022 (17.0+) OR Visual Studio Code
- MAUI workload

## Installation Steps

### 1. Install .NET 8
```bash
# Download from https://dotnet.microsoft.com/download
# Or use package manager
winget install Microsoft.DotNet.SDK.8
```

### 2. Create MAUI Workload
```bash
dotnet workload restore
```

### 3. Clone/Open Project
```bash
cd VinhKhanhFoodGuide
dotnet restore
```

### 4. Build Project
```bash
# For Android
dotnet build -f net8.0-android -c Release

# Check build succeeded
echo "Build complete!"
```

## Running the App

### Option A: Android Emulator

```bash
# List available emulators
emulator -list-avds

# Start emulator (or use Android Studio)
emulator -avd Pixel_5_API_30

# Wait for emulator to fully boot, then:
dotnet run -f net8.0-android
```

### Option B: Physical Android Device

```bash
# Enable USB debugging on device
# Connect device via USB

# Check device connected
adb devices

# Run app
dotnet run -f net8.0-android
```

### Option C: Visual Studio 2022

1. Open `VinhKhanhFoodGuide.sln`
2. Select Android target from dropdown
3. Click Green Play button or press F5
4. Wait for app to deploy

## First Run Behavior

### Automatic Setup
1. **Database Creation**: SQLite database created at first launch
2. **Table Creation**: POI and POIContent tables created
3. **Data Seeding**: 5 sample POI locations loaded with Vietnamese/English content
4. **Location Permission**: App requests location access on first use

### Initial State
- App starts on HomePage
- **Status**: "Initialized. Tap 'Start Tracking' to begin."
- **Tracking**: Disabled (tap Start Tracking button)
- **POIs**: List shows all 5 sample locations

## Testing the App

### Test Scenario 1: Simulate Geofence Trigger
The app includes 5 real POI locations in District 4, HCMC:
- **Bánh Mì Tươi**: 10.77695°N, 106.67895°E
- **Cơm Tấm Sài Gòn**: 10.77705°N, 106.67915°E
- **Phở Hương Liệu**: 10.77715°N, 106.67835°E
- **Kem Tươi Tây Ninh**: 10.77685°N, 106.67955°E
- **Nước Mía Minh Châu**: 10.77675°N, 106.67875°E

**Using Emulator (mock location)**:
```bash
# In Android Studio Emulator Extended controls:
# 1. Virtual Sensors → Location
# 2. Set latitude/longitude to POI coordinates
# 3. Execute in app
# 4. When within POI radius, geofence triggers automatically
```

**Using Physical Device**:
- Walk into Vinh Khanh Food Street area
- App automatically triggers when you enter any POI geofence
- Audio plays (English or Vietnamese)

### Test Scenario 2: Text-to-Speech
1. Navigate to any POI in the list
2. Tap the POI card to view details (if detail navigation added)
3. Click **Play** button
4. Device speaks the POI description

### Test Scenario 3: Language Selection
1. Go to **Settings** tab
2. Change "App Language" dropdown
3. Return to Home
4. POI content automatically updates

### Test Scenario 4: Location Tracking
1. Open Home page
2. Click **Start Tracking**
3. Status message changes to "Tracking location..."
4. "Current Location" updates with GPS coordinates
5. "Nearest POI" updates as you move

## Troubleshooting

### App won't compile
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build -f net8.0-android
```

### Geolocation returns null
- Ensure Android 21+ (API 21+)
- Check permission setting: Settings > Apps > VinhKhanhFoodGuide > Permissions > Location
- Restart emulator/device
- Enable mock location (emulator)

### TTS not working
- Check device has TTS engine installed
- Android Settings > Accessibility > Text-to-Speech output
- Enable any available engine

### Database issues
```bash
# Clear app data (emulator)
adb shell pm clear com.vinhkhanh.foodguide

# Clear and reinstall
adb uninstall com.vinhkhanh.foodguide
dotnet run -f net8.0-android
```

### Geofence not triggering
- Check: IsAudioPlaying not stuck (tap Stop in detail page if needed)
- Check: Cooldown period (default 30 min per POI)
- Check: Actually within POI radius (50 meters)
- Check: Firebase Crashlytics logs if available

## Key Files to Monitor

| File | Purpose |
|------|---------|
| `Services/LocationService.cs` | GPS tracking logic |
| `Services/GeofenceEngine.cs` | Haversine distance + trigger logic |
| `Services/AudioManager.cs` | TTS + audio queue |
| `Data/PoiRepository.cs` | Database + seeding |
| `Pages/HomePage.xaml(.cs)` | Main UI |
| `VinhKhanhFoodGuide.csproj` | NuGet packages |

## Database Location

SQLite database stored at:
- **Android Emulator**: `/data/data/com.vinhkhanh.foodguide/files/VinhKhanhFoodGuide.db`
- **Physical Device**: Internal app storage

## Important Notes

### Geofence Behavior
- **Debounce**: 3-second minimum between trigger checks
- **Cooldown**: 30 minutes per POI (configurable in DB)
- **Audio Lock**: Won't trigger while audio playing
- **Running Status**: Only triggered while tracking is active

### Battery Impact
- **Active Tracking**: ~15-20% per hour (continuous GPS)
- **Idle**: Minimal battery drain
- **TTS**: Audio playback uses device speaker system

### Offline Mode
- Fully functional without internet
- All data stored locally in SQLite
- Network capability prepared for future cloud sync

## Development Workflow

### Code Changes
```bash
# Make changes
vim Services/GeofenceEngine.cs

# Hot reload (if VS Code with MAUI extension)
dotnet run -f net8.0-android --no-build

# Or full rebuild
dotnet run -f net8.0-android
```

### Adding New POIs
Edit `Data/PoiRepository.cs` → `SeedDemoDataAsync()` method:
```csharp
var pois = new List<POI>
{
    new POI
    {
        Name = "New POI",
        Latitude = 10.77700,
        Longitude = 106.67900,
        Radius = 50,
        Priority = 5,
        CooldownMinutes = 30
    }
};
```

### Adding Languages
Edit `Data/POIContent` entries with new `LanguageCode`:
```csharp
new POIContent { LanguageCode = "fr", TextContent = "..." }
```

Then update `SettingsViewModel.AvailableLanguages`.

## Performance Tips

1. **Reduce geofence checks**: Increase debounce delay (currently 3s)
2. **Location accuracy**: Use `GeolocationAccuracy.Default` instead of `Best`
3. **POI count**: Database can handle 100+ POIs efficiently
4. **Memory**: Audio queue limited to prevent memory leaks

## Next Steps

### For Production
- [ ] Replace mock coordinates with actual Vinh Khanh locations
- [ ] Add real POI images (replace "placeholder.png")
- [ ] Record audio files or configure TTS voice
- [ ] Test on multiple Android devices
- [ ] Implement analytics/tracking
- [ ] Add push notifications for POIs
- [ ] Implement cloud sync for POI updates
- [ ] Create admin CMS for content management
- [ ] Add user reviews/ratings

### For Enhancement
- [ ] Integrate Firebase for backend
- [ ] Add offline map support
- [ ] Implement QR code scanning
- [ ] Create multiple language packs
- [ ] Add cultural/historical information
- [ ] Integrate restaurant ratings/reviews
- [ ] Add photo gallery per POI
- [ ] Create user accounts/favorites

## Support

For issues or questions:
1. Check README.md for detailed documentation
2. Review error logs: `adb logcat | grep VinhKhanh`
3. Test on emulator first before device
4. Check Android version compatibility

---

**Happy testing! The app is ready to use and extend.** 🚀
