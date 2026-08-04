using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests.Pages;

internal abstract class UiPage(WindowsDriver driver)
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    protected WindowsDriver Driver { get; } = driver;

    protected IWebElement FindByAutomationId(string automationId) => WaitUntil(
        () => Driver.FindElement(MobileBy.AccessibilityId(automationId)),
        $"control with automation ID '{automationId}'");

    protected void SwitchToWindowContainingAutomationId(string automationId)
    {
        WaitUntil(() =>
        {
            foreach (var handle in Driver.WindowHandles)
            {
                Driver.SwitchTo().Window(handle);
                try
                {
                    if (Driver.FindElement(MobileBy.AccessibilityId(automationId)) is not null)
                    {
                        return Driver;
                    }
                }
                catch (WebDriverException)
                {
                    // The editor window may not have completed its UI Automation tree yet.
                }
            }

            return null;
        }, $"window containing control with automation ID '{automationId}'");
    }

    protected void WaitFor(Func<bool> condition, string expectation)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < DefaultTimeout)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException($"Timed out waiting for {expectation}.");
    }

    private static T WaitUntil<T>(Func<T?> action, string expectation) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;
        while (stopwatch.Elapsed < DefaultTimeout)
        {
            try
            {
                var result = action();
                if (result is not null)
                {
                    return result;
                }
            }
            catch (WebDriverException exception)
            {
                lastException = exception;
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException($"Timed out waiting for {expectation}.", lastException);
    }
}
