using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(FusionCanvas.App.Tests.HeadlessTestApp))]

namespace FusionCanvas.App.Tests;

internal static class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() => Program.BuildAvaloniaApp().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
