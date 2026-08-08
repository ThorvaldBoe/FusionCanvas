using Avalonia;
using Avalonia.Controls;
using FusionCanvas.Application.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests;

public sealed class MainWindowLayoutNormalizerTests
{
    [Fact]
    public void TryNormalize_ClampsLargeWindowAndOffScreenPosition()
    {
        var saved = new WindowLayoutSettings(-5000, -4000, 3000, 2000, 360);
        var screens = new[]
        {
            new ScreenLayoutInfo(new PixelRect(0, 0, 1920, 1080), 1, true)
        };

        var normalized = AssertNormalize(saved, screens);

        Assert.Equal(1920, normalized.Width);
        Assert.Equal(1080, normalized.Height);
        Assert.Equal(360, normalized.NavigationWidth);
        Assert.True(normalized.PositionX >= -1920 + 48);
        Assert.True(normalized.PositionY >= -1080 + 48);
    }

    [Fact]
    public void TryNormalize_UsesScreenScalingWhenClampingLogicalSize()
    {
        var saved = new WindowLayoutSettings(2000, 100, 1400, 900, 400);
        var screens = new[]
        {
            new ScreenLayoutInfo(new PixelRect(0, 0, 1920, 1080), 1, true),
            new ScreenLayoutInfo(new PixelRect(1920, 0, 2560, 1440), 2, false)
        };

        var normalized = AssertNormalize(saved, screens);

        Assert.Equal(1280, normalized.Width);
        Assert.Equal(720, normalized.Height);
        Assert.Equal(400, normalized.NavigationWidth);
    }

    [Fact]
    public void TryNormalize_RejectsInvalidOrUnsupportedValues()
    {
        var screens = new[]
        {
            new ScreenLayoutInfo(new PixelRect(0, 0, 1920, 1080), 1, true)
        };

        Assert.False(MainWindowLayoutNormalizer.TryNormalize(
            new WindowLayoutSettings(0, 0, double.NaN, 700, 320), screens, 900, 600, out _));
        Assert.False(MainWindowLayoutNormalizer.TryNormalize(
            new WindowLayoutSettings(0, 0, 1000, 700, 600), screens, 900, 600, out _));
        Assert.False(MainWindowLayoutNormalizer.TryNormalize(
            new WindowLayoutSettings(0, 0, 800, 700, 320), screens, 900, 600, out _));
    }

    [Fact]
    public void TryCapture_IgnoresPlatformManagedWindowStates()
    {
        Assert.False(MainWindowLayoutNormalizer.TryCapture(
            WindowState.Maximized,
            new PixelPoint(10, 20),
            1200,
            800,
            320,
            900,
            600,
            out _));
        Assert.False(MainWindowLayoutNormalizer.TryCapture(
            WindowState.FullScreen,
            new PixelPoint(10, 20),
            1200,
            800,
            320,
            900,
            600,
            out _));
        Assert.True(MainWindowLayoutNormalizer.TryCapture(
            WindowState.Normal,
            new PixelPoint(10, 20),
            1200,
            800,
            320,
            900,
            600,
            out var layout));
        Assert.Equal(new WindowLayoutSettings(10, 20, 1200, 800, 320), layout);
    }

    private static WindowLayoutSettings AssertNormalize(
        WindowLayoutSettings saved,
        IReadOnlyList<ScreenLayoutInfo> screens)
    {
        Assert.True(MainWindowLayoutNormalizer.TryNormalize(saved, screens, 900, 600, out var normalized));
        return normalized;
    }
}
