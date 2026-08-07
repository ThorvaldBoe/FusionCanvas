using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FusionCanvas.App.Groups;

public partial class GroupActionConfirmationWindow : Window
{
    public GroupActionConfirmationWindow()
    {
        InitializeComponent();
    }

    public GroupActionConfirmationWindow(string title, string message)
        : this()
    {
        DataContext = new GroupActionConfirmationViewModel(title, message);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);
}

public sealed record GroupActionConfirmationViewModel(string Title, string Message);
