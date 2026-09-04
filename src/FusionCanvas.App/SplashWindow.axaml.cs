using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Versioning;

namespace FusionCanvas.App;

public partial class SplashWindow : Window
{
    public SplashWindow()
        : this(null)
    {
    }

    public SplashWindow(IApplicationVersionProvider? versionProvider)
    {
        DataContext = (versionProvider ?? new AssemblyApplicationVersionProvider()).GetVersion();
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
