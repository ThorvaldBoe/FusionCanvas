using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Diagnostics;
using System.ComponentModel;
using FusionCanvas.App.Settings;
using FusionCanvas.App.RejectedPhrases;
using FusionCanvas.App.Snowclones;
using FusionCanvas.App.Views;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Ideation;

public partial class IdeationWindow : Window
{
    private bool _allowClose;
    private bool _confirmationOpen;
    private RejectIdeaWindow? _rejectWindow;
    private SnowcloneLibraryWindow? _snowcloneLibraryWindow;
    private RejectedPhrasesWindow? _rejectedPhrasesWindow;

    internal IWindowGeometryStore? GeometryStore { get; set; }

    public IdeationWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => GuidanceTextBox.Focus(), DispatcherPriority.Input);
        Closing += OnClosing;
    }

    private IdeationViewModel? ViewModel => DataContext as IdeationViewModel;

    private void OnCreateCandidate(object? sender, RoutedEventArgs e) =>
        Run(async () =>
        {
            if (sender is Button { DataContext: IdeaCandidateViewModel candidate } && ViewModel is { } viewModel)
            {
                await viewModel.CreateCandidateAsync(candidate);
                FocusNextCandidate();
            }
        });

    private void OnRejectCandidate(object? sender, RoutedEventArgs e) =>
        Run(async () =>
        {
            if (_rejectWindow is not null ||
                sender is not Button { DataContext: IdeaCandidateViewModel candidate } ||
                ViewModel is not { } viewModel)
            {
                return;
            }

            viewModel.RejectCandidateCommand.Execute(candidate);
            _rejectWindow = new RejectIdeaWindow { DataContext = viewModel };
            if (GeometryStore is { } rejectStore)
            {
                WindowGeometryRegistrar.Register(_rejectWindow, rejectStore, WindowLayoutKeys.RejectIdea, _rejectWindow.MinWidth, _rejectWindow.MinHeight);
            }
            await _rejectWindow.ShowDialog(this);
            _rejectWindow = null;
            FocusNextCandidate();
        });

    private void OnClearAll(object? sender, RoutedEventArgs e) =>
        Run(() => RequestDiscardAsync(close: false));

    private void OnManageSnowclones(object? sender, RoutedEventArgs e) =>
        Run(async () =>
        {
            if (_snowcloneLibraryWindow is not null || ViewModel is not { } viewModel)
            {
                return;
            }

            viewModel.OpenSnowcloneLibrary();
            if (viewModel.SnowcloneLibrary is not { } library)
            {
                return;
            }

            _snowcloneLibraryWindow = new SnowcloneLibraryWindow { DataContext = library };
            if (GeometryStore is { } snowcloneStore)
            {
                WindowGeometryRegistrar.Register(_snowcloneLibraryWindow, snowcloneStore, WindowLayoutKeys.SnowcloneLibrary, _snowcloneLibraryWindow.MinWidth, _snowcloneLibraryWindow.MinHeight);
            }
            await _snowcloneLibraryWindow.ShowDialog(this);
            _snowcloneLibraryWindow = null;
            await viewModel.CompleteSnowcloneLibraryAsync();
            Dispatcher.UIThread.Post(() => ManageSnowclonesButton.Focus(), DispatcherPriority.Input);
        });

    private void OnManageRejectedPhrases(object? sender, RoutedEventArgs e) =>
        Run(async () =>
        {
            if (_rejectedPhrasesWindow is not null || ViewModel is not { } viewModel)
            {
                return;
            }

            viewModel.OpenRejectedPhrases();
            if (viewModel.RejectedPhrases is not { } manager)
            {
                return;
            }

            _rejectedPhrasesWindow = new RejectedPhrasesWindow { DataContext = manager };
            if (GeometryStore is { } rejectedPhrasesStore)
            {
                WindowGeometryRegistrar.Register(_rejectedPhrasesWindow, rejectedPhrasesStore, WindowLayoutKeys.RejectedPhrases, _rejectedPhrasesWindow.MinWidth, _rejectedPhrasesWindow.MinHeight);
            }
            await _rejectedPhrasesWindow.ShowDialog(this);
            _rejectedPhrasesWindow = null;
            await viewModel.CompleteRejectedPhrasesAsync();
            Dispatcher.UIThread.Post(() => ManageRejectedPhrasesButton.Focus(), DispatcherPriority.Input);
        });

    private void OnClose(object? sender, RoutedEventArgs e) =>
        Run(() => RequestDiscardAsync(close: true));

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_snowcloneLibraryWindow is not null)
        {
            e.Cancel = true;
            _snowcloneLibraryWindow.Close();
            return;
        }

        if (_rejectedPhrasesWindow is not null)
        {
            e.Cancel = true;
            _rejectedPhrasesWindow.Close();
            return;
        }

        if (_allowClose || ViewModel is not { IsOpen: true })
        {
            return;
        }

        e.Cancel = true;
        RunOnDispatcher(() => RequestDiscardAsync(close: true));
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

    private void Run(Func<Task> operation) => _ = ObserveCallbackAsync(operation);

    private void RunOnDispatcher(Func<Task> operation) =>
        _ = ObserveCallbackAsync(() => Dispatcher.UIThread.InvokeAsync(operation));

    internal static async Task ObserveCallbackAsync(
        Func<Task> operation,
        Action<Exception>? reportFailure = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            await operation().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            (reportFailure ?? ReportCallbackFailure)(exception);
        }
    }

    private static void ReportCallbackFailure(Exception exception) =>
        Trace.TraceError("Ideation window async callback failed: {0}", exception);
}
