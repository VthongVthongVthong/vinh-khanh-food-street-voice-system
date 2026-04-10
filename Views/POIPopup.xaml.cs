using VinhKhanhstreetfoods.ViewModels;

namespace VinhKhanhstreetfoods.Views
{
    /// <summary>
    /// POI Popup view - displays triggered POI with language selection and play button
    /// </summary>
    public partial class POIPopup : ContentView
    {
        private readonly POIPopupViewModel _viewModel;

        public POIPopup(POIPopupViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = _viewModel;
        }

        /// <summary>
        /// Animate popup entrance with fade and slide effect
        /// </summary>
        public async Task ShowWithAnimationAsync()
        {
            try
            {
                Opacity = 0;
                TranslationY = 50;

                await Task.WhenAll(
                  this.FadeTo(1, 300, Easing.CubicOut),
                    this.TranslateTo(0, 0, 300, Easing.CubicOut)
                 );

                System.Diagnostics.Debug.WriteLine("[POIPopup] Popup shown with animation");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POIPopup] Animation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Animate popup exit with fade and slide effect
        /// </summary>
        public async Task HideWithAnimationAsync()
        {
            try
            {
                await Task.WhenAll(
                  this.FadeTo(0, 200, Easing.CubicIn),
                  this.TranslateTo(0, 100, 200, Easing.CubicIn)
                );

                System.Diagnostics.Debug.WriteLine("[POIPopup] Popup hidden with animation");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POIPopup] Animation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update popup with smooth transition animation
        /// </summary>
        public async Task UpdateWithAnimationAsync()
        {
            try
            {
                // Subtle fade effect when updating content
                await this.FadeTo(0.7, 150, Easing.CubicInOut);
                await this.FadeTo(1, 150, Easing.CubicInOut);

                System.Diagnostics.Debug.WriteLine("[POIPopup] Popup updated with animation");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POIPopup] Animation error: {ex.Message}");
            }
        }
    }
}
