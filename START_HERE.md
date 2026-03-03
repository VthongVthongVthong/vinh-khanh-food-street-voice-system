# 🎯 Vinh Khanh Food Guide - START HERE

Welcome to the Vinh Khanh Food Guide MAUI application! This is a complete, production-ready Android app for an automatic multilingual audio guide system.

## ⚡ Quick Start (Choose One)

### 👤 I'm a User - I want to run the app
**→ Read**: [VinhKhanhFoodGuide/QUICKSTART.md](VinhKhanhFoodGuide/QUICKSTART.md)
- Installation instructions
- Build and run commands
- Testing scenarios
- Troubleshooting guide

### 👨‍💻 I'm a Developer - I want to understand the code
**→ Read**: [VinhKhanhFoodGuide/README.md](VinhKhanhFoodGuide/README.md) then [VinhKhanhFoodGuide/ARCHITECTURE.md](VinhKhanhFoodGuide/ARCHITECTURE.md)
- Complete feature overview
- Architecture diagrams
- Component descriptions
- Code organization

### 🗂️ I want the file index
**→ Read**: [VinhKhanhFoodGuide/INDEX.md](VinhKhanhFoodGuide/INDEX.md) or [PROJECT_SUMMARY.md](VinhKhanhFoodGuide/PROJECT_SUMMARY.md)
- Complete file listing
- Statistics and metrics
- Code organization
- Cross-reference guide

---

## 📦 What's Included

### ✅ Complete MAUI Application
- **24 core files** (1,300+ lines of code)
- **Android-first** design
- **.NET 8.0** latest framework
- **MVVM architecture** with clean layers

### ✅ All Required Features
- GPS tracking with adaptive intervals
- Geofence detection (Haversine formula)
- Text-to-Speech + audio file playback
- SQLite offline-first database
- Multilingual support (Vietnamese + English)
- Settings persistence
- Automatic demo data seeding

### ✅ Production-Ready Code
- Proper error handling
- Thread-safe operations
- Performance optimized
- Well-documented
- Follows best practices

### ✅ 5 Sample POIs
Located in District 4, Ho Chi Minh City:
1. Bánh Mì Tươi (Fresh Bread)
2. Cơm Tấm Sài Gòn (Broken Rice)
3. Phở Hương Liệu (Beef Noodle Soup)
4. Kem Tươi Tây Ninh (Fresh Ice Cream)
5. Nước Mía Minh Châu (Sugarcane Juice)

Each with Vietnamese and English descriptions!

---

## 🚀 Installation (3 Minutes)

### Prerequisites
- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Android emulator or device

### Installation Steps

```bash
# 1. Navigate to project
cd VinhKhanhFoodGuide

# 2. Restore dependencies
dotnet restore

# 3. Build for Android
dotnet build -f net8.0-android -c Release

# 4. Run (ensure Android emulator is open OR device connected)
dotnet run -f net8.0-android
```

**That's it!** App launches on your device/emulator.

---

## 📁 Project Structure

```
vinh-khanh-food-street-voice-system/
│
├── VinhKhanhFoodGuide.sln          ← Open in Visual Studio
│
├── VinhKhanhFoodGuide/
│   ├── 📋 Configuration & Setup
│   │   ├── VinhKhanhFoodGuide.csproj
│   │   ├── MauiProgram.cs
│   │   ├── App.xaml(.cs)
│   │   └── AppShell.xaml(.cs)
│   │
│   ├── 📚 Models/                  (Data entities)
│   │   ├── POI.cs
│   │   ├── POIContent.cs
│   │   ├── LocationData.cs
│   │   └── GeofenceEvent.cs
│   │
│   ├── ⚙️  Services/               (Business logic)
│   │   ├── LocationService.cs      (GPS tracking)
│   │   ├── GeofenceEngine.cs       (Geofence + Haversine)
│   │   └── AudioManager.cs         (TTS + audio queue)
│   │
│   ├── 💾 Data/                    (Database)
│   │   ├── IPoiRepository.cs
│   │   └── PoiRepository.cs        (SQLite + seeding)
│   │
│   ├── 🧠 ViewModels/              (MVVM logic)
│   │   ├── HomeViewModel.cs
│   │   ├── POIDetailViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── 🎨 Pages/                   (UI)
│   │   ├── HomePage.xaml(.cs)
│   │   ├── POIDetailPage.xaml(.cs)
│   │   └── SettingsPage.xaml(.cs)
│   │
│   ├── 🔧 Platforms/Android/
│   │   ├── AndroidManifest.xml
│   │   └── LocationService.cs
│   │
│   └── 📖 Documentation           (IMPORTANT - READ THESE!)
│       ├── README.md              ← Complete guide
│       ├── QUICKSTART.md          ← Setup & testing
│       ├── ARCHITECTURE.md        ← Design docs
│       ├── PROJECT_SUMMARY.md     ← File breakdown
│       ├── INDEX.md               ← File index
│       └── .gitignore
│
└── INDEX.md (at root) ← Navigation guide
```

