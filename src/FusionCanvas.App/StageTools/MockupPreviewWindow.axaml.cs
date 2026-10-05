using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FusionCanvas.App.StageTools;

public partial class MockupPreviewWindow : Window
{
    public MockupPreviewWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
