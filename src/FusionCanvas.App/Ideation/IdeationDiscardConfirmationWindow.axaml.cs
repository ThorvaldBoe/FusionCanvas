using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Ideation;

public partial class IdeationDiscardConfirmationWindow : Window
{
    public IdeationDiscardConfirmationWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => CancelButton.Focus(), DispatcherPriority.Input);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);
}
