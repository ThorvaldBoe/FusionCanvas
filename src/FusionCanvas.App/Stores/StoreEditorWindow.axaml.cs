using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
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
    private MockupTemplateEditorWindow? _mockupTemplateEditorWindow;
    private bool _designAreaEditorOpen;
    private StorePrintifyCredentialsViewModel? _printify;
    private PrintifyApiKeyWindow? _printifyDialog;

    private async void OnPrintifyEditRequested(object? sender, EventArgs e)
    {
        if (_printifyDialog is not null || _printify?.CreateEditor() is not { } model) return;
        var credentials = _printify;
        _printifyDialog = new PrintifyApiKeyWindow { DataContext = model };
        await _printifyDialog.ShowDialog(this);
        _printifyDialog = null;
        if (model.Saved) await credentials.RefreshAsync();
        PrintifyManageButton.Focus();
    }

    internal IWindowGeometryStore? GeometryStore { get; set; }

    public StoreEditorWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_subscribedViewModel?.PrintifyCatalogImportSession is { } printifyImport)
        {
            printifyImport.SelectionFocusRequested -= OnPrintifySelectionFocusRequested;
            printifyImport.ImportFocusRequested -= OnPrintifyImportFocusRequested;
        }
        if (_printify is not null)
        {
            _printify.EditRequested -= OnPrintifyEditRequested;
            _printify.CancelPending();
        }
        if (_mockupTemplateEditorWindow is { IsVisible: true } dialog)
        {
            dialog.Close();
        }

        base.OnClosed(e);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_printifyDialog is { } dialog)
        {
            dialog.Close();
            if (dialog.IsVisible) { e.Cancel = true; return; }
        }
        if (DataContext is StoreManagementViewModel viewModel && !viewModel.TryCloseStoreEditor())
        {
            e.Cancel = true;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_printify is not null) _printify.EditRequested -= OnPrintifyEditRequested;
        _printify = (DataContext as StoreManagementViewModel)?.PrintifyCredentials;
        if (_printify is not null) _printify.EditRequested += OnPrintifyEditRequested;
        if (_subscribedViewModel is not null)
        {
            if (_subscribedViewModel.PrintifyCatalogImportSession is { } previousImport)
            {
                previousImport.SelectionFocusRequested -= OnPrintifySelectionFocusRequested;
                previousImport.ImportFocusRequested -= OnPrintifyImportFocusRequested;
            }
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
        if (viewModel.PrintifyCatalogImportSession is { } printifyImport)
        {
            printifyImport.SelectionFocusRequested += OnPrintifySelectionFocusRequested;
            printifyImport.ImportFocusRequested += OnPrintifyImportFocusRequested;
        }
        if (viewModel.CatalogSetup is { } catalog)
        {
            _subscribedCatalog = catalog;
            if (TopLevel.GetTopLevel(this)?.StorageProvider is { } storageProvider)
                catalog.FilePicker = new AvaloniaAssetFilePicker(storageProvider);
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

    private void OnPrintifySelectionFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (PrintifyBlueprintList is { } list)
        {
            (list.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault() as Control ?? list).Focus();
        }
        else
        {
            PrintifyImportPanel.Focus();
        }
    });

    private void OnPrintifyImportFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => PrintifyImportButton.Focus());

    private void OnPrintifyImportKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not StoreManagementViewModel { PrintifyCatalogImportSession: { } session } || !session.IsOpen)
            return;

        if (e.Key == Key.Escape && !session.IsBusy)
        {
            session.CancelCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && session.CanConfirm)
        {
            session.ConfirmCommand.Execute(null);
            e.Handled = true;
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
        if (_mockupTemplateEditorWindow is { IsVisible: true } existingDialog)
        {
            existingDialog.Activate();
            return;
        }

        _mockupTemplateEditorOpen = true;
        var editedTemplateId = catalog.SelectedTemplateId;
        try
        {
            if (VisualRoot is null || !IsVisible)
            {
                // Do not create an ownerless editor during StoreEditor construction/teardown.
                // It would outlive this window and retain the application-wide geometry key.
                catalog.CancelAddTemplateCommand.Execute(null);
                return;
            }

            var dialog = new MockupTemplateEditorWindow { DataContext = catalog };
            _mockupTemplateEditorWindow = dialog;
            dialog.Closed += (_, _) =>
            {
                if (ReferenceEquals(_mockupTemplateEditorWindow, dialog))
                {
                    _mockupTemplateEditorWindow = null;
                }
            };
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
            WindowGeometryRegistrar.Register(window, GeometryStore, key, window.MinWidth, window.MinHeight);
        }
    }
}
