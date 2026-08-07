using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FusionCanvas.App.StageTools;

public partial class DesignPreviewWindow : Window
{
    public DesignPreviewWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}