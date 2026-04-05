using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Behaviors
{
    /// <summary>
    /// Behavior to automatically update label text when language changes.
    /// Usage: <Label Text="{local:Translate Settings_Title}" behaviors:LocalizationBehavior.Key="Settings_Title" />
    /// </summary>
    public static class LocalizationBehavior
    {
   public static readonly BindableProperty KeyProperty = BindableProperty.CreateAttached(
"Key",
            typeof(string),
            typeof(LocalizationBehavior),
            null,
          propertyChanged: OnKeyChanged);

        public static string? GetKey(BindableObject bindable)
        {
            return (string?)bindable.GetValue(KeyProperty);
        }

      public static void SetKey(BindableObject bindable, string? value)
        {
       bindable.SetValue(KeyProperty, value);
    }

        private static void OnKeyChanged(BindableObject bindable, object oldValue, object newValue)
{
   if (bindable is Label label && newValue is string key)
       {
       label.Text = LocalizationResourceManager.Instance.GetString(key);
     }
     else if (bindable is Button button && newValue is string btnKey)
            {
                button.Text = LocalizationResourceManager.Instance.GetString(btnKey);
    }
  }
    }
}
