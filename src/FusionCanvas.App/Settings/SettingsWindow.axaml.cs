using Avalonia.Controls;

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
}