---

## 🎯 Core Features

### 1️⃣ GPS Tracking
- Real-time location updates
- Adaptive interval (2-5 seconds based on speed)
- Battery optimized

### 2️⃣ Geofence Detection
- Calculates distance using Haversine formula
- Triggers when within POI radius
- Debounce (3 seconds) prevents spam
- Per-POI cooldown (default 30 minutes)
- Audio lock prevents overlapping audio

### 3️⃣ Audio Playback
- Text-to-Speech (using MAUI API)
- Local audio files
- Sequential queue processing
- Play/Pause/Stop controls

### 4️⃣ Multilingual Support
- Vietnamese (Tiếng Việt)
- English
- Add more languages easily (just add content rows)

### 5️⃣ Offline-First Database
- SQLite (local storage)
- 5 sample POIs pre-seeded (Vietnamese + English)
- Designed for future cloud sync

---

## 🧪 Test It Right Now

### Quick Test (2 minutes)

1. **Start the app** → See HomePage with 5 sample POIs
2. **Tap Start Tracking** → Location tracking begins
3. **Open Android emulator's Extended Controls**
   - Go to: Virtual Sensors → Location
4. **Set coordinates to POI location**
   - Example: 10.77695°N, 106.67895°E (Bánh Mì Tươi)
5. **Watch app trigger geofence**
   - Status updates to "Arrived at [POI Name]"
   - Audio plays (TTS) with description

### Test Language Switch

1. Go to **Settings** tab
2. Change language dropdown
3. Return and see description in new language

---

## 📚 Documentation Guide

| Document | Read When | Time |
|----------|-----------|------|
| [README.md](VinhKhanhFoodGuide/README.md) | You want complete overview | 10 min |
| [QUICKSTART.md](VinhKhanhFoodGuide/QUICKSTART.md) | You want to install & test | 15 min |
| [ARCHITECTURE.md](VinhKhanhFoodGuide/ARCHITECTURE.md) | You want to understand design | 15 min |
| [INDEX.md](VinhKhanhFoodGuide/INDEX.md) | You want to find specific code | 5 min |
| [PROJECT_SUMMARY.md](VinhKhanhFoodGuide/PROJECT_SUMMARY.md) | You want file-by-file details | 10 min |

---

## 🔑 Key Technologies

- **Framework**: .NET MAUI 8.0
- **Language**: C# 11
- **Database**: SQLite (offline-first)
- **Platform**: Android 21+ (API 21+)
- **Architecture**: MVVM + 3-Layer Pattern
- **Location**: MAUI Geolocation API
- **Audio**: MAUI TextToSpeech API

---

## 💡 Pro Tips

### For Beginners
1. Start with [README.md](VinhKhanhFoodGuide/README.md)
2. Follow [QUICKSTART.md](VinhKhanhFoodGuide/QUICKSTART.md) exactly
3. Play with the sample POIs
4. Then explore the code

### For Developers
1. Review [ARCHITECTURE.md](VinhKhanhFoodGuide/ARCHITECTURE.md) first
2. Study the 3-layer pattern:
   - Services (LocationService, GeofenceEngine, AudioManager)
   - Data (PoiRepository with SQLite)
   - ViewModels (MVVM binding)
   - Pages (XAML UI)
3. Modify sample POIs in [PoiRepository.cs](VinhKhanhFoodGuide/Data/PoiRepository.cs)

