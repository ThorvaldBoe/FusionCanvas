using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Views;

public partial class MainWindow : Window
{
    private StoreEditorWindow? _storeEditorWindow;
    private WorkspaceManagementWindow? _workspaceManagementWindow;
    private AssetsWindow? _assetsWindow;
    private Window? _designPreviewWindow;
    private PointerPressedEventArgs? _dragPointerArgs;
    private WorkspaceTreeNodeViewModel? _dragNode;
    private Avalonia.Point _dragStart;
    private WorkspaceTreeNodeViewModel? _dropTarget;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = MainWindowViewModel.CreateForDefaultWorkspace();
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
        DataContext = viewModel;
        SyncWorkspaceManagementWindow(viewModel.WorkspaceManagement);
        SyncStoreEditorWindow(viewModel.StoreManagement);
        SyncAssetsWindow(viewModel.AssetsManagement);
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
            };
            _workspaceManagementWindow.Show(this);
            return;
        }

        if (!workspaceManagement.IsWorkspaceManagementOpen && _workspaceManagementWindow is not null)
        {
            _workspaceManagementWindow.Close();
        }
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

    private void OnTreeNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: WorkspaceTreeNodeViewModel node } control ||
            DataContext is not MainWindowViewModel viewModel ||
            node.IsEditing)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.OpenInTabCommand.Execute(node);
        }
        else
        {
            viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
        }

        if (node.EntityKind is WorkspaceEntityKind.Group or WorkspaceEntityKind.Item && point.Properties.IsLeftButtonPressed)
        {
            _dragPointerArgs = e;
            _dragNode = node;
            _dragStart = point.Position;
        }
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
        transfer.Add(DataTransferItem.CreateText($"{_dragNode.EntityKind}:{_dragNode.EntityId}"));
        var pressedArgs = _dragPointerArgs;
        _dragPointerArgs = null;
        _dragNode = null;
        await DragDrop.DoDragDropAsync(pressedArgs, transfer, DragDropEffects.Move);
    }

    private void OnTreeNodeDragOver(object? sender, DragEventArgs e)
    {
        ClearDropTarget();
        if (DataContext is MainWindowViewModel viewModel &&
            TryGetDraggedEntity(e, out var sourceKind, out var sourceId) &&
            sender is Control { DataContext: WorkspaceTreeNodeViewModel { EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group } target } control)
        {
            var placement = sourceKind == WorkspaceEntityKind.Item ? new GroupPlacement() : PlacementFor(target, control, e);
            if (viewModel.WorkspaceTree.CanDrop(sourceKind, sourceId, target, placement, out var error))
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
            !TryGetDraggedEntity(e, out var sourceKind, out var sourceId) ||
            sourceId == target.EntityId)
        {
            return;
        }

        var placement = sourceKind == WorkspaceEntityKind.Item ? new GroupPlacement() : PlacementFor(target, control, e);

        await viewModel.WorkspaceTree.MoveAsync(sourceKind, sourceId, target, placement);
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

    private async void OnImportDesignFiles(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel
            || viewModel.ItemInspector.LoadedItemId is not Guid itemId
            || !StorageProvider.CanOpen)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import PNG Design files",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("PNG images") { Patterns = ["*.png"] },
                FilePickerFileTypes.All
            ]
        });

        foreach (var file in files)
        {
            if (file.TryGetLocalPath() is { } path)
            {
                await viewModel.DesignTool.ImportAsync(itemId, path);
            }
        }

        if (sender is Button importButton)
        {
            importButton.Focus();
        }
    }

    private async void OnPreviewDesignFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        await using var stream = await viewModel.DesignTool.PreviewAsync();
        if (stream is null)
        {
            return;
        }

        var bitmap = new Bitmap(stream);
        _designPreviewWindow?.Close();
        _designPreviewWindow = new Window
        {
            Title = viewModel.DesignTool.SelectedFile?.Name ?? "Design preview",
            Width = Math.Clamp(bitmap.PixelSize.Width + 32, 320, 1000),
            Height = Math.Clamp(bitmap.PixelSize.Height + 48, 240, 800),
            Content = new Image
            {
                Source = bitmap,
                Stretch = Avalonia.Media.Stretch.Uniform,
                Margin = new Thickness(12)
            }
        };
        _designPreviewWindow.Closed += (_, _) =>
        {
            bitmap.Dispose();
            _designPreviewWindow = null;
        };
        _designPreviewWindow.Show(this);
    }

    private async void OnExportDesignFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !StorageProvider.CanSave)
        {
            return;
        }

        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Design file copy",
            SuggestedFileName = viewModel.DesignTool.SelectedFile?.Name ?? "design.png",
            FileTypeChoices = [new FilePickerFileType("PNG image") { Patterns = ["*.png"] }]
        });

        if (destination?.TryGetLocalPath() is { } path)
        {
            await viewModel.DesignTool.ExportAsync(path);
        }
    }

    private void OnRequestRemoveDesignFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { DesignTool.SelectedFile: not null } viewModel)
        {
            viewModel.IsDesignRemoveConfirmationVisible = true;
        }
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
        }
    }

    private async void OnContextNewItem(object? sender, RoutedEventArgs e)
    {
        if (TrySelectContextNode(sender, out var viewModel, out _))
        {
            await viewModel.WorkspaceTree.BeginCreateItemAsync();
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
