using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Helpers
{
    /// <summary>
 /// XAML Markup Extension for localization.
    /// Usage: Text="{local:Translate Settings_Title}"
    /// </summary>
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension<string>
  {
    public string Key { get; set; } = string.Empty;

        public string ProvideValue(IServiceProvider serviceProvider)
        {
          if (string.IsNullOrEmpty(Key))
       return string.Empty;

            var resourceManager = LocalizationResourceManager.Instance;
   return resourceManager.GetString(Key) ?? Key;
   }

      object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
         return ProvideValue(serviceProvider);
        }
    }
}
