using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests.TestSupport.Drivers;

internal sealed class MainWindowDriver(MainWindow window)
{
    internal IReadOnlyList<Control> GroupRows => window.GetVisualDescendants()
        .OfType<Control>()
        .Where(control => (AutomationProperties.GetAutomationId(control) ?? string.Empty).StartsWith("Workspace.Group", StringComparison.Ordinal))
        .ToArray();

}
