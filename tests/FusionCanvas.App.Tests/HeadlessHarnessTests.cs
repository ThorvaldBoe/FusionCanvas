using Avalonia.Headless.XUnit;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests;

public class HeadlessHarnessTests
{
    [AvaloniaFact]
    public void MainWindow_CanInstantiate()
    {
        var window = new MainWindow();

        Assert.NotNull(window);
        Assert.IsType<MainWindowViewModel>(window.DataContext);
    }
}
