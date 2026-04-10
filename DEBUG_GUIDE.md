// ?? DEBUG TRACING GUIDE FOR POPUP SYSTEM
// S? d?ng file này ?? trace các l?i khi app ch?y

/*
 * STARTUP FLOW:
 * 
 * 1. App.xaml.cs -> OnStart()
 *    ?? Localization preload
 *    ?? Warmup cache
 * 
 * 2. AppShell.xaml.cs -> constructor
 *    ?? [AppShell] InitializeComponent() called
 *    ?? [AppShell] ? Overlay created and subscribed
 *    ?? [AppShell] ? Overlay initialized
 *    ?? [AppShell] ? Tab titles updated
 *
 * 3. HomePage.xaml.cs -> Geofencing starts
 *    ?? [LocationService] Location listening started
 *    ?? [GeofenceEngine] ?? User at coordinates
 *    ?? [GeofenceEngine] ?? TRIGGER: POI Name (distance)
 *
 * 4. HybridPopupService -> HandleIncomingPOIAsync()
 *    ?? [HybridPopupService] ?? Showing POI
 *    ?? PopupRequested event fired
 *  ?? [AppShell] ?? Popup for POI
 *
 * 5. HybridPOIPopupOverlay -> OnPopupRequested()
 *?? [HybridPOIPopupOverlay] ?? ShowPopup START
 *    ?? [HybridPOIPopupViewModel] Selected POI: Name
 *    ?? [HybridPOIPopup] Swipe gestures configured
 * ?? [HybridPOIPopupOverlay] ?? Backdrop animation done
 *    ?? [HybridPOIPopup] Popup shown with animation
 *    ?? [HybridPOIPopupOverlay] ?? ShowPopup COMPLETE
 *
 * 6. User closes popup (auto or manual)
 *    ?? PopupClosed event fired
 *    ?? [HybridPOIPopupOverlay] ?? ClosePopup START
 *    ?? [HybridPOIPopup] Popup hidden with animation
 *    ?? [HybridPOIPopupOverlay] ? Popup hide animation done
 * ?? [HybridPOIPopupOverlay] ? Backdrop hide animation done
 *    ?? [HybridPOIPopupOverlay] ?? ClosePopup COMPLETE
 */

/*
 * ERROR PATTERNS TO WATCH:
 * 
 * ??  Pattern 1: "?? Navigation unavailable"
 *     Cause: AppShell.CurrentPage is null
 *     Fix: Already fixed - don't push as modal
 * 
 * ??  Pattern 2: "? PopupContainer is null"
 *     Cause: XAML not compiled properly
 *     Fix: Ensure <MauiXaml> in csproj
 * 
 * ??  Pattern 3: "? BackdropOverlay is null"
 *     Cause: FindByName() returned null
 *     Fix: Check x:Name in XAML matches code
 * 
 * ??  Pattern 4: No popup appearing
 *     Cause: Event not firing or overlay not in visual tree
 *     Fix: Check InitializeComponent() is called
 * 
 * ??  Pattern 5: "NullReferenceException in animation"
 *     Cause: Using control before null check
 *     Fix: Always check if (_control != null)
 *
 * ??  Pattern 6: "The type or namespace name 'X' does not exist"
 *     Cause: Namespace mismatch in XAML x:Class vs code-behind
 *     Fix: Ensure x:Class="Namespace.ClassName" matches namespace
 */

/*
 * XAML COMPILATION CHECKLIST:
 * 
 * For each .xaml file in csproj, must have:
 * ? <MauiXaml Update="Path/File.xaml">
 *   <Generator>MSBuild:Compile</Generator>
 *   </MauiXaml>
 * 
 * Check:
 * - AppShell.xaml: ? (added by us)
 * - App.xaml: ? (added by us)
 * - Pages/HybridPOIPopupOverlay.xaml: ?
 * - Views/HybridPOIPopup.xaml: ?
 * - Views/SettingsPage.xaml: ?
 * - Pages/POIPopupOverlay.xaml: ?
 * - Views/POIPopup.xaml: ?
 */

/*
 * CODE-BEHIND BEST PRACTICES:
 * 
 * 1. ALWAYS be "partial class"
 *    public partial class MyPage : ContentPage { }
 * 
 * 2. ALWAYS call InitializeComponent() FIRST in constructor
 * public MyPage() {
 *        InitializeComponent();  // ? FIRST!
 *        // ... other code
 *    }
 * 
 * 3. ALWAYS check FindByName() result
 *    var control = this.FindByName<MyControl>("MyControl");
 *    if (control == null) {
 *        Debug.WriteLine("Control not found!");
 *   return;
 *    }
 * 
 * 4. ALWAYS wrap animations in try-catch
 *    try {
 *   await control.FadeTo(...);
 *    } catch (Exception ex) {
 *      Debug.WriteLine($"Animation error: {ex.Message}");
 *    }
 * 
 * 5. ALWAYS hook events in constructor
 *    service.EventName += OnEventHandler;
 * 
 * 6. ALWAYS unhook events in OnDisappearing()
 *    protected override void OnDisappearing() {
 *        base.OnDisappearing();
 *     service.EventName -= OnEventHandler;
 *    }
 */

/*
 * DEBUG OUTPUT LEVELS:
 * 
 * ??  Animation events
 * ??  Completion events
 * ??  Location/POI events
 * ??  Audio events
 * ??  Visual events
 * ??  Cleanup events
 * ?  Success events
 * ??   Warning events
 * ?  Error events
 * ??  State change events
 * ??  Data load events
 */

/*
 * TESTING SCENARIOS:
 * 
 * Scenario 1: App startup
 *   Expected: All [AppShell] messages show ?
 *   If error: Check InitializeComponent() is called
 * 
 * Scenario 2: Trigger POI via geofence
 *   Expected: ?? ShowPopup START ? ?? ShowPopup COMPLETE
 *   If error: Check PopupContainer is not null
 * 
 * Scenario 3: Close popup
 *   Expected: ?? ClosePopup START ? ?? ClosePopup COMPLETE
 *   If error: Check animation handlers
 * 
 * Scenario 4: Change language
 *   Expected: Tab titles update correctly
 *   If error: Check FindByName() works
 */
