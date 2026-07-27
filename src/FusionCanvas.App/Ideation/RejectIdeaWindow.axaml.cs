using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Ideation;

public partial class RejectIdeaWindow : Window
{
    public RejectIdeaWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => ReasonTextBox.Focus(), DispatcherPriority.Input);
        Closing += (_, _) =>
        {
            if (DataContext is IdeationViewModel { IsRejectionVisible: true } viewModel)
            {
                viewModel.CancelReject();
            }
        };
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        if (DataContext is IdeationViewModel viewModel)
        {
            viewModel.CancelReject();
        }

        Close();
    }

    private async void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not IdeationViewModel viewModel)
        {
            return;
        }

        await viewModel.ConfirmRejectAsync();
        if (!viewModel.IsRejectionVisible)
        {
            Close();
        }
    }
}
