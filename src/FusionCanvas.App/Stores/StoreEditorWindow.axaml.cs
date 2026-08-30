using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.Application.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Stores;

public partial class StoreEditorWindow : Window
{
    private StoreManagementViewModel? _subscribedViewModel;
    private CatalogSetupViewModel? _subscribedCatalog;
    private bool _designAreaArchiveConfirmationOpen;
    private bool _optionValueManagementOpen;
    private bool _variantCreationDialogOpen;
    private bool _mockupTemplateEditorOpen;
    private bool _designAreaEditorOpen;

    internal IWindowGeometryStore? GeometryStore { get; set; }

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
            _subscribedCatalog.OptionValueManagementRequested -= OnOptionValueManagementRequested;
            _subscribedCatalog.OptionChoiceFocusRequested -= OnOptionChoiceFocusRequested;
            _subscribedCatalog.AddVariantRequested -= OnAddVariantRequested;
            _subscribedCatalog.VariantActionsFocusRequested -= OnVariantActionsFocusRequested;
            _subscribedCatalog.BulkVariantsRequested -= OnBulkVariantsRequested;
            _subscribedCatalog.BulkVariantActionFocusRequested -= OnBulkVariantActionFocusRequested;
            _subscribedCatalog.DesignAreaArchiveConfirmationRequested -= OnDesignAreaArchiveConfirmationRequested;
            _subscribedCatalog.DesignAreaArchiveFocusRequested -= OnDesignAreaArchiveFocusRequested;
            _subscribedCatalog.MockupTemplateEditorRequested -= OnMockupTemplateEditorRequested;
            _subscribedCatalog.DesignAreaEditorRequested -= OnDesignAreaEditorRequested;
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
            catalog.OptionValueManagementRequested += OnOptionValueManagementRequested;
            catalog.OptionChoiceFocusRequested += OnOptionChoiceFocusRequested;
            catalog.AddVariantRequested += OnAddVariantRequested;
            catalog.VariantActionsFocusRequested += OnVariantActionsFocusRequested;
            catalog.BulkVariantsRequested += OnBulkVariantsRequested;
            catalog.BulkVariantActionFocusRequested += OnBulkVariantActionFocusRequested;
            catalog.DesignAreaArchiveConfirmationRequested += OnDesignAreaArchiveConfirmationRequested;
            catalog.DesignAreaArchiveFocusRequested += OnDesignAreaArchiveFocusRequested;
            catalog.MockupTemplateEditorRequested += OnMockupTemplateEditorRequested;
            catalog.DesignAreaEditorRequested += OnDesignAreaEditorRequested;
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

    private async void OnOptionValueManagementRequested(object? sender, EventArgs e)
    {
        if (_optionValueManagementOpen || _subscribedCatalog is not { } catalog) return;
        _optionValueManagementOpen = true;
        try
        {
            var dialog = new OptionValueManagementWindow { DataContext = catalog };
            AttachGeometry(dialog, WindowLayoutKeys.OptionValueManagement);
            await dialog.ShowDialog(this);
            if (catalog.IsManagingOptionValues) catalog.CloseOptionValueManagementCommand.Execute(null);
        }
        finally
        {
            _optionValueManagementOpen = false;
        }
    }

