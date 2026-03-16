using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class SettingsPage : ContentPage
{
 public SettingsPage()
 {
 InitializeComponent();
 BindingContext = new SettingsViewModel();
 }
}
