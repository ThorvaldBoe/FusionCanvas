using Avalonia.Threading;
using Avalonia.Headless.XUnit;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests;

public sealed class MainWindowDispatcherTests
{
    [AvaloniaFact]
    public void QueuedCallbackIsSkippedWhenTheWindowClosesBeforeDispatcherRunsIt()
    {
        var window = new MainWindow();
        window.Show();

        try
        {
            var callbackRan = false;
            window.PostToDispatcher(() => callbackRan = true, DispatcherPriority.Background);

            window.Close();
            Dispatcher.UIThread.RunJobs();

            Assert.False(callbackRan);
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void DispatcherCallbackRunsBeforeTheWindowCloses()
    {
        var window = new MainWindow();
        window.Show();

        try
        {
            var callbackRan = false;
            window.PostToDispatcher(() => callbackRan = true, DispatcherPriority.Background);
            Dispatcher.UIThread.RunJobs();

            Assert.True(callbackRan);
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        }
    }
}
