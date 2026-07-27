using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.ComponentModel;

namespace FusionCanvas.App.Ideation;

public partial class IdeationWindow : Window
{
    private bool _allowClose;
    private bool _confirmationOpen;
    private RejectIdeaWindow? _rejectWindow;

    public IdeationWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => GuidanceTextBox.Focus(), DispatcherPriority.Input);
        Closing += OnClosing;
    }

    private IdeationViewModel? ViewModel => DataContext as IdeationViewModel;

    private async void OnCreateCandidate(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: IdeaCandidateViewModel candidate } && ViewModel is { } viewModel)
        {
            await viewModel.CreateCandidateAsync(candidate);
            FocusNextCandidate();
        }
    }

    private async void OnRejectCandidate(object? sender, RoutedEventArgs e)
    {
        if (_rejectWindow is not null ||
            sender is not Button { DataContext: IdeaCandidateViewModel candidate } ||
            ViewModel is not { } viewModel)
        {
            return;
        }

        viewModel.RejectCandidateCommand.Execute(candidate);
        _rejectWindow = new RejectIdeaWindow { DataContext = viewModel };
        await _rejectWindow.ShowDialog(this);
        _rejectWindow = null;
        FocusNextCandidate();
    }

    private async void OnClearAll(object? sender, RoutedEventArgs e) =>
        await RequestDiscardAsync(close: false);

    private async void OnClose(object? sender, RoutedEventArgs e) =>
        await RequestDiscardAsync(close: true);

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || ViewModel is not { IsOpen: true })
        {
            return;
        }

        e.Cancel = true;
        Dispatcher.UIThread.Post(async () => await RequestDiscardAsync(close: true));
    }

    private async Task RequestDiscardAsync(bool close)
    {
        if (_confirmationOpen || ViewModel is not { } viewModel)
        {
            return;
        }

        if (close)
        {
            viewModel.RequestClose();
        }
        else
        {
            viewModel.RequestClear();
        }

        if (viewModel.IsDiscardConfirmationVisible)
        {
            _confirmationOpen = true;
            var confirmation = new IdeationDiscardConfirmationWindow
            {
                DataContext = viewModel
            };
            var confirmed = await confirmation.ShowDialog<bool>(this);
            _confirmationOpen = false;
            if (confirmed)
            {
                viewModel.ConfirmDiscard();
            }
            else
            {
                viewModel.CancelDiscard();
                return;
            }
        }

        if (close && !viewModel.IsOpen && IsVisible)
        {
            _allowClose = true;
            Close();
        }
    }

    private void FocusNextCandidate()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel?.Candidates.Count > 0)
            {
                CandidateList.SelectedIndex = 0;
                CandidateList.Focus();
            }
            else
            {
                GuidanceTextBox.Focus();
            }
        }, DispatcherPriority.Input);
    }
}
