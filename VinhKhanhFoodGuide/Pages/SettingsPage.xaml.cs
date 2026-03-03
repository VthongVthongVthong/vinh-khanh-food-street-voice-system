using VinhKhanhFoodGuide.ViewModels;

namespace VinhKhanhFoodGuide.Pages;

public partial class SettingsPage : ContentPage
{
    private SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Wire up UI controls to ViewModel
        LanguagePicker.SelectedIndex = _viewModel.AvailableLanguages.IndexOf(_viewModel.SelectedLanguage);
        TtsSwitch.IsToggled = _viewModel.IsTtsEnabled;
        IntervalSlider.Value = _viewModel.UpdateIntervalSeconds;
        UpdateIntervalLabel();

        LanguagePicker.SelectedIndexChanged += (s, e) =>
        {
            if (e.NewSelectedIndex >= 0)
                _viewModel.SelectedLanguage = _viewModel.AvailableLanguages[e.NewSelectedIndex];
        };

        TtsSwitch.Toggled += (s, e) => _viewModel.IsTtsEnabled = e.Value;

        IntervalSlider.ValueChanged += (s, e) =>
        {
            _viewModel.UpdateIntervalSeconds = (int)e.NewValue;
            UpdateIntervalLabel();
        };
    }

    private void UpdateIntervalLabel()
    {
        IntervalLabel.Text = $"{_viewModel.UpdateIntervalSeconds} seconds";
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        _viewModel.SaveSettings();
        DisplayAlert("Success", "Settings saved successfully", "OK");
    }
}
