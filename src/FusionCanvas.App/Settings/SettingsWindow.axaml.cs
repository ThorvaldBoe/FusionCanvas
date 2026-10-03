using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.App.DesignSystem;
using FusionCanvas.App.TermsConsent;

namespace FusionCanvas.App.Settings;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (DataContext is SettingsViewModel settings && !settings.RequestClose())
            {
                args.Cancel = true;
            }
        };
    }

    private void OnOpenDesignSystemGallery(object? sender, RoutedEventArgs e)
    {
        new DesignSystemGalleryWindow().Show(this);
    }

    private void OnReviewTerms(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel settings)
        {
            var window = new TermsConsentWindow
            {
                DataContext = settings.CreateTermsConsentViewModel()
            };
            _ = window.ShowDialog(this);
        }
    }
}
