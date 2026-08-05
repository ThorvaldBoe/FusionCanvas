using Avalonia.Controls;
using Avalonia.Threading;

namespace FusionCanvas.App.Items.Import;

public partial class ItemImportWindow : Window
{
    private bool _allowClose;
    private ItemImportViewModel? _viewModel;

    public ItemImportWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is not ItemImportViewModel viewModel)
        {
            return;
        }

        Attach(viewModel);
        viewModel.FilePicker = new AvaloniaItemCsvFilePicker(StorageProvider);
        Dispatcher.UIThread.Post(() => RawSourceBox.Focus(), DispatcherPriority.Input);
    }

    private void Attach(ItemImportViewModel viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not ItemImportViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        viewModel.CloseCommand.Execute(null);
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _allowClose = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        base.OnClosed(e);
    }
}
