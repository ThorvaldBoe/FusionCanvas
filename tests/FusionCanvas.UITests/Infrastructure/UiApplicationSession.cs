using System.Net.Sockets;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Integration.Persistence;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests.Infrastructure;

internal sealed class UiApplicationSession : IDisposable
{
    private readonly DisposableUiTestRoot _testRoot;
    private bool _disposed;

    private UiApplicationSession(WindowsDriver driver, DisposableUiTestRoot testRoot)
    {
        Driver = driver;
        _testRoot = testRoot;
    }

    public WindowsDriver Driver { get; }

    public DisposableUiTestRoot TestRoot => _testRoot;

    public static UiApplicationSession Start()
    {
        var configuration = UiTestConfiguration.Load();
        configuration.ValidateForDesktopRun();
        EnsureAutomationServerAvailable(configuration.AutomationServerUri);

        var testRoot = new DisposableUiTestRoot();
        try
        {
            SeedWorkspace(testRoot);

            var options = new AppiumOptions();
            options.App = configuration.ApplicationPath;
            options.AddAdditionalAppiumOption("appArguments", testRoot.CreateApplicationArguments());
            options.AddAdditionalAppiumOption("appWorkingDir", Path.GetDirectoryName(configuration.ApplicationPath)!);
            options.AddAdditionalAppiumOption("createSessionTimeout", 15_000);
            options.PlatformName = "Windows";
            options.DeviceName = "WindowsPC";
            options.AutomationName = "Windows";

            var driver = new WindowsDriver(configuration.AutomationServerUri, options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            return new UiApplicationSession(driver, testRoot);
        }
        catch (Exception sessionException)
        {
            try
            {
                testRoot.Dispose();
            }
            catch (Exception cleanupException)
            {
                throw new InvalidOperationException(
                    $"The Appium session could not start and cleanup retained disposable root '{testRoot.RootPath}'.",
                    new AggregateException(sessionException, cleanupException));
            }

            throw;
        }
    }

    private static void SeedWorkspace(DisposableUiTestRoot testRoot)
    {
        var repository = new SqliteWorkspaceRepository(testRoot.DatabasePath, useConnectionPooling: false);
        var workspaces = new WorkspaceManagementService(repository);
        var result = workspaces.CreateWorkspaceAsync(new WorkspaceManagementCreateRequest("UI Smoke Workspace"))
            .GetAwaiter()
            .GetResult();

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not seed the disposable UI-test workspace: {result.Error}");
        }
    }

    public static void EnsureAutomationServerAvailable(Uri serverUri)
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(serverUri.Host, serverUri.Port);
            if (!connect.Wait(TimeSpan.FromSeconds(3)))
            {
                throw new TimeoutException();
            }
        }
        catch (Exception exception) when (exception is SocketException or TimeoutException or AggregateException)
        {
            throw new InvalidOperationException(
                $"Appium Windows automation server is unavailable at {serverUri}. Start 'appium --port 4723' as Administrator " +
                $"(or configure {UiTestConfiguration.AutomationServerUrlEnvironmentVariable}) before running this suite. " +
                "See tests/FusionCanvas.UITests/README.md.", exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            Driver.Quit();
        }
        finally
        {
            _testRoot.Dispose();
        }
    }
}
