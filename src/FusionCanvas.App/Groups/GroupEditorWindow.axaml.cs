using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace FusionCanvas.App.Groups;

public partial class GroupEditorWindow : Window
{
    private GroupManagementViewModel? _subscribedViewModel;

    public GroupEditorWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not GroupManagementViewModel viewModel || !viewModel.IsOpen)
        {
            return;
        }

        viewModel.TryClose();
        e.Cancel = viewModel.IsOpen;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.GroupNameFocusRequested -= OnGroupNameFocusRequested;
            _subscribedViewModel.GroupListFocusRequested -= OnGroupListFocusRequested;
        }

        _subscribedViewModel = DataContext as GroupManagementViewModel;
        if (_subscribedViewModel is null)
        {
            return;
        }

        _subscribedViewModel.GroupNameFocusRequested += OnGroupNameFocusRequested;
        _subscribedViewModel.GroupListFocusRequested += OnGroupListFocusRequested;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GroupManagementViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (viewModel.DiscardPromptVisible)
            {
                viewModel.KeepEditingCommand.Execute(null);
            }
            else if (viewModel.ArchiveWarningVisible)
            {
                viewModel.CancelArchiveCommand.Execute(null);
            }
            else
            {
                viewModel.CloseCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control) && viewModel.CanSave)
        {
            viewModel.SaveCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnGroupNameFocusRequested(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            GroupNameTextBox.Focus();
            GroupNameTextBox.SelectAll();
        });

    private void OnGroupListFocusRequested(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => ActiveGroupsList.Focus());
}
