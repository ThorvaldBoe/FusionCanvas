using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Workspace;
using FusionCanvas.Application.Workspace;

namespace FusionCanvas.App.Views;

public partial class MainWindow : Window
{
    private StoreEditorWindow? _storeEditorWindow;
    private WorkspaceManagementWindow? _workspaceManagementWindow;
    private GroupEditorWindow? _groupEditorWindow;
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
        viewModel.GroupManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GroupManagementViewModel.IsOpen))
            {
                SyncGroupEditorWindow(viewModel.GroupManagement);
            }
        };
        DataContext = viewModel;
        SyncWorkspaceManagementWindow(viewModel.WorkspaceManagement);
        SyncStoreEditorWindow(viewModel.StoreManagement);
        SyncGroupEditorWindow(viewModel.GroupManagement);
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

    private void SyncGroupEditorWindow(GroupManagementViewModel groupManagement)
    {
        if (groupManagement.IsOpen && _groupEditorWindow is null)
        {
            _groupEditorWindow = new GroupEditorWindow { DataContext = groupManagement };
            _groupEditorWindow.Closed += (_, _) =>
            {
                _groupEditorWindow = null;
                if (groupManagement.IsOpen)
                {
                    groupManagement.CloseCommand.Execute(null);
                }

                GroupActionsButton.Focus();
            };
            _groupEditorWindow.Show(this);
            return;
        }

        if (!groupManagement.IsOpen && _groupEditorWindow is not null)
        {
            _groupEditorWindow.Close();
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

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.WorkspaceTree.OpenInTabCommand.Execute(node);
        }
        else
        {
            viewModel.WorkspaceTree.SelectNodeCommand.Execute(node);
        }

        var point = e.GetCurrentPoint(control);
        if (node.EntityKind == FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group && point.Properties.IsLeftButtonPressed)
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
        transfer.Add(DataTransferItem.CreateText(_dragNode.EntityId.ToString()));
        var pressedArgs = _dragPointerArgs;
        _dragPointerArgs = null;
        _dragNode = null;
        await DragDrop.DoDragDropAsync(pressedArgs, transfer, DragDropEffects.Move);
    }

    private void OnTreeNodeDragOver(object? sender, DragEventArgs e)
    {
        ClearDropTarget();
        if (TryGetDraggedGroupId(e, out _) &&
            sender is Control { DataContext: WorkspaceTreeNodeViewModel { EntityKind: FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Niche or FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group } target } control)
        {
            _dropTarget = target;
            var position = e.GetPosition(control).Y / Math.Max(control.Bounds.Height, 1);
            target.IsDropBefore = target.EntityKind == FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group && position < 0.25;
            target.IsDropAfter = target.EntityKind == FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group && position > 0.75;
            target.IsDropTarget = !target.IsDropBefore && !target.IsDropAfter;
            if (target.Children.Count > 0)
            {
                target.IsExpanded = true;
            }

            e.DragEffects = DragDropEffects.Move;
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
            !TryGetDraggedGroupId(e, out var sourceGroupId) ||
            sourceGroupId == target.EntityId)
        {
            return;
        }

        var placement = new GroupPlacement();
        if (target.EntityKind == FusionCanvas.Domain.Workspace.WorkspaceEntityKind.Group)
        {
            var position = e.GetPosition(control).Y / Math.Max(control.Bounds.Height, 1);
            placement = position switch
            {
                < 0.25 => new GroupPlacement(GroupPlacementKind.Before, target.EntityId),
                > 0.75 => new GroupPlacement(GroupPlacementKind.After, target.EntityId),
                _ => new GroupPlacement()
            };
        }

        await viewModel.WorkspaceTree.MoveAsync(sourceGroupId, target, placement);
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

    private static bool TryGetDraggedGroupId(DragEventArgs e, out Guid groupId) =>
        Guid.TryParse(e.DataTransfer.TryGetText(), out groupId);

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
        else if (e.Key == Key.F2)
        {
            viewModel.WorkspaceTree.BeginRenameCommand.Execute(null);
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
}
