using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.Application.Groups;

namespace FusionCanvas.App.Groups;

public partial class GroupSelectionWindow : Window
{
    public GroupSelectionWindow()
    {
        InitializeComponent();
    }

    public GroupSelectionWindow(IReadOnlyList<GroupDestination> destinations, GroupDestination? defaultDestination)
        : this()
    {
        DataContext = new GroupSelectionViewModel(destinations, defaultDestination);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GroupSelectionViewModel viewModel && viewModel.CanConfirm)
        {
            Close(true);
        }
        else if (DataContext is GroupSelectionViewModel invalid)
        {
            invalid.ErrorMessage = "Enter a name and choose a destination.";
        }
    }
}
