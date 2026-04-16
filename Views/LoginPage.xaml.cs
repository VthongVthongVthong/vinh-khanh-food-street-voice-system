using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Views;

public partial class LoginPage : ContentPage
{
    private readonly UserService _userService;

    public LoginPage(UserService userService)
    {
        InitializeComponent();
        _userService = userService;
        BackgroundColor = Color.FromArgb("#80000000"); 
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ErrorLabel.Text = "Nhập đủ thông tin";
            ErrorLabel.IsVisible = true;
            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        var user = await _userService.LoginAsync(UsernameEntry.Text, PasswordEntry.Text);

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (user != null)
        {
            // Lưu thông tin người dùng vào preferences
            Preferences.Set("LoggedInUserName", string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName);
            Preferences.Set("IsLoggedIn", true);
            
            // Gửi thông báo để SettingsViewModel cập nhật UI
            MessagingCenter.Send(this, "LoginSuccess");
            
            await Navigation.PopModalAsync();
        }
        else
        {
            ErrorLabel.Text = "Tài khoản hoặc mật khẩu sai!";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}