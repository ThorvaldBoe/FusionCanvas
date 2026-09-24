using System.Runtime.InteropServices;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.UITests.Infrastructure;
using FusionCanvas.UITests.Pages;
using Microsoft.Data.Sqlite;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests;

public sealed class TelemetryDebugWindowUiTests
{
    [Trait("ScenarioPack", "debug-window")]
    [Trait("Suite", "UiSmoke")]
    [Fact]
    public async Task DebugWindow_CapturesResizesCopiesAndClearsWithoutDeletingDatabaseRecords()
    {
        using var session = UiApplicationSession.Start();
        var snapshot = await new SqliteWorkspaceRepository(session.TestRoot.DatabasePath, useConnectionPooling: false)
            .LoadAsync(TestContext.Current.CancellationToken);
        var workspace = Assert.Single(snapshot.Workspaces);

        new MainWindowPage(session.Driver).OpenWorkspaceDiagnostics();
        var output = session.Driver.FindElement(MobileBy.AccessibilityId(AutomationIds.TelemetryDebugOutput));
        WaitFor(() => ReadText(output).Contains("DebugWindowOpened", StringComparison.Ordinal));

        var originalSize = session.Driver.Manage().Window.Size;
        session.Driver.Manage().Window.Size = new System.Drawing.Size(originalSize.Width + 120, originalSize.Height + 80);
        Assert.NotEqual(originalSize, session.Driver.Manage().Window.Size);

        session.Driver.FindElement(MobileBy.AccessibilityId(AutomationIds.TelemetryCopy)).Click();
        WaitFor(() => ReadWindowsClipboard().Contains("DebugWindowOpened", StringComparison.Ordinal));

        session.Driver.FindElement(MobileBy.AccessibilityId(AutomationIds.TelemetryClear)).Click();
        WaitFor(() => string.IsNullOrWhiteSpace(ReadText(output)));
        var telemetry = new SqliteTelemetryStore(session.TestRoot.DatabasePath);
        Assert.Contains(await telemetry.ReadAllAsync(workspace.Id, TestContext.Current.CancellationToken),
            entry => entry.Name == "DebugWindowOpened");
        SqliteConnection.ClearAllPools();
    }

    private static string ReadText(OpenQA.Selenium.IWebElement element) =>
        element.GetAttribute("Value.Value") ?? element.Text;

    private static void WaitFor(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout) throw new TimeoutException("The debug window did not reach the expected state.");
            Thread.Sleep(100);
        }
    }

    private static string ReadWindowsClipboard()
    {
        if (!OpenClipboard(IntPtr.Zero)) throw new InvalidOperationException("Could not open the Windows clipboard.");
        try
        {
            var handle = GetClipboardData(13); // CF_UNICODETEXT
            if (handle == IntPtr.Zero) return string.Empty;
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) return string.Empty;
            try { return Marshal.PtrToStringUni(pointer) ?? string.Empty; }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr handle);
}
