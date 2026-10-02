using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FusionCanvas.App.DesignSystem;

public partial class DesignSystemGalleryWindow : Window
{
    public DesignSystemGalleryWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
