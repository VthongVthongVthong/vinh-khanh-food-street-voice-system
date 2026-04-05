using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace VinhKhanhstreetfoods.Services
{
    /// <summary>
    /// Manages application localization and language switching.
    /// Supports 7 languages: Vietnamese, English, Chinese (Simplified), Japanese, Korean, French, Russian.
    /// </summary>
    public class LocalizationService : INotifyPropertyChanged
    {
        private string _currentLanguage = "vi";
        private static LocalizationService? _instance;

        public static LocalizationService Instance =>
            _instance ??= new LocalizationService();

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizationService()
        {
            _currentLanguage = InitializeLanguage();
            ApplyCultureInfo(_currentLanguage);
            Debug.WriteLine($"[LocalizationService] Initialized with language: {_currentLanguage}");
        }

        /// <summary>
        /// Get or set current application language
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                var normalized = NormalizeSupportedLanguage(value);
                if (_currentLanguage == normalized)
                    return;

                _currentLanguage = normalized;
                Preferences.Set("app_language", normalized);

                // Apply language to system culture
                ApplyCultureInfo(normalized);

                OnPropertyChanged();
                Debug.WriteLine($"[LocalizationService] Language changed to: {normalized}");
            }
        }

        /// <summary>
        /// Force UI refresh when resources are loaded but language value remains the same.
        /// </summary>
        public void NotifyLanguageRefreshed()
        {
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        /// <summary>
        /// Get all available languages
        /// </summary>
        public static readonly List<(string Code, string Name)> AvailableLanguages = new()
        {
            ("vi", "Tiếng Việt"),
            ("en", "English"),
            ("zh", "中文 (简体)"),
            ("ja", "日本語"),
            ("ko", "한국어"),
            ("fr", "Français"),
            ("ru", "Русский")
        };

        /// <summary>
        /// Get localized string by resource key
        /// </summary>
        public static string GetString(string key)
        {
            try
            {
                return LocalizationResourceManager.Instance.GetString(key) ?? key;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationService] Error getting string '{key}': {ex.Message}");
                return key;
            }
        }

        private static string InitializeLanguage()
        {
            try
            {
                // App-selected language has priority
                if (Preferences.ContainsKey("app_language"))
                {
                    var saved = Preferences.Get("app_language", "vi");
                    return NormalizeSupportedLanguage(saved);
                }

                // First launch: detect system language
                var systemTwoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var detected = NormalizeSupportedLanguage(systemTwoLetter);
                Preferences.Set("app_language", detected);

                Debug.WriteLine($"[LocalizationService] First launch detected system language: {systemTwoLetter} -> {detected}");
                return detected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationService] InitializeLanguage fallback: {ex.Message}");
                return "vi";
            }
        }

        private static string NormalizeSupportedLanguage(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return "vi";

            var normalized = languageCode.Trim().ToLowerInvariant().Replace('_', '-');
            var twoLetter = normalized.Contains('-') ? normalized[..normalized.IndexOf('-')] : normalized;

            return twoLetter switch
            {
                "vi" or "vn" => "vi",
                "en" => "en",
                "zh" => "zh",
                "ja" => "ja",
                "ko" => "ko",
                "fr" => "fr",
                "ru" => "ru",
                _ => "vi"
            };
        }

        /// <summary>
        /// Apply culture info based on language code
        /// </summary>
        private void ApplyCultureInfo(string languageCode)
        {
            try
            {
                var cultureInfo = languageCode switch
                {
                    "vi" => new CultureInfo("vi-VN"),
                    "en" => new CultureInfo("en-US"),
                    "zh" => new CultureInfo("zh-CN"),
                    "ja" => new CultureInfo("ja-JP"),
                    "ko" => new CultureInfo("ko-KR"),
                    "fr" => new CultureInfo("fr-FR"),
                    "ru" => new CultureInfo("ru-RU"),
                    _ => new CultureInfo("vi-VN")
                };

                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                Debug.WriteLine($"[LocalizationService] Applied culture: {cultureInfo.Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalizationService] Error applying culture: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
