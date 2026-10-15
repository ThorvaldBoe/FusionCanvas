using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.StageTools;

public partial class ArtworkAdjustmentDiscardWindow : Window
{
    public ArtworkAdjustmentDiscardWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => KeepEditingButton.Focus(), DispatcherPriority.Input);
    }

    private void OnKeepEditing(object? sender, RoutedEventArgs e) => Close(false);

    private void OnDiscard(object? sender, RoutedEventArgs e) => Close(true);
}
