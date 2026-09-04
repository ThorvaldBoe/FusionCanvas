using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FusionCanvas.App.Assets;

public partial class AssetPreviewWindow : Window
{
    public AssetPreviewWindow() => InitializeComponent();

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
