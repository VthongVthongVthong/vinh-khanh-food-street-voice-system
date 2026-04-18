using System.ComponentModel;
using System.Runtime.CompilerServices;
using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Helpers
{
    /// <summary>
    /// Provides localized strings with INotifyPropertyChanged support.
    /// This allows XAML bindings to automatically update when language changes.
    /// </summary>
  public class LocalizationResourceProvider : INotifyPropertyChanged
    {
 private static LocalizationResourceProvider? _instance;
       private readonly LocalizationService _localizationService;
   private readonly LocalizationResourceManager _resourceManager;

public static LocalizationResourceProvider Instance =>
 _instance ??= new LocalizationResourceProvider();

       public event PropertyChangedEventHandler? PropertyChanged;

   public LocalizationResourceProvider()
        {
_localizationService = LocalizationService.Instance;
     _resourceManager = LocalizationResourceManager.Instance;

 // Subscribe to language changes
 _localizationService.PropertyChanged += (s, e) =>
      {
       if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
   {
     OnLanguageChanged();
       }
      };
      
      // ? NEW: Subscribe to ResourceManager language changes
      _resourceManager.LanguageChanged += (s, e) =>
      {
          OnLanguageChanged();
      };
}

   /// <summary>
      /// Get localized string by key
/// </summary>
    public string GetString(string key)
       {
    return _resourceManager.GetString(key) ?? key;
  }

     /// <summary>
   /// Get formatted string with parameters
       /// </summary>
        public string GetString(string key, params object?[] args)
    {
       var value = GetString(key);
   try
  {
          return string.Format(System.Globalization.CultureInfo.CurrentCulture, value, args);
 }
     catch
          {
   return value;
        }
      }

       private void OnLanguageChanged()
      {
  // ? Ensure we're on MainThread before notifying
  MainThread.BeginInvokeOnMainThread(() =>
      {
      // Notify all properties have changed
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
      });
  }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
