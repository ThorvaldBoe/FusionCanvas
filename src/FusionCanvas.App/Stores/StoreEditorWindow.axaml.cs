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
            _subscribedViewModel.ProductNameFocusRequested -= OnProductNameFocusRequested;
            _subscribedViewModel.OfferingNameFocusRequested -= OnOfferingNameFocusRequested;
            _subscribedViewModel = null;
        }

        if (sender is not StoreEditorWindow { DataContext: StoreManagementViewModel viewModel })
        {
            return;
        }

        _subscribedViewModel = viewModel;
        viewModel.StoreNameFocusRequested += OnStoreNameFocusRequested;
        viewModel.ProductNameFocusRequested += OnProductNameFocusRequested;
        viewModel.OfferingNameFocusRequested += OnOfferingNameFocusRequested;
    }

    private void OnStoreNameFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StoreNameTextBox.Focus();
            StoreNameTextBox.SelectAll();
        });
    }

    private void OnProductNameFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ProductNameTextBox is not null)
            {
                ProductNameTextBox.Focus();
                ProductNameTextBox.SelectAll();
            }
        });
    }

    private void OnOfferingNameFocusRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            OfferingNameTextBox.Focus();
            OfferingNameTextBox.SelectAll();
        });
    }
}
