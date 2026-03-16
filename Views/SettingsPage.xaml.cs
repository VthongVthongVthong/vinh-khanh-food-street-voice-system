using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
