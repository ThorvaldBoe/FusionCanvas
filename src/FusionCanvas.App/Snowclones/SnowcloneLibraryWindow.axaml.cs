using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Snowclones;

public partial class SnowcloneLibraryWindow : Window
{
    private bool _allowClose;
    private SnowcloneLibraryViewModel? _viewModel;

    public SnowcloneLibraryWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not SnowcloneLibraryViewModel viewModel)
        {
            return;
        }

        Attach(viewModel);
        viewModel.FilePicker = new AvaloniaSnowcloneCsvFilePicker(StorageProvider);
        if (!viewModel.IsLoaded)
        {
            await viewModel.OpenAsync();
        }

        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
    }

    private void Attach(SnowcloneLibraryViewModel viewModel)
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

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not SnowcloneLibraryViewModel viewModel)
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
