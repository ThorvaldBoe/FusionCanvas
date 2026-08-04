using System.Net.Sockets;
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
            var options = new AppiumOptions();
            options.AddAdditionalAppiumOption("app", configuration.ApplicationPath);
            options.AddAdditionalAppiumOption("appArguments", testRoot.CreateApplicationArguments());
            options.AddAdditionalAppiumOption("platformName", "Windows");
            options.AddAdditionalAppiumOption("deviceName", "WindowsPC");

            var driver = new WindowsDriver(configuration.AutomationServerUri, options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            return new UiApplicationSession(driver, testRoot);
        }
        catch
        {
            testRoot.Dispose();
            throw;
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
                $"Windows UI automation server is unavailable at {serverUri}. Start WinAppDriver (or configure {UiTestConfiguration.AutomationServerUrlEnvironmentVariable}) before running this suite. " +
                "See tests/FusionCanvas.UITests/README.md.",
                exception);
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
