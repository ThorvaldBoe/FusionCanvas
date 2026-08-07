using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Ideation;
using FusionCanvas.App.Items;
using FusionCanvas.App.Items.Import;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Application.WorkspaceTree;

namespace FusionCanvas.App.Views;

public partial class MainWindow : Window
{
    private StoreEditorWindow? _storeEditorWindow;
    private WorkspaceManagementWindow? _workspaceManagementWindow;
    private SettingsWindow? _settingsWindow;
    private AssetsWindow? _assetsWindow;
    private IdeationWindow? _ideationWindow;
    private Window? _designPreviewWindow;
    private PointerPressedEventArgs? _dragPointerArgs;
    private WorkspaceTreeNodeViewModel? _dragNode;
    private Avalonia.Point _dragStart;
    private WorkspaceTreeNodeViewModel? _dropTarget;

    public MainWindow()
    {
        InitializeComponent();
        WorkspaceTreeControl.AddHandler(PointerPressedEvent, OnWorkspaceTreePointerPressed, RoutingStrategies.Tunnel);
    }

    public MainWindow(AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        InitializeComponent();
        WorkspaceTreeControl.AddHandler(PointerPressedEvent, OnWorkspaceTreePointerPressed, RoutingStrategies.Tunnel);
        var viewModel = MainWindowViewModel.CreateForDefaultWorkspace(
            services.Settings,
            services.AiTextGeneration);
        viewModel.WorkspaceManagement.PackagePicker = new AvaloniaWorkspacePackagePicker(StorageProvider);
        viewModel.WorkspaceTree.FilePicker = new FusionCanvas.App.Items.AvaloniaItemCsvFilePicker(StorageProvider);
        viewModel.WorkspaceTree.CsvCodec = new FusionCanvas.Integration.Items.ItemCsvCodec();
        viewModel.StoreManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StoreManagementViewModel.IsStoreEditorOpen))
            {
                SyncStoreEditorWindow(viewModel.StoreManagement);
            }
        };
        viewModel.WorkspaceManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkspaceManagementViewModel.IsWorkspaceManagementOpen))
            {
                SyncWorkspaceManagementWindow(viewModel.WorkspaceManagement);
            }
        };
        viewModel.AssetsManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AssetsViewModel.IsOpen))
            {
                SyncAssetsWindow(viewModel.AssetsManagement);
            }
        };
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.IsStatusConfirmationVisible)
                && viewModel.IsStatusConfirmationVisible)
            {
                Dispatcher.UIThread.Post(() => CancelStatusChangeButton.Focus(), DispatcherPriority.Input);
            }
        };
        viewModel.Ideation.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IdeationViewModel.IsOpen))
            {
                SyncIdeationWindow(viewModel.Ideation);
            }
        };
        viewModel.DesignTool.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DesignStageToolViewModel.ShowPreviewDialog))
            {
                SyncDesignPreviewWindow(viewModel.DesignTool);
            }
        };
        viewModel.Settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.IsOpen))
            {
                SyncSettingsWindow(viewModel.Settings);
            }
        };
        DataContext = viewModel;
        SyncSettingsWindow(viewModel.Settings);
        SyncWorkspaceManagementWindow(viewModel.WorkspaceManagement);
        SyncStoreEditorWindow(viewModel.StoreManagement);
        SyncAssetsWindow(viewModel.AssetsManagement);
        SyncIdeationWindow(viewModel.Ideation);
    }

    private void SyncSettingsWindow(SettingsViewModel settings)
    {
        if (settings.IsOpen && _settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow { DataContext = settings };
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                if (settings.IsOpen)
                {
                    settings.CloseCommand.Execute(null);
                }

                if (CanFocusOwner(this))
                {
                    this.Activate();
                }
            };
            _settingsWindow.Show(this);
            return;
        }

        if (!settings.IsOpen && _settingsWindow is not null)
        {
            _settingsWindow.Close();
        }
    }

    private void SyncWorkspaceManagementWindow(WorkspaceManagementViewModel workspaceManagement)
    {
        if (workspaceManagement.IsWorkspaceManagementOpen && _workspaceManagementWindow is null)
        {
            _workspaceManagementWindow = new WorkspaceManagementWindow { DataContext = workspaceManagement };
            _workspaceManagementWindow.Closed += (_, _) =>
            {
                _workspaceManagementWindow = null;
                if (workspaceManagement.IsWorkspaceManagementOpen)
                {
                    workspaceManagement.CloseWorkspaceManagementCommand.Execute(null);
                }

                if (_settingsWindow is { } settings && settings.IsVisible)
                {
                    settings.Activate();
                }
            };
            _workspaceManagementWindow.Show((Window?)_settingsWindow ?? this);
            return;
        }

        if (!workspaceManagement.IsWorkspaceManagementOpen && _workspaceManagementWindow is not null)
        {
            _workspaceManagementWindow.Close();
        }
    }

    private static bool CanFocusOwner(Window window)
    {
        try { return window.IsVisible; }
        catch { return false; }
    }

    private void SyncStoreEditorWindow(StoreManagementViewModel storeManagement)
    {
        if (storeManagement.IsStoreEditorOpen && _storeEditorWindow is null)
        {
            _storeEditorWindow = new StoreEditorWindow { DataContext = storeManagement };
            _storeEditorWindow.Closed += (_, _) =>
            {
                _storeEditorWindow = null;
                if (storeManagement.IsStoreEditorOpen)
                {
                    storeManagement.CloseStoreEditorCommand.Execute(null);
                }
            };
            _storeEditorWindow.Show(this);
            return;
        }

        if (!storeManagement.IsStoreEditorOpen && _storeEditorWindow is not null)
        {
            _storeEditorWindow.Close();
        }
    }

    private void SyncAssetsWindow(AssetsViewModel assets)
    {
        if (assets.IsOpen && _assetsWindow is null)
        {
            _assetsWindow = new AssetsWindow { DataContext = assets };
            assets.FilePicker = new AvaloniaAssetFilePicker(_assetsWindow.StorageProvider);
            _assetsWindow.Closed += (_, _) =>
            {
                _assetsWindow = null;
                if (assets.IsOpen)
                {
                    assets.CloseCommand.Execute(null);
                }

                WorkspaceTreeControl.Focus();
            };
            _assetsWindow.Show(this);
            return;
        }

        if (!assets.IsOpen && _assetsWindow is not null)
        {
            _assetsWindow.Close();
        }
    }

    private void SyncIdeationWindow(IdeationViewModel ideation)
    {
        if (ideation.IsOpen && _ideationWindow is null)
        {
            _ideationWindow = new IdeationWindow { DataContext = ideation };
            _ideationWindow.Closed += (_, _) =>
            {
                _ideationWindow = null;
                if (CanFocusOwner(this))
                {
                    Activate();
                    if (!IdeationButton.Focus())
                    {
                        WorkspaceTreeControl.Focus();
                    }
                }
            };
            _ = _ideationWindow.ShowDialog(this);
            return;
        }

        if (!ideation.IsOpen && _ideationWindow is not null)
        {
            _ideationWindow.Close();
        }
    }

    private void SyncDesignPreviewWindow(DesignStageToolViewModel designTool)
    {
        if (designTool.ShowPreviewDialog && _designPreviewWindow is null)
        {
            _designPreviewWindow = new DesignPreviewWindow { DataContext = designTool };
            _designPreviewWindow.Closed += (_, _) =>
            {
                _designPreviewWindow = null;
                designTool.ClosePreviewDialog();
            };
            _designPreviewWindow.Show(this);
            return;
        }

        if (!designTool.ShowPreviewDialog && _designPreviewWindow is not null)
        {
            _designPreviewWindow.Close();
        }
    }

    private void OnTreeNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: WorkspaceTreeNodeViewModel node } control ||
            DataContext is not MainWindowViewModel viewModel ||
            node.IsEditing)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
        {
            viewModel.WorkspaceTree.OpenInTabPreservingSelection(node);
            e.Handled = true;
        }
        else if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            viewModel.WorkspaceTree.PrepareContextSelection(node);
            e.Handled = true;
        }
        else if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            var controlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            viewModel.WorkspaceTree.SelectNodeWithModifiers(node, controlPressed && !shiftPressed, shiftPressed, controlPressed);
            e.Handled = true;
        }

        if (node.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item && point.Properties.IsLeftButtonPressed)
        {
            _dragPointerArgs = e;
            _dragNode = node;
            _dragStart = point.Position;
        }
    }

    private void OnWorkspaceTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TreeView ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            e.Source is not Visual source ||
            source is ToggleButton ||
            source.GetVisualAncestors().OfType<ToggleButton>().Any())
        {
            return;
        }

        var expander = ((TreeView)sender).GetVisualDescendants()
            .OfType<ToggleButton>()
            .Select(button => new { Button = button, Node = button.DataContext as WorkspaceTreeNodeViewModel })
            .FirstOrDefault(candidate => candidate.Node is { HasChildren: true } && IsWithinExpanderHitTarget(e, candidate.Button));
        if (expander?.Node is { } node)
        {
            node.IsExpanded = !node.IsExpanded;
            e.Handled = true;
        }
    }

    private static bool IsWithinExpanderHitTarget(PointerPressedEventArgs e, ToggleButton expander)
    {
        const double ExpanderHitTargetSize = 32;
        var position = e.GetPosition(expander);
        var horizontalPadding = Math.Max(0, (ExpanderHitTargetSize - expander.Bounds.Width) / 2);
        var verticalPadding = Math.Max(0, (ExpanderHitTargetSize - expander.Bounds.Height) / 2);
        return position.X >= -horizontalPadding && position.X <= expander.Bounds.Width + horizontalPadding &&
               position.Y >= -verticalPadding && position.Y <= expander.Bounds.Height + verticalPadding;
    }

    private async void OnTreeNodePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragPointerArgs is null || _dragNode is null || sender is not Control control)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (!point.Properties.IsLeftButtonPressed ||
            Math.Abs(point.Position.X - _dragStart.X) + Math.Abs(point.Position.Y - _dragStart.Y) < 6)
        {
            return;
        }

        var transfer = new DataTransfer();
        var selections = (DataContext as MainWindowViewModel)?.WorkspaceTree.GetDragSelections(_dragNode)
            ?? [new WorkspaceTreeSelection(_dragNode.EntityKind, _dragNode.EntityId)];
        transfer.Add(DataTransferItem.CreateText(
            $"FusionCanvasSelection|{string.Join(',', selections.Select(selection => $"{selection.Kind}:{selection.Id}"))}"));
        var pressedArgs = _dragPointerArgs;
        _dragPointerArgs = null;
        _dragNode = null;
        await DragDrop.DoDragDropAsync(pressedArgs, transfer, DragDropEffects.Move);
    }

    private void OnTreeNodeDragOver(object? sender, DragEventArgs e)
    {
        ClearDropTarget();
        if (DataContext is MainWindowViewModel viewModel &&
            TryGetDraggedSelections(e, out var sources) &&
            sender is Control { DataContext: WorkspaceTreeNodeViewModel { EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group } target } control)
        {
            var placement = sources.Any(source => source.Kind == WorkspaceEntityKind.Item)
                ? new GroupPlacement()
                : PlacementFor(target, control, e);
            if (viewModel.WorkspaceTree.CanDrop(sources, target, placement, out var error))
            {
                viewModel.WorkspaceTree.ShowDropFeedback(null);
                _dropTarget = target;
                target.IsDropBefore = placement.Kind == GroupPlacementKind.Before;
                target.IsDropAfter = placement.Kind == GroupPlacementKind.After;
                target.IsDropTarget = placement.Kind == GroupPlacementKind.Append;
                if (target.Children.Count > 0)
                {
                    target.IsExpanded = true;
                }

                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                viewModel.WorkspaceTree.ShowDropFeedback(error);
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnTreeNodeDragLeave(object? sender, DragEventArgs e) => ClearDropTarget();

    private async void OnTreeNodeDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            sender is not Control { DataContext: WorkspaceTreeNodeViewModel target } control ||
            !TryGetDraggedSelections(e, out var sources) ||
            sources.Any(source => source.Id == target.EntityId))
        {
            return;
        }

        var placement = sources.Any(source => source.Kind == WorkspaceEntityKind.Item)
            ? new GroupPlacement()
            : PlacementFor(target, control, e);

        if (sources.Count == 1)
        {
            await viewModel.WorkspaceTree.MoveAsync(sources[0].Kind, sources[0].Id, target, placement);
        }
        else
        {
            await viewModel.WorkspaceTree.MoveSelectionAsync(sources, target, placement);
        }
        ClearDropTarget();
        e.Handled = true;
    }

    private void ClearDropTarget()
    {
        if (_dropTarget is null)
        {
            return;
        }

        _dropTarget.IsDropTarget = false;
        _dropTarget.IsDropBefore = false;
        _dropTarget.IsDropAfter = false;
        _dropTarget = null;
    }

    private static bool TryGetDraggedEntity(DragEventArgs e, out WorkspaceEntityKind kind, out Guid entityId)
    {
        kind = default;
        entityId = default;
        var parts = e.DataTransfer.TryGetText()?.Split(':', 2);
        return parts is { Length: 2 } && Enum.TryParse(parts[0], out kind) && Guid.TryParse(parts[1], out entityId);
    }

    private static bool TryGetDraggedSelections(DragEventArgs e, out IReadOnlyList<WorkspaceTreeSelection> selections)
    {
        selections = [];
        var text = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!text.StartsWith("FusionCanvasSelection|", StringComparison.Ordinal))
        {
            return TryGetDraggedEntity(e, out var kind, out var id) &&
                   (selections = [new WorkspaceTreeSelection(kind, id)]) is not null;
        }

        var parsed = text["FusionCanvasSelection|".Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split(':', 2))
            .Where(parts => parts.Length == 2 && Enum.TryParse(parts[0], out WorkspaceEntityKind _) && Guid.TryParse(parts[1], out _))
            .Select(parts => new WorkspaceTreeSelection(Enum.Parse<WorkspaceEntityKind>(parts[0]), Guid.Parse(parts[1])))
            .ToArray();
        selections = parsed;
        return parsed.Length > 0;
    }

    private static GroupPlacement PlacementFor(
        WorkspaceTreeNodeViewModel target,
        Control control,
        DragEventArgs e)
    {
        if (target.EntityKind != FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group)
        {
            return new GroupPlacement();
        }

        var position = e.GetPosition(control).Y / Math.Max(control.Bounds.Height, 1);
        return position switch
        {
            < 0.25 => new GroupPlacement(GroupPlacementKind.Before, target.EntityId),
            > 0.75 => new GroupPlacement(GroupPlacementKind.After, target.EntityId),
            _ => new GroupPlacement()
        };
    }

    private void OnDetailsFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CommitActiveDetailsEdits();
        }
    }

    // === Design Stage Tool event handlers ===

    private async void OnColorToggle(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not ToggleButton { DataContext: DesignColorViewModel colorVM } toggle)
            return;
        await vm.DesignTool.ToggleColorAsync(colorVM.ColorValue, toggle.IsChecked ?? false);
    }

    private async void OnMakeSpecificForColor(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not Button { Tag: string colorValue } btn)
            return;
        await vm.DesignTool.MakeSpecificForColorAsync(colorValue);
    }

    private async void OnRemoveSpecificRow(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || sender is not Button { Tag: Guid rowId } btn)
            return;
        vm.DesignTool.RequestRemoveSpecificRow(rowId);
    }

    private void OnSlotDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Border { DataContext: DesignSlotViewModel { IsReadOnly: false } })
            return;
        // Accept file drops (validation happens on drop)
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnSlotDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slotVm = FindSlotViewModel(sender);
        if (slotVm is null)
            return;

        var files = e.DataTransfer?.TryGetFiles();
        if (files is null || files.Length == 0)
            return;

        var file = files[0];
        var path = file.TryGetLocalPath();
        if (path is null)
        {
            vm.DesignTool.ErrorMessage = "Could not read the dropped file path.";
            e.Handled = true;
            return;
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext != ".png")
        {
            vm.DesignTool.ErrorMessage = "Only PNG files can be assigned to slots. The dropped file was not imported.";
            e.Handled = true;
            return;
        }

        var rowVM = vm.DesignTool.Rows.FirstOrDefault(r => r.Slots.Contains(slotVm));
        if (rowVM is null)
            return;

        vm.DesignTool.ErrorMessage = null;
        await vm.DesignTool.AssignSlotImageAsync(rowVM.RowId, slotVm.DesignAreaId, path);
        e.Handled = true;
    }

    private static DesignSlotViewModel? FindSlotViewModel(object? sender)
    {
        if (sender is Border { DataContext: DesignSlotViewModel slot })
            return slot;
        // Walk visual tree
        for (var element = sender as Avalonia.Visual; element is not null; element = element.GetVisualParent())
        {
            if (element is Border { DataContext: DesignSlotViewModel s })
                return s;
        }
        return null;
    }

    private async void OnViewSlotImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSlotViewModel(sender);
        if (slot is null || slot.AssetId is null)
            return;
        var rowVM = vm.DesignTool.Rows.FirstOrDefault(r => r.Slots.Contains(slot));
        if (rowVM is null)
            return;

        await vm.DesignTool.PreviewSlotImageAsync(rowVM.RowId, slot.DesignAreaId);
    }

    private async void OnRemoveSlotImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSlotViewModel(sender);
        if (slot is null)
            return;
        var rowVM = vm.DesignTool.Rows.FirstOrDefault(r => r.Slots.Contains(slot));
        if (rowVM is null)
            return;

        vm.DesignTool.RequestRemoveSlotImage(rowVM.RowId, slot.DesignAreaId);
    }

    private async void OnExportSlotImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSlotViewModel(sender);
        if (slot is null || slot.AssetId is null)
            return;
        var rowVM = vm.DesignTool.Rows.FirstOrDefault(r => r.Slots.Contains(slot));
        if (rowVM is null)
            return;

        if (!StorageProvider.CanSave)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export slot image",
            DefaultExtension = ".png",
            FileTypeChoices = [new FilePickerFileType("PNG image") { Patterns = ["*.png"] }]
        });

        if (file?.TryGetLocalPath() is { } path)
        {
            await vm.DesignTool.ExportSlotImageAsync(rowVM.RowId, slot.DesignAreaId, path);
        }
    }

    private async void OnExportSupportingImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSupportingImageViewModel(sender);
        if (slot?.AssetId is null)
            return;

        if (!StorageProvider.CanSave)
            return;

        // Compute default extension from the actual managed file
        var ext = slot.ThumbnailPath is not null
            ? Path.GetExtension(slot.ThumbnailPath)?.TrimStart('.')
            : null;
        if (string.IsNullOrEmpty(ext)) ext = "png";

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export supporting image",
            DefaultExtension = $".{ext}",
            FileTypeChoices = [new FilePickerFileType("Image files") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.svg", "*.gif", "*.bmp"] }]
        });

        if (file?.TryGetLocalPath() is { } path)
        {
            await vm.DesignTool.ExportSupportingImageAsync(slot.AssetId.Value, path);
        }
    }

    private async void OnConfirmRemoval(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        await vm.DesignTool.ConfirmPendingRemovalAsync();
    }

    private void OnCancelRemoval(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        vm.DesignTool.CancelPendingRemoval();
    }

    private async void OnImportSupportingImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm
            || vm.ItemInspector.LoadedItemId is not Guid itemId
            || !StorageProvider.CanOpen)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import supporting image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image files") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.svg", "*.gif", "*.bmp"] },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count == 1 && files[0].TryGetLocalPath() is { } path)
        {
            await vm.DesignTool.ImportSupportingImageAsync(path);
        }
    }

    private async void OnViewSupportingImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSupportingImageViewModel(sender);
        if (slot?.AssetId is null)
            return;
        vm.DesignTool.PreviewSupportingImage(slot.AssetId.Value, slot.ThumbnailPath);
    }

    private async void OnRemoveSupportingImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;
        var slot = FindSupportingImageViewModel(sender);
        if (slot?.AssetId is null)
            return;
        vm.DesignTool.RequestRemoveSupportingImage(slot.AssetId.Value);
    }

    private static DesignSlotViewModel? FindSupportingImageViewModel(object? sender)
    {
        if (sender is Control { DataContext: DesignSlotViewModel slot })
            return slot;
        for (var element = sender as Avalonia.Visual; element is not null; element = element.GetVisualParent())
        {
            if (element is Control { DataContext: DesignSlotViewModel s })
                return s;
        }
        return null;
    }

    private void OnTreeEditorAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextBox textBox && textBox.IsVisible)
        {
            Dispatcher.UIThread.Post(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }

    private async void OnTreeEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            await viewModel.WorkspaceTree.CommitEditAsync(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            viewModel.WorkspaceTree.CancelEdit();
            e.Handled = true;
        }
    }

    private async void OnTreeEditorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.WorkspaceTree.HasEditingNode)
        {
            await viewModel.WorkspaceTree.CommitEditAsync();
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox)
        {
            return;
        }

        if (e.Key == Key.N && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            viewModel.WorkspaceTree.BeginCreateCommand.Execute(null);
        }
        else if (e.Key == Key.L && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            viewModel.WorkspaceTree.BeginCreateItemCommand.Execute(null);
        }
        else if (e.Key == Key.F2)
        {
            viewModel.WorkspaceTree.BeginRenameCommand.Execute(null);
            FocusVisibleTreeEditor();
        }
        else if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.SelectAllVisibleEntities();
        }
        else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.CopyCommand.Execute(null);
        }
        else if (e.Key == Key.X && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.CutCommand.Execute(null);
        }
        else if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.PasteCommand.Execute(null);
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    private async void OnContextNewGroup(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextGroup(sender, out var viewModel, out _))
        {
            await viewModel.WorkspaceTree.BeginCreateAsync();
            FocusVisibleTreeEditor();
        }
    }

    private void OnContextOpenInTab(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out var node))
        {
            viewModel.WorkspaceTree.OpenInTabCommand.Execute(node);
        }
    }

    private void OnContextOpenSelectedInTabs(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel node } &&
            DataContext is MainWindowViewModel viewModel &&
            node.HasMultiSelectionContext)
        {
            viewModel.WorkspaceTree.OpenSelectedInTabsCommand.Execute(null);
        }
    }

    private async void OnContextNewItem(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            await viewModel.WorkspaceTree.BeginCreateItemAsync();
            FocusVisibleTreeEditor();
        }
    }

    private void OnContextRename(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            viewModel.WorkspaceTree.BeginRename();
            FocusVisibleTreeEditor();
        }
    }

    private void OnContextCopy(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            viewModel.WorkspaceTree.Copy();
        }
    }

    private void OnContextCut(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            viewModel.WorkspaceTree.Cut();
        }
    }

    private async void OnContextPaste(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            await viewModel.WorkspaceTree.PasteAsync();
        }
    }

    private async void OnContextDuplicate(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out var node) && node.IsItem)
        {
            await viewModel.WorkspaceTree.DuplicateAsync();
        }
    }

    private async void OnContextDuplicateSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel node } &&
            DataContext is MainWindowViewModel viewModel &&
            node.HasMultiSelectionContext)
        {
            await viewModel.WorkspaceTree.DuplicateSelectedAsync();
        }
    }

    private async void OnContextExportSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel node } &&
            DataContext is MainWindowViewModel viewModel &&
            node.HasMultiSelectionContext)
        {
            await viewModel.WorkspaceTree.ExportSelectedAsync();
        }
    }

    private async void OnContextGroupSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: WorkspaceTreeNodeViewModel node } ||
            DataContext is not MainWindowViewModel viewModel ||
            !node.HasMultiSelectionContext)
        {
            return;
        }

        var destinations = viewModel.WorkspaceTree.GetGroupDestinationsForSelection();
        var dialog = new GroupSelectionWindow(
            destinations,
            viewModel.WorkspaceTree.GetDefaultGroupDestination(destinations));
        if (await dialog.ShowDialog<bool>(this) && dialog.DataContext is GroupSelectionViewModel selection && selection.SelectedDestination is { } destination)
        {
            await viewModel.WorkspaceTree.GroupSelectedAsync(selection.Name, destination);
        }
    }

    private async void OnContextArchiveSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: WorkspaceTreeNodeViewModel node } ||
            DataContext is not MainWindowViewModel viewModel ||
            !node.HasMultiSelectionContext)
        {
            return;
        }

        var dialog = new GroupActionConfirmationWindow(
            "Archive selected entities",
            $"Archive {node.SelectionCount} selected entities? They can be restored later.");
        if (await dialog.ShowDialog<bool>(this))
        {
            await viewModel.WorkspaceTree.ArchiveSelectedAsync();
        }
    }

    private async void OnContextDeleteSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: WorkspaceTreeNodeViewModel node } ||
            DataContext is not MainWindowViewModel viewModel ||
            !node.HasMultiSelectionContext)
        {
            return;
        }

        var dialog = new GroupActionConfirmationWindow(
            "Delete selected entities",
            $"Permanently delete {node.SelectionCount} selected entities and any contained descendants? This cannot be undone.");
        if (await dialog.ShowDialog<bool>(this))
        {
            await viewModel.WorkspaceTree.DeleteSelectedAsync();
        }
    }

    private void OnContextAssets(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel node } &&
            DataContext is MainWindowViewModel viewModel &&
            node.HasAssetActions)
        {
            viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
            viewModel.WorkspaceTree.ManageAssetsCommand.Execute(null);
        }
    }

    private async void OnContextExport(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out var node) &&
            node.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group)
        {
            await viewModel.WorkspaceTree.ExportCsvAsync(node);
        }
    }

    private async void OnContextStoreAssets(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.OpenManageStoreAssetsAsync();
        }
    }

    private async void OnContextDelete(object? sender, RoutedEventArgs e)
    {
        if (!TrySelectContextGroup(sender, out var viewModel, out var node))
        {
            return;
        }

        var dialog = new GroupDeleteConfirmationWindow(viewModel.WorkspaceTree.GetDeleteImpact(node.EntityId));
        if (await dialog.ShowDialog<bool>(this))
        {
            await viewModel.WorkspaceTree.DeleteGroupAsync(node.EntityId, ConfirmPermanentDeletion: true);
        }
    }

    private async void OnContextImport(object? sender, RoutedEventArgs e)
    {
        if (!TrySelectContextTopic(sender, out var viewModel, out var node))
        {
            return;
        }

        var topic = new ItemTopicReference(
            node.EntityKind == WorkspaceEntityKind.Group
                ? WorkspaceEntityKind.Group
                : WorkspaceEntityKind.Niche,
            node.EntityId);
        var import = new ItemImportViewModel(
            topic,
            node.Name,
            viewModel.ItemCsvImport,
            new FusionCanvas.Integration.Items.Import.ItemCsvCodec());
        var window = new ItemImportWindow { DataContext = import };
        await window.ShowDialog(this);
        if (import.HasImportCompleted)
        {
            await viewModel.RefreshWorkspaceAfterImportAsync();
        }
    }

    private bool TrySelectContextGroup(
        object? sender,
        out MainWindowViewModel viewModel,
        out WorkspaceTreeNodeViewModel node)
    {
        viewModel = DataContext as MainWindowViewModel ?? null!;
        node = sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel candidate } ? candidate : null!;
        if (viewModel is null || node is null || node.EntityKind != FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group)
        {
            return false;
        }

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
        return true;
    }

    private bool TrySelectContextNode(
        object? sender,
        out MainWindowViewModel viewModel,
        out WorkspaceTreeNodeViewModel node)
    {
        viewModel = DataContext as MainWindowViewModel ?? null!;
        node = sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel candidate } ? candidate : null!;
        if (viewModel is null || node is null || !node.HasContextActions)
        {
            return false;
        }

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
        return true;
    }

    private bool TrySelectContextTopic(
        object? sender,
        out MainWindowViewModel viewModel,
        out WorkspaceTreeNodeViewModel node)
    {
        viewModel = DataContext as MainWindowViewModel ?? null!;
        node = sender is MenuItem { DataContext: WorkspaceTreeNodeViewModel candidate } ? candidate : null!;
        if (viewModel is null || node is null || !node.IsTopic)
        {
            return false;
        }

        viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
        return true;
    }

    private void FocusVisibleTreeEditor()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var editor = WorkspaceTreeControl
                .GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(textBox => textBox.IsVisible);
            if (editor is not null)
            {
                editor.Focus();
                editor.SelectAll();
            }
        }, DispatcherPriority.Input);
    }
}
