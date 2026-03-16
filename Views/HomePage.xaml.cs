using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
