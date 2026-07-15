using Avalonia.Controls;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class StoreEditorWindow : Window
{
    private StoreManagementViewModel? _subscribedViewModel;

    public StoreEditorWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is StoreManagementViewModel viewModel && !viewModel.TryCloseStoreEditor())
        {
            e.Cancel = true;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.StoreNameFocusRequested -= OnStoreNameFocusRequested;
            _subscribedViewModel = null;
        }

        if (sender is not StoreEditorWindow { DataContext: StoreManagementViewModel viewModel })
        {
            return;
        }

        _subscribedViewModel = viewModel;
        viewModel.StoreNameFocusRequested += OnStoreNameFocusRequested;
    }

    private void OnStoreNameFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StoreNameTextBox.Focus();
            StoreNameTextBox.SelectAll();
        });
    }
}
