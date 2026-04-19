using VinhKhanhstreetfoods.Services;

namespace VinhKhanhstreetfoods.Views;

public partial class LoginPage : ContentPage
{
    private readonly UserService _userService;
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public LoginPage(UserService userService)
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
        Title = _resourceManager.GetString("Auth_Title_Login");
        TitleLabel.Text = _resourceManager.GetString("Auth_Title_Login");
        UsernameEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Username");
        PasswordEntry.Placeholder = _resourceManager.GetString("Auth_Placeholder_Password");
        LoginClickedButton.Text = _resourceManager.GetString("Auth_Button_Login");
        NoAccountLabel.Text = _resourceManager.GetString("Auth_Label_NoAccount");
        RegisterNowLabel.Text = _resourceManager.GetString("Auth_Label_RegisterNow");
        CloseButton.Text = _resourceManager.GetString("Auth_Button_Close");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ErrorLabel.Text = _resourceManager.GetString("Auth_Error_Required");
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
            ErrorLabel.Text = _resourceManager.GetString("Auth_Error_LoginFailed");
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPage(_userService));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
    }
}