using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Stores;

public partial class StoreEditorWindow : Window
{
    private StoreManagementViewModel? _subscribedViewModel;
    private CatalogSetupViewModel? _subscribedCatalog;
    private readonly InMemoryDataProvider<SellableVariantRowViewModel> _sellableVariantRows = new([]);
    private readonly InMemoryDataProvider<MockupTemplateCardViewModel> _mockupTemplateRows = new([]);
    private Button? _pendingVariantArchiveButton;
    private bool _designAreaArchiveConfirmationOpen;
    private bool _optionValueManagementOpen;
    private bool _variantCreationDialogOpen;
    private bool _mockupTemplateEditorOpen;
    private MockupTemplateEditorWindow? _mockupTemplateEditorWindow;
    private bool _mockupTemplateArchiveConfirmationOpen;
    private MockupTemplateArchiveConfirmationWindow? _mockupTemplateArchiveConfirmationWindow;
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
        SellableVariantGrid.ItemsSource = _sellableVariantRows;
        MockupTemplateGrid.ItemsSource = _mockupTemplateRows;
        MockupTemplateGrid.AddHandler(InputElement.PointerPressedEvent, OnMockupTemplateGridPointerPressed,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        MockupTemplateGrid.AddHandler(InputElement.KeyDownEvent, OnMockupTemplateGridKeyDown,
            RoutingStrategies.Bubble, handledEventsToo: true);
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
        AddHandler(Button.ClickEvent, OnSellableVariantArchiveButtonClick, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(InputElement.PointerPressedEvent, OnSellableVariantPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.PointerReleasedEvent, OnSellableVariantPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnClosed(EventArgs e)
    {
        DetachViewModelSubscriptions();
        DetachCatalogSubscriptions();
        if (_printify is not null)
        {
            _printify.EditRequested -= OnPrintifyEditRequested;
            _printify.CancelPending();
            _printify = null;
        }
        if (_mockupTemplateEditorWindow is { IsVisible: true } dialog)
        {
            dialog.Close();
        }
        if (_mockupTemplateArchiveConfirmationWindow is { IsVisible: true } archiveDialog)
        {
            archiveDialog.Close(false);
        }

        DataContext = null;
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

        DetachViewModelSubscriptions();
        DetachCatalogSubscriptions();

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
            catalog.SellableVariantRows.CollectionChanged += OnSellableVariantRowsChanged;
            catalog.MockupTemplateCards.CollectionChanged += OnMockupTemplateCardsChanged;
            catalog.PropertyChanged += OnCatalogSetupPropertyChanged;
            RefreshSellableVariantRows();
            RefreshMockupTemplateRows();
            catalog.AttachStoreEditor();
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
            catalog.MockupTemplateArchiveConfirmationRequested += OnMockupTemplateArchiveConfirmationRequested;
            catalog.MockupTemplateEditorRequested += OnMockupTemplateEditorRequested;
            catalog.DesignAreaEditorRequested += OnDesignAreaEditorRequested;
        }
    }

    private void DetachViewModelSubscriptions()
    {
        if (_subscribedViewModel is null) return;

        if (_subscribedViewModel.PrintifyCatalogImportSession is { } printifyImport)
        {
            printifyImport.SelectionFocusRequested -= OnPrintifySelectionFocusRequested;
            printifyImport.ImportFocusRequested -= OnPrintifyImportFocusRequested;
        }
        _subscribedViewModel.StoreNameFocusRequested -= OnStoreNameFocusRequested;
        _subscribedViewModel.ProductNameFocusRequested -= OnProductNameFocusRequested;
        _subscribedViewModel.OfferingNameFocusRequested -= OnOfferingNameFocusRequested;
        _subscribedViewModel = null;
    }

    private void DetachCatalogSubscriptions()
    {
        if (_subscribedCatalog is null) return;

        _subscribedCatalog.SellableVariantRows.CollectionChanged -= OnSellableVariantRowsChanged;
        _subscribedCatalog.MockupTemplateCards.CollectionChanged -= OnMockupTemplateCardsChanged;
        _subscribedCatalog.PropertyChanged -= OnCatalogSetupPropertyChanged;
        _subscribedCatalog.DetachStoreEditor();
        _subscribedCatalog.OptionValueManagementRequested -= OnOptionValueManagementRequested;
        _subscribedCatalog.OptionChoiceFocusRequested -= OnOptionChoiceFocusRequested;
        _subscribedCatalog.AddVariantRequested -= OnAddVariantRequested;
        _subscribedCatalog.VariantActionsFocusRequested -= OnVariantActionsFocusRequested;
        _subscribedCatalog.BulkVariantsRequested -= OnBulkVariantsRequested;
        _subscribedCatalog.BulkVariantActionFocusRequested -= OnBulkVariantActionFocusRequested;
        _subscribedCatalog.DesignAreaArchiveConfirmationRequested -= OnDesignAreaArchiveConfirmationRequested;
        _subscribedCatalog.DesignAreaArchiveFocusRequested -= OnDesignAreaArchiveFocusRequested;
        _subscribedCatalog.MockupTemplateArchiveConfirmationRequested -= OnMockupTemplateArchiveConfirmationRequested;
        _subscribedCatalog.MockupTemplateEditorRequested -= OnMockupTemplateEditorRequested;
        _subscribedCatalog.DesignAreaEditorRequested -= OnDesignAreaEditorRequested;
        _subscribedCatalog = null;
        _pendingVariantArchiveButton = null;
        _sellableVariantRows.Reset([]);
        _mockupTemplateRows.Reset([]);
    }

    private void OnSellableVariantRowsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshSellableVariantRows();

    private void RefreshSellableVariantRows() => _sellableVariantRows.Reset(_subscribedCatalog?.SellableVariantRows.ToArray() ?? []);

    private void OnMockupTemplateCardsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshMockupTemplateRows();

    private void OnMockupTemplateGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down) || _subscribedCatalog is not { } catalog) return;