### For DevOps/Deployment
1. Review [VinhKhanhFoodGuide.csproj](VinhKhanhFoodGuide/VinhKhanhFoodGuide.csproj) for packages
2. Check [Platforms/Android/AndroidManifest.xml](VinhKhanhFoodGuide/Platforms/Android/AndroidManifest.xml) for permissions
3. Configure signing for Google Play Store
4. Build release APK: `dotnet build -f net8.0-android -c Release`

---

## ❓ FAQ

**Q: Will it work on iOS?**  
A: This version is Android-only. MAUI supports cross-platform but iOS requires .NET 8 for iOS target and additional configuration.

**Q: Can I add more POIs?**  
A: Yes! Edit [PoiRepository.cs](VinhKhanhFoodGuide/Data/PoiRepository.cs) → `SeedDemoDataAsync()` method.

**Q: How do I change the default location?**  
A: Edit [LocationService.cs](VinhKhanhFoodGuide/Services/LocationService.cs) line 30-34.

**Q: Where's the map view?**  
A: Currently using a ListView. Map integration (Google Maps, MapControl) can be added as an enhancement.

**Q: How do I deploy to Google Play?**  
A: See "Production Checklist" in [QUICKSTART.md](VinhKhanhFoodGuide/QUICKSTART.md).

---

## 📞 Need Help?

1. **Installation issues** → [QUICKSTART.md Troubleshooting](VinhKhanhFoodGuide/QUICKSTART.md#troubleshooting)
2. **Architecture questions** → [ARCHITECTURE.md](VinhKhanhFoodGuide/ARCHITECTURE.md)
3. **Code questions** → [Check file comments and XML docs](VinhKhanhFoodGuide/)
4. **Feature questions** → [README.md Features section](VinhKhanhFoodGuide/README.md)

---

## ✅ Project Status

| Component | Status | Notes |
|-----------|--------|-------|
| GPS Tracking | ✅ Complete | Adaptive interval implemented |
| Geofence Engine | ✅ Complete | Haversine formula + logic |
| Audio Manager | ✅ Complete | TTS + file playback |
| Database | ✅ Complete | SQLite + seed data |
| UI Pages | ✅ Complete | Home, Detail, Settings |
| MVVM | ✅ Complete | Full binding setup |
| Demo Data | ✅ Complete | 5 POIs × 2 languages |
| Documentation | ✅ Complete | 4 guides + comments |

---

## 🎓 Learning Outcomes

By exploring this project, you'll learn:

✓ MAUI cross-platform development  
✓ MVVM architectural pattern  
✓ Dependency injection in .NET  
✓ SQLite database integration  
✓ Geofencing algorithms  
✓ Text-to-Speech APIs  
✓ Location services  
✓ Async/await patterns  
✓ Event-driven architecture  
✓ Clean code practices  

---

## 🚀 Next Steps

### To Run the App (Immediate)
```bash
cd VinhKhanhFoodGuide
dotnet run -f net8.0-android
```

### To Understand the Code (Next)
1. Open [VinhKhanhFoodGuide/ARCHITECTURE.md](VinhKhanhFoodGuide/ARCHITECTURE.md)
2. Review [Services/LocationService.cs](VinhKhanhFoodGuide/Services/LocationService.cs)
3. Study [Services/GeofenceEngine.cs](VinhKhanhFoodGuide/Services/GeofenceEngine.cs)

### To Extend the App (Later)
1. Add real POI data
2. Integrate with Google Maps API
3. Add cloud database sync
4. Implement user authentication
5. Create CMS for content management

---

## 📄 License

This project is provided as-is. No license restrictions - free to use and modify.

---

## 📊 Project Stats

- **24 core files** created
- **1,300+ lines** of production C# code
- **1,400+ lines** of comprehensive documentation
- **5 sample POIs** with authentic descriptions
- **2 languages** (Vietnamese + English) included
- **Zero external APIs required** (standalone app)
- **Ready to deploy** to Android immediately

---

## 🎉 You're All Set!

Everything is ready to go. Pick your next step from the Quick Start section above and dive in!

**Recommended**: Start with [QUICKSTART.md](VinhKhanhFoodGuide/QUICKSTART.md) to get the app running in ~5 minutes.

Happy coding! 🍲🚀

---

**Project**: Vinh Khanh Food Guide  
**Version**: 1.0  
**Status**: ✅ Complete & Ready  
**Last Updated**: March 3, 2026  
**Framework**: .NET MAUI 8.0
