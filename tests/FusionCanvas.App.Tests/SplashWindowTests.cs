using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;

namespace FusionCanvas.App.Tests;

public sealed class SplashWindowTests
{
    [AvaloniaFact]
    public void SplashWindow_UsesPackagedBannerAndNonInteractiveChrome()
    {
        var window = new SplashWindow();

        try
        {
            Assert.False(window.ShowInTaskbar);
            Assert.False(window.CanResize);
            Assert.Equal(WindowDecorations.None, window.WindowDecorations);
            Assert.NotNull(window.Icon);

            var border = Assert.IsType<Border>(window.Content);
            var image = Assert.IsType<Image>(border.Child);
            Assert.IsType<Bitmap>(image.Source);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StartupSuccess_ClosesSplash()
    {
        var window = new SplashWindow();
        window.Show();

        try
        {
            FusionCanvas.App.App.RunWithSplashCleanup(window, static () => { });

            Assert.False(window.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void StartupFailure_ClosesSplashAndPreservesException()
    {
        var window = new SplashWindow();
        window.Show();

        try
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                FusionCanvas.App.App.RunWithSplashCleanup(
                    window,
                    () => throw new InvalidOperationException("startup failed")));

            Assert.Equal("startup failed", error.Message);
            Assert.False(window.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }
}
