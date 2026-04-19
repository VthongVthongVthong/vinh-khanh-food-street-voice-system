using VinhKhanhstreetfoods.Services;
using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views;

public partial class SettingsPage : ContentPage
{
    private readonly LocalizationService _localizationService;
    private readonly LocalizationResourceManager _resourceManager;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

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
        Title = _resourceManager.GetString("Settings_Title");

        HeaderLabel.Text = _resourceManager.GetString("Settings_Header");
      
        // Profile Section
        LoginButton.Text = _resourceManager.GetString("Auth_Button_Login");
        LogoutButton.Text = _resourceManager.GetString("Auth_Button_Logout");
        UpdateProfileAccountLabel();
        
        LanguageSectionLabel.Text = _resourceManager.GetString("Settings_Language_Section");
        UiLanguageLabel.Text = _resourceManager.GetString("Settings_Language_UI");
        UiLanguagePicker.Title = _resourceManager.GetString("Settings_Language_UIPickerTitle");

        NarrationLanguageLabel.Text = _resourceManager.GetString("Settings_Language_Narration");
        NarrationLanguagePicker.Title = _resourceManager.GetString("Settings_Language_NarrationPickerTitle");

        DataFromDbButton.Text = _resourceManager.GetString("Settings_Language_DataFromDB");
        DownloadTitleLabel.Text = _resourceManager.GetString("Settings_Download_Title");

        AudioTitleLabel.Text = _resourceManager.GetString("Settings_Audio_Title");
        EnableAudioLabel.Text = _resourceManager.GetString("Settings_Audio_Enable");
        AutoPlayLabel.Text = _resourceManager.GetString("Settings_Audio_AutoPlay");

        CooldownTitleLabel.Text = _resourceManager.GetString("Settings_Cooldown_Title");
        TriggerTitleLabel.Text = _resourceManager.GetString("Settings_TriggerRadius_Title");

        ResetButton.Text = _resourceManager.GetString("Settings_Button_Reset");
        ApplyingLanguageLabel.Text = _resourceManager.GetString("Common_Loading");

        // Refresh string format text according to language by forcing binding re-evaluation.
        CooldownValueLabel.RemoveBinding(Label.TextProperty);
        CooldownValueLabel.SetBinding(Label.TextProperty, new Binding("CooldownMinutes", stringFormat: _resourceManager.GetString("Settings_Cooldown_Current")));

        TriggerValueLabel.RemoveBinding(Label.TextProperty);
        TriggerValueLabel.SetBinding(Label.TextProperty, new Binding("TriggerRadiusMeters", stringFormat: _resourceManager.GetString("Settings_TriggerRadius_Current")));
    }
    
    private void UpdateProfileAccountLabel()
    {
        var viewModel = BindingContext as SettingsViewModel;
    if (viewModel != null)
        {
      ProfileAccountLabel.FormattedText = null;
            ProfileAccountLabel.SetBinding(Label.TextProperty, new Binding("LoggedInUserName", stringFormat: _resourceManager.GetString("Auth_Profile_Account")));
     }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _localizationService.PropertyChanged -= OnLanguageChanged;
    }
}
