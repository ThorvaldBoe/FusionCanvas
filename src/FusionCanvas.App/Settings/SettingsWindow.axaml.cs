using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.App.DesignSystem;

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
}
