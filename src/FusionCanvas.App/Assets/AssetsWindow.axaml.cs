using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.App.StageTools;

namespace FusionCanvas.App.Assets;

public partial class AssetsWindow : Window
{
    public AssetsWindow() => InitializeComponent();

    private async void OnPreviewClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AssetRowViewModel row } || row.Thumbnail is null)
            return;

        var preview = new AssetPreviewWindow { DataContext = row };
        await preview.ShowDialog(this);
    }
}
