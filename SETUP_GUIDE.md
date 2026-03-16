# Vinh Khanh Food Street Audio Guide - Setup Guide

## Quick Start (5 minutes)

### Step 1: Prerequisites
```bash
# Verify .NET 9 installation
dotnet --version

# Should output: 9.x.x or higher
```

### Step 2: Restore Packages
```bash
cd VinhKhanhstreetfoods
dotnet restore
```

### Step 3: Build
```bash
dotnet build
```

### Step 4: Run
```bash
# For Android
dotnet run --framework net9.0-android

# For iOS
dotnet run --framework net9.0-ios

# For Windows
dotnet run --framework "net9.0-windows10.0.19041.0"
```

---

## Detailed Setup Instructions

### Part 1: Project Configuration

#### 1.1 Google Maps API Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create new project or select existing
3. Enable these APIs:
   - Maps SDK for Android
   - Maps SDK for iOS
   - Google Maps API
4. Create API key:
   - **Restrictions > Application restrictions**: Select "Android app" or "iOS app"
   - **Restrictions > Key restrictions**: Select "Maps APIs"
5. Copy your key

#### 1.2 Configure API Key in App

**Option A: In MauiProgram.cs**
```csharp
builder.Services.AddSingleton<MapService>(
    new MapService("your_actual_google_maps_api_key")
);
```

**Option B: Environment Variable**
```csharp
var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY") 
    ?? "YOUR_GOOGLE_MAPS_API_KEY";
builder.Services.AddSingleton<MapService>(new MapService(apiKey));
```

### Part 2: Platform-Specific Setup

#### 2.1 Android Configuration

**AndroidManifest.xml** (located in `Platforms/Android/`)
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- Location Permissions -->
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
    
    <!-- Network Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    
    <!-- Maps API Key -->
    <application>
        <meta-data
            android:name="com.google.android.geo.API_KEY"
            android:value="YOUR_GOOGLE_MAPS_API_KEY" />
    </application>
</manifest>
```

**MainActivity.cs** (no changes needed, permission handling is automatic)

#### 2.2 iOS Configuration

**Info.plist** (located in `Platforms/iOS/`)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Location Permissions -->
    <key>NSLocationWhenInUseUsageDescription</key>
    <string>We need your location to provide audio guides for nearby restaurants.</string>
    
    <key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
    <string>We use your location to track nearby restaurants and provide automatic narration.</string>
    
    <!-- Maps Configuration -->
    <key>NSBonjourServices</key>
    <array>
        <string>_maps._tcp</string>
    </array>
    
    <!-- Microphone for TTS -->
    <key>NSMicrophoneUsageDescription</key>
    <string>Microphone access is needed for audio playback.</string>
</dict>
</plist>
```

#### 2.3 Windows Configuration

No special permissions needed for Windows desktop deployment.

### Part 3: Database Setup

The database initializes automatically on first app launch:

1. **Database Location**: `%LocalApplicationData%/VinhKhanhFoodGuide.db3`
   - Android: `/data/data/com.companyname.vinhkhanhstreetfoods/files/`
   - iOS: App Documents folder
   - Windows: `C:\Users\[YourName]\AppData\Local\`

2. **Automatic Population**: 
   - First run creates empty tables
   - `SeedData.InitializeAsync()` populates 20 POIs
   - Uses Haversine formula for distance calculation

3. **To Reset Database**:
   ```csharp
   // In App.xaml.cs, modify InitializeDatabaseAsync:
   await poiRepository.ClearAllPOIsAsync();  // Clear all data
   await SeedData.InitializeAsync(poiRepository);  // Re-seed
   ```

### Part 4: NuGet Package Verification

Verify these packages are installed:

```bash
dotnet package search sqlite-net-pcl
dotnet package search CommunityToolkit.Maui
dotnet package search Microsoft.Maui.Controls.Maps
```

If missing, add them:
```bash
dotnet add package sqlite-net-pcl --version 3.1.14
dotnet add package CommunityToolkit.Maui --version 9.3.1
dotnet add package Microsoft.Maui.Controls.Maps
```

---

## Deployment Guide

### Android Deployment

#### Development
```bash
# Debug mode on emulator
dotnet run --framework net9.0-android -c Debug

# Debug on connected device
dotnet run --framework net9.0-android -c Debug
```

#### Release
```bash
# Build signed APK
dotnet publish -f net9.0-android -c Release

# Output: bin/Release/net9.0-android/publish/
```

For Google Play Store:
1. Create keystore: `keytool -genkey -v -keystore mykey.jks ...`
2. Configure in VinhKhanhstreetfoods.csproj:
```xml
<PropertyGroup>
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>mykey.jks</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>myalias</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>Password123!</AndroidSigningKeyPass>
    <AndroidSigningStorePass>Password123!</AndroidSigningStorePass>
