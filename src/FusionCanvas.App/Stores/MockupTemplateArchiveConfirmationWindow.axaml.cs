using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class MockupTemplateArchiveConfirmationWindow : Window
{
    public MockupTemplateArchiveConfirmationWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => CancelButton.Focus(), DispatcherPriority.Input);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);

    private void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(true);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