    private void OnOptionChoiceFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        var optionId = _subscribedCatalog?.SelectedOptionId;
        var button = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(candidate =>
            candidate.DataContext is OfferingChoiceGroupViewModel group
            && group.Option.Id == optionId
            && string.Equals(candidate.Content as string, "Manage values", StringComparison.Ordinal));
        (button ?? AddOptionButton).Focus();
    });

    private void OnVariantActionsFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => AddVariantButton.Focus());

    private async void OnAddVariantRequested(object? sender, EventArgs e)
    {
        if (_variantCreationDialogOpen || _subscribedCatalog is not { } catalog) return;
        _variantCreationDialogOpen = true;
        try
        {
            var dialog = new AddVariantWindow { DataContext = catalog };
            AttachGeometry(dialog, WindowLayoutKeys.AddVariant);
            await dialog.ShowDialog(this);
            catalog.CancelAddVariantCommand.Execute(null);
        }
        finally
        {
            _variantCreationDialogOpen = false;
        }
    }

    private async void OnBulkVariantsRequested(object? sender, EventArgs e)
    {
        if (_variantCreationDialogOpen || _subscribedCatalog is not { } catalog) return;
        _variantCreationDialogOpen = true;
        try
        {
            var dialog = new BulkAddVariantsWindow { DataContext = catalog };
            AttachGeometry(dialog, WindowLayoutKeys.BulkAddVariants);
            await dialog.ShowDialog(this);
            catalog.CancelBulkVariantsCommand.Execute(null);
        }
        finally
        {
            _variantCreationDialogOpen = false;
        }
    }

    private void OnBulkVariantActionFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => BulkAddVariantButton.Focus());

    private async void OnDesignAreaEditorRequested(object? sender, EventArgs e)
    {
        if (_designAreaEditorOpen || _subscribedCatalog is not { } catalog) return;
        _designAreaEditorOpen = true;
        var originId = catalog.SelectedPlaceholderId;
        try
        {
            var dialog = new DesignAreaEditorWindow { DataContext = catalog };
            AttachGeometry(dialog, WindowLayoutKeys.DesignAreaEditor);
            await dialog.ShowDialog(this);
            if (catalog.IsAddingPlaceholder) catalog.CancelAddPlaceholderCommand.Execute(null);
        }
        finally
        {
            _designAreaEditorOpen = false;
            Dispatcher.UIThread.Post(() =>
            {
                if (originId is Guid id)
                {
                    var editButton = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(button =>
                        string.Equals(button.Content as string, "Edit", StringComparison.Ordinal)
                        && button.DataContext is DesignAreaCardViewModel card
                        && card.Id == id);
                    if (editButton is not null)
                    {
                        editButton.Focus();
                        return;
                    }
                }
                AddDesignAreaButton.Focus();
            });
        }
    }

    private async void OnDesignAreaArchiveConfirmationRequested(object? sender, EventArgs e)
    {
        if (_designAreaArchiveConfirmationOpen || _subscribedCatalog is not { } catalog) return;
        _designAreaArchiveConfirmationOpen = true;
        try
        {
            var confirmation = new DesignAreaArchiveConfirmationWindow { DataContext = catalog };
            var confirmed = await confirmation.ShowDialog<bool>(this);
            if (confirmed) catalog.ConfirmDesignAreaArchiveCommand.Execute(null);
            else catalog.CancelDesignAreaArchiveCommand.Execute(null);
        }
        finally
        {
            _designAreaArchiveConfirmationOpen = false;
        }
    }

    private void OnDesignAreaArchiveFocusRequested(object? sender, EventArgs e)
    {
        if (_subscribedCatalog is not { } catalog) return;
        var pendingId = catalog.PendingDesignAreaArchiveId;
        Dispatcher.UIThread.Post(() =>
        {
            if (pendingId is not Guid id) return;
            var archiveButton = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(button =>
                string.Equals(button.Content as string, "Archive", StringComparison.Ordinal)
                && button.DataContext is DesignAreaCardViewModel card
                && card.Id == id);
            archiveButton?.Focus();
        });
    }

    private async void OnMockupTemplateEditorRequested(object? sender, EventArgs e)
    {
        if (_mockupTemplateEditorOpen || _subscribedCatalog is not { } catalog) return;
        _mockupTemplateEditorOpen = true;
        var editedTemplateId = catalog.SelectedTemplateId;
        try
        {
            var dialog = new MockupTemplateEditorWindow { DataContext = catalog };
            AttachGeometry(dialog, WindowLayoutKeys.MockupTemplateEditor);
            await dialog.ShowDialog(this);
            if (catalog.IsAddingTemplate) catalog.CancelAddTemplateCommand.Execute(null);
        }
        finally
        {
            _mockupTemplateEditorOpen = false;
            Dispatcher.UIThread.Post(() =>
            {
                var editButton = editedTemplateId is Guid id
                    ? this.GetVisualDescendants().OfType<Button>().FirstOrDefault(button =>
                        button.DataContext is MockupTemplateCardViewModel card && card.Id == id)
                    : null;
                (editButton ?? AddMockupTemplateButton).Focus();
            });
        }
    }

    private void AttachGeometry(Window window, string key) 
    {
        if (GeometryStore is not null)
        {
            WindowGeometryPersistence.Attach(window, GeometryStore, key, window.MinWidth, window.MinHeight);
        }
    }
}
