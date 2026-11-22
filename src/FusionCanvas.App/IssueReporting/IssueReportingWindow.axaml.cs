using Avalonia.Controls;

namespace FusionCanvas.App.IssueReporting;

public partial class IssueReportingWindow : Window
{
    public IssueReportingWindow() => InitializeComponent();

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
