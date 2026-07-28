using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.RejectedPhrases;

public partial class RejectedPhrasesWindow : Window
{
    private bool _allowClose;
    private RejectedPhrasesViewModel? _viewModel;

    public RejectedPhrasesWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not RejectedPhrasesViewModel viewModel)
        {
            return;
        }

        Attach(viewModel);
        if (!viewModel.IsLoaded)
        {
            await viewModel.OpenAsync(
                viewModel.Scope,
                viewModel.ScopeOptions,
                default);
        }

        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
    }

    private void Attach(RejectedPhrasesViewModel viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel.FocusPhraseRequested -= OnFocusPhraseRequested;
            _viewModel.FocusEditorRequested -= OnFocusEditorRequested;
        }

        _viewModel = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.FocusPhraseRequested += OnFocusPhraseRequested;
        _viewModel.FocusEditorRequested += OnFocusEditorRequested;
    }

    private void OnScopeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || e.AddedItems.Count == 0)
        {
            return;
        }

        if (e.AddedItems[0] is ScopeOption option)
        {
            _viewModel.SelectScopeCommand.Execute(option);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not RejectedPhrasesViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        viewModel.RequestClose();
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _allowClose = true;
        Close();
    }

    private void OnFocusPhraseRequested(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => PhraseBox.Focus(), DispatcherPriority.Input);

    private void OnFocusEditorRequested(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(
            () =>
            {
                if (PhraseBox.IsVisible)
                {
                    PhraseBox.Focus();
                }
                else
                {
                    SearchBox.Focus();
                }
            },
            DispatcherPriority.Input);

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
            _viewModel.FocusPhraseRequested -= OnFocusPhraseRequested;
            _viewModel.FocusEditorRequested -= OnFocusEditorRequested;
        }

        base.OnClosed(e);
    }
}
