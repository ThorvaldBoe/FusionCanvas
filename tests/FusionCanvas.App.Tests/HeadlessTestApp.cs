using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(FusionCanvas.App.Tests.HeadlessTestApp))]

namespace FusionCanvas.App.Tests;

internal static class HeadlessTestApp
{
    private static readonly string TestRoot = Path.Combine(
        Path.GetTempPath(),
        "FusionCanvas.App.Tests",
        Guid.NewGuid().ToString("N"));

    public static AppBuilder BuildAvaloniaApp()
    {
        Directory.CreateDirectory(TestRoot);
        Environment.SetEnvironmentVariable(
            "FUSIONCANVAS_WORKSPACE_DB",
            Path.Combine(TestRoot, "workspace.db"));
        Environment.SetEnvironmentVariable(
            "FUSIONCANVAS_WORKSPACE_ROOT",
            Path.Combine(TestRoot, "workspace-files"));
        Environment.SetEnvironmentVariable(
            "FUSIONCANVAS_SETTINGS_PATH",
            Path.Combine(TestRoot, "settings.json"));
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(TestRoot, recursive: true); }
            catch { }
        };
        return Program.BuildAvaloniaApp().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