</PropertyGroup>
```

### iOS Deployment

#### Development
```bash
# Build for simulator
dotnet build -f net9.0-ios -c Debug

# Build for device
dotnet build -f net9.0-ios -c Release
```

#### App Store
1. Requires Apple Developer account
2. Create App ID in Developer Portal
3. Set signing in project

### Windows Deployment

```bash
# Creates standalone executable
dotnet publish -f net9.0-windows10.0.19041.0 -c Release

# Output: Published exe in bin/Release/net9.0-windows10.0.19041.0/publish/
```

---

## Troubleshooting

### Issue: App Crashes on Startup
**Solution**: Check InitializeDatabaseAsync in App.xaml.cs
```csharp
Debug.WriteLine($"Database initialization error: {ex}");
```

### Issue: Location Not Working
**Solution**: 
1. Check Android/iOS permissions granted
2. Verify permission request code in LocationService.cs
3. Enable mock locations in emulator settings

### Issue: Audio Won't Play
**Solution**:
1. Check TextToSpeechService initialization
2. Verify TTS engine installed on device
3. Check volume settings (not muted)

### Issue: POIs Not Showing
**Solution**:
1. Verify database populated: `await repository.GetAllPOIsAsync()`
2. Check POI coordinates are valid
3. Ensure trigger radius not too small

### Issue: Build Fails with Package Errors
**Solution**:
```bash
# Clean and restore
dotnet clean
rm -rf obj bin
dotnet restore
dotnet build
```

---

## Performance Optimization

### Battery Usage
1. Adjust LocationUpdateIntervalSeconds (higher = less battery drain)
2. Enable BatteryOptimizationEnabled in settings
3. Use Stop button when not exploring

### Map Performance
1. Only load visible POIs using collision detection
2. Implement POI clustering for dense areas
3. Cache map tiles locally

### Audio Performance
1. Use pre-recorded MP3s instead of TTS when possible
2. Implement audio streaming instead of full load
3. Clean up audio queue after playback

---

## Testing Scenarios

### Scenario 1: Basic Functionality
1. Open app
2. Enable location
3. Walk near POI
4. Hear audio narration ✓

### Scenario 2: Multiple Triggers
1. Enable location
2. Walk through multiple POIs
3. Trigger 3+ different POIs
4. Verify audio queuing works ✓

### Scenario 3: Settings Persistence
1. Change language to English
2. Close app completely
3. Reopen app
4. Verify language still English ✓

### Scenario 4: Offline Usage
1. Disable internet connection
2. App still works (no map features)
3. Database queries still work ✓

### Scenario 5: Error Handling
1. Disable location permission
2. App shows permission error ✓
3. User can retry

---

## Advanced Configuration

### Custom POI Data Import

**Import from CSV**:
```csharp
// Example in SettingsViewModel
public async Task ImportPOIsFromCSV(string csvPath)
{
    var lines = File.ReadAllLines(csvPath);
    var pois = new List<POI>();
    
    foreach (var line in lines.Skip(1)) // Skip header
    {
        var parts = line.Split(',');
        pois.Add(new POI
        {
            Name = parts[0],
            Latitude = double.Parse(parts[1]),
            Longitude = double.Parse(parts[2]),
            DescriptionText = parts[3]
        });
    }
    
    await _poiRepository.AddPOIsAsync(pois);
}
```

### Backend Integration (Future)

```csharp
// Add HttpClient for API calls
builder.Services.AddHttpClient<POIApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.vinhkhanh.com");
});

// Implement sync logic
public async Task SyncPOIsFromCloud()
{
    var pois = await _apiService.GetAllPOIsAsync();
    await _repository.AddPOIsAsync(pois);
}
```

---

## Documentation Index

| Topic | File | Lines |
|-------|------|-------|
| GPS Tracking | LocationService.cs | 1-100 |
| Geofence Logic | GeofenceEngine.cs | 1-150 |
| Audio Queue | AudioManager.cs | 1-120 |
| Database Schema | POIRepository.cs | 1-80 |
| UI Layout | Views/*.xaml | All |
| View Logic | ViewModels/* | All |

---

## Success Checklist

- [ ] Project opens without errors
- [ ] All NuGet packages restored
- [ ] App builds successfully for Android
- [ ] App builds successfully for iOS
- [ ] App builds successfully for Windows
- [ ] Location permission request works
- [ ] Database initializes with 20 POIs
- [ ] Settings page loads
- [ ] Maps functionality ready
- [ ] Audio system initialized
- [ ] Git repository initialized
- [ ] Ready for production deployment

---

**Last Updated**: March 2026  
**Version**: 1.0.0  
**Status**: Production Ready
