using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Views;

public partial class RegisterPage : ContentPage
{
    private readonly UserService _userService;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public RegisterPage(UserService userService)
    {
        InitializeComponent();
        _userService = userService;
        BackgroundColor = Color.FromArgb("#80000000"); 
    
        _localizationService = LocalizationService.Instance;
        _resourceManager = LocalizationResourceManager.Instance;
      
        _localizationService.PropertyChanged += OnLanguageChanged;
        ApplyLocalizedText();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
        _localizationService.PropertyChanged += OnLanguageChanged;
        ApplyLocalizedText();
    }

    private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
        {
            MainThread.BeginInvokeOnMainThread(ApplyLocalizedText);
        }
    }

    private void ApplyLocalizedText()
    {
        Title = _resourceManager.GetString("Auth_Title_Register");
        TitleLabel.Text = _resourceManager.GetString("Auth_Title_Register");
        UsernameEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Username");
        EmailEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Email");
        PhoneEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Phone");
        PasswordEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Password");
        ConfirmPasswordEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_ConfirmPassword");
        RegisterButton.Text = _resourceManager.GetString("Auth_Button_Register");
        HaveAccountLabel.Text = _resourceManager.GetString("Auth_Label_HaveAccount");
        LoginNowLabel.Text = _resourceManager.GetString("Auth_Label_LoginNow");
        CloseButton.Text = _resourceManager.GetString("Auth_Button_Close");
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
            ShowError(_resourceManager.GetString("Auth_Error_Required"));
            return;
        }

        if (password != confirmPassword)
        {
            ShowError(_resourceManager.GetString("Auth_Error_PasswordMismatch"));
            return;
        }

        if (password.Length < 6)
        {
            ShowError(_resourceManager.GetString("Auth_Error_PasswordShort"));
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
            SuccessLabel.Text = _resourceManager.GetString("Auth_Success_RegisterComplete");
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
            ShowError(_resourceManager.GetString("Auth_Error_RegisterFailed"));
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
    }
}