        var rows = catalog.FilteredMockupTemplateCards;
        if (rows.Count == 0) return;

        var selectedIndex = -1;
        for (var index = 0; index < rows.Count; index++)
        {
            if (rows[index].Id == catalog.SelectedMockupTemplateCard?.Id)
            {
                selectedIndex = index;
                break;
            }
        }

        var nextIndex = selectedIndex < 0
            ? (e.Key == Key.Down ? 0 : rows.Count - 1)
            : Math.Clamp(selectedIndex + (e.Key == Key.Down ? 1 : -1), 0, rows.Count - 1);
        var selectedRow = rows[nextIndex];
        MockupTemplateGrid.SelectedItem = selectedRow;
        catalog.SelectedMockupTemplateCard = selectedRow;
        e.Handled = true;
    }

    private void OnMockupTemplateGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(MockupTemplateGrid).Properties.IsLeftButtonPressed
            || _subscribedCatalog is not { } catalog
            || e.Source is not Visual source) return;

        var row = source.GetVisualAncestors().Prepend(source).OfType<VirtualDataRow>().FirstOrDefault();
        if (row?.DataContext is not MockupTemplateCardViewModel card
            || !catalog.FilteredMockupTemplateCards.Any(item => item.Id == card.Id)) return;

        MockupTemplateGrid.SelectedItem = card;
        catalog.SelectedMockupTemplateCard = card;
    }

    private void OnCatalogSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.SelectedMockupTemplateCard)
            && _subscribedCatalog is { } catalog)
        {
            MockupTemplateGrid.SelectedItem = catalog.SelectedMockupTemplateCard;
        }

        if (e.PropertyName is nameof(CatalogSetupViewModel.MockupTemplateSearchText)
            or nameof(CatalogSetupViewModel.FilteredMockupTemplateCards))
        {
            RefreshMockupTemplateRows();
        }
    }

    private void RefreshMockupTemplateRows()
    {
        var catalog = _subscribedCatalog;
        _mockupTemplateRows.Reset(catalog?.FilteredMockupTemplateCards ?? []);
        MockupTemplateGrid.SelectedItem = catalog?.SelectedMockupTemplateCard;
    }

    private void OnSellableVariantArchiveButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button
            && button.Classes.Contains("sellableVariantArchive")
            && button.CommandParameter is SellableVariantRowViewModel variant)
        {
            _subscribedCatalog?.ArchiveVariantCommand.Execute(variant);
        }
    }

    private void OnSellableVariantPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var row = FindSellableVariantRow(e.Source);
        if (row is null)
        {
            row = FindSellableVariantRowAtPoint(e.GetPosition(SellableVariantGrid));
        }

        if (row is null) return;
        var button = FindSellableVariantButton(row, e.Source);
        if (button is null)
            button = FindSellableVariantButtonAtPoint(row, e.GetPosition(SellableVariantGrid));
        if (button?.Classes.Contains("sellableVariantArchive") != true) return;

        _pendingVariantArchiveButton = button;
        e.Pointer.Capture(button);
        e.Handled = true;
    }

    private void OnSellableVariantPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var button = _pendingVariantArchiveButton;
        _pendingVariantArchiveButton = null;
        if (button is null) return;

        e.Pointer.Capture(null);
        var position = e.GetPosition(button);
        if (position.X >= 0 && position.Y >= 0 && position.X <= button.Bounds.Width && position.Y <= button.Bounds.Height
            && button.CommandParameter is SellableVariantRowViewModel variant)
        {
            _subscribedCatalog?.ArchiveVariantCommand.Execute(variant);
        }

        e.Handled = true;
    }

    private static VirtualDataRow? FindSellableVariantRow(object? source) => source is Visual visual
        ? visual.GetVisualAncestors().Prepend(visual).OfType<VirtualDataRow>().FirstOrDefault()
        : null;

    private VirtualDataRow? FindSellableVariantRowAtPoint(Point position) => SellableVariantGrid.GetVisualDescendants()
        .OfType<VirtualDataRow>()
        .FirstOrDefault(row => row.DataContext is SellableVariantRowViewModel
            && row.TranslatePoint(new Point(0, 0), SellableVariantGrid) is { } origin
            && new Rect(origin, row.Bounds.Size).Contains(position));

    private static Button? FindSellableVariantButton(VirtualDataRow row, object? source) => source is Visual visual
        ? visual.GetVisualAncestors().Prepend(visual).TakeWhile(candidate => !ReferenceEquals(candidate, row)).OfType<Button>().FirstOrDefault()
        : source as Button;

    private Button? FindSellableVariantButtonAtPoint(VirtualDataRow row, Point gridPosition) => row.GetVisualDescendants()
        .OfType<Button>()
        .FirstOrDefault(button => SellableVariantGrid.TranslatePoint(gridPosition, button) is { } position
            && new Rect(button.Bounds.Size).Contains(position));

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

        if (e.Key == Key.Escape)
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
            if (VisualRoot is null || !IsVisible)
            {
                // Catalog events can outlive the editor during teardown. Do not
                // attempt to show a modal dialog with a closed owner.
                catalog.CloseOptionValueManagementCommand.Execute(null);
                return;
            }

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
            if (VisualRoot is null || !IsVisible)
            {
                // Catalog events can outlive the editor during teardown. Do not
                // attempt to show a modal dialog with a closed owner.
                catalog.CancelAddPlaceholderCommand.Execute(null);
                return;
            }

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

    private async void OnMockupTemplateArchiveConfirmationRequested(object? sender, EventArgs e)
    {
        if (_mockupTemplateArchiveConfirmationOpen || _subscribedCatalog is not { } catalog) return;
        var templateId = catalog.PendingMockupTemplateArchiveId;
        if (templateId is null || VisualRoot is null || !IsVisible)
        {
            catalog.CancelMockupTemplateArchiveCommand.Execute(null);
            return;
        }

        _mockupTemplateArchiveConfirmationOpen = true;
        var dialog = new MockupTemplateArchiveConfirmationWindow { DataContext = catalog };
        _mockupTemplateArchiveConfirmationWindow = dialog;
        var confirmed = false;
        try
        {
            confirmed = await dialog.ShowDialog<bool>(this);
            if (confirmed)
            {
                catalog.ConfirmMockupTemplateArchiveCommand.Execute(null);
            }
            else
            {
                catalog.CancelMockupTemplateArchiveCommand.Execute(null);
            }
        }
        finally
        {
            if (ReferenceEquals(_mockupTemplateArchiveConfirmationWindow, dialog))
            {
                _mockupTemplateArchiveConfirmationWindow = null;
            }
            _mockupTemplateArchiveConfirmationOpen = false;
            RestoreMockupTemplateArchiveFocus(templateId.Value, confirmed);
        }
    }

    private void RestoreMockupTemplateArchiveFocus(Guid templateId, bool archived)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!archived)
            {
                var archiveButton = this.GetVisualDescendants().OfType<Button>()
                    .FirstOrDefault(button => button.CommandParameter is MockupTemplateCardViewModel card && card.Id == templateId
                        && AutomationProperties.GetName(button)?.StartsWith("Archive ", StringComparison.Ordinal) == true);
                if (archiveButton is not null && archiveButton.Focus()) return;
            }

            MockupTemplateSearchBox.Focus();
        }, DispatcherPriority.Input);
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
                        button.CommandParameter is MockupTemplateCardViewModel card && card.Id == id
                        && ReferenceEquals(button.Command, catalog.EditTemplateCommand))
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
