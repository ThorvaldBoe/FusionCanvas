using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FusionCanvas.App.Stores;

public partial class StoreEditorWindow : Window
{
    private StoreManagementViewModel? _subscribedViewModel;
    private CatalogSetupViewModel? _subscribedCatalog;

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
        if (_subscribedCatalog is not null)
        {
            _subscribedCatalog.OptionValueEditorFocusRequested -= OnOptionValueEditorFocusRequested;
            _subscribedCatalog.OptionChoiceFocusRequested -= OnOptionChoiceFocusRequested;
            _subscribedCatalog.VariantEditorFocusRequested -= OnVariantEditorFocusRequested;
            _subscribedCatalog.VariantActionsFocusRequested -= OnVariantActionsFocusRequested;
            _subscribedCatalog.BulkVariantEditorFocusRequested -= OnBulkVariantEditorFocusRequested;
            _subscribedCatalog.BulkVariantActionFocusRequested -= OnBulkVariantActionFocusRequested;
            _subscribedCatalog = null;
        }

        if (sender is not StoreEditorWindow { DataContext: StoreManagementViewModel viewModel })
        {
            return;
        }

        _subscribedViewModel = viewModel;
        viewModel.StoreNameFocusRequested += OnStoreNameFocusRequested;
        viewModel.ProductNameFocusRequested += OnProductNameFocusRequested;
        viewModel.OfferingNameFocusRequested += OnOfferingNameFocusRequested;
        if (viewModel.CatalogSetup is { } catalog)
        {
            _subscribedCatalog = catalog;
            catalog.OptionValueEditorFocusRequested += OnOptionValueEditorFocusRequested;
            catalog.OptionChoiceFocusRequested += OnOptionChoiceFocusRequested;
            catalog.VariantEditorFocusRequested += OnVariantEditorFocusRequested;
            catalog.VariantActionsFocusRequested += OnVariantActionsFocusRequested;
            catalog.BulkVariantEditorFocusRequested += OnBulkVariantEditorFocusRequested;
            catalog.BulkVariantActionFocusRequested += OnBulkVariantActionFocusRequested;
        }
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

    private void OnOptionValueEditorFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (_subscribedCatalog?.IsAddingOptionValue == true)
        {
            OptionValueTextBox.Focus();
            OptionValueTextBox.SelectAll();
        }
        else OptionValueDoneButton.Focus();
    });

    private void OnOptionChoiceFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        var optionId = _subscribedCatalog?.SelectedOptionId;
        var button = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(candidate =>
            candidate.DataContext is OfferingChoiceGroupViewModel group
            && group.Option.Id == optionId
            && string.Equals(candidate.Content as string, "Manage values", StringComparison.Ordinal));
        (button ?? AddOptionButton).Focus();
    });

    private void OnVariantEditorFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        VariantNameTextBox.Focus();
        VariantNameTextBox.SelectAll();
    });

    private void OnVariantActionsFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => AddVariantButton.Focus());

    private void OnBulkVariantEditorFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => BulkColorComboBox.Focus());

    private void OnBulkVariantActionFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => BulkAddVariantButton.Focus());
}
