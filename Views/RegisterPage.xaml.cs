using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Views;

public partial class RegisterPage : ContentPage
{
    private readonly UserService _userService;

    public RegisterPage(UserService userService)
    {
        InitializeComponent();
        _userService = userService;
        BackgroundColor = Color.FromArgb("#80000000"); 
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        SuccessLabel.IsVisible = false;

        string username = UsernameEntry.Text?.Trim();
        string email = EmailEntry.Text?.Trim();
        string phone = PhoneEntry.Text?.Trim();
        string password = PasswordEntry.Text;
        string confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phone))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Mật khẩu không khớp.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải từ 6 ký tự trở lên.");
            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        RegisterButton.IsEnabled = false;

        bool success = await _userService.RegisterAsync(username, email, phone, password);

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        RegisterButton.IsEnabled = true;

        if (success)
        {
            SuccessLabel.Text = "Đăng ký thành công! Vui lòng đăng nhập.";
            SuccessLabel.IsVisible = true;
            
            // Tự động clear form
            UsernameEntry.Text = "";
            EmailEntry.Text = "";
            PhoneEntry.Text = "";
            PasswordEntry.Text = "";
            ConfirmPasswordEntry.Text = "";

            // Tự động đóng modal sau 1.5s
            await Task.Delay(1500);
            await Navigation.PopModalAsync();
        }
        else
        {
            ShowError("Đăng ký thất bại. Email hoặc tài khoản có thể đã tồn tại.");
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        // Có thể user đang mượn Register Page làm modal riêng, 
        // lúc đóng ta pop về 
        await Navigation.PopModalAsync();
    }
}
