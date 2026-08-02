using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FusionCanvas.App;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
