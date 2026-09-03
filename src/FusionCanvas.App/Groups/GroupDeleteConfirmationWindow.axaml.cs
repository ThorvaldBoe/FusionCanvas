using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.App.Navigation;

namespace FusionCanvas.App.Groups;

public partial class GroupDeleteConfirmationWindow : Window
{
    public GroupDeleteConfirmationWindow()
    {
        InitializeComponent();
    }

    public GroupDeleteConfirmationWindow(GroupDeleteImpact impact)
        : this()
    {
        ArgumentNullException.ThrowIfNull(impact);
        DataContext = new GroupDeleteConfirmationViewModel(impact);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);
}
