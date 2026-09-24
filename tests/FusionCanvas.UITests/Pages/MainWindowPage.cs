using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests.Pages;

internal sealed class MainWindowPage(WindowsDriver driver) : UiPage(driver)
{
    public void OpenStoreManagement()
    {
        FindByAutomationId(AutomationIds.StoreManagementOpenEditor).Click();
        SwitchToWindowContainingAutomationId(AutomationIds.StoreEditorNewStore);
    }

    public void OpenWorkspaceDiagnostics()
    {
        FindByAutomationId(AutomationIds.SettingsOpen).Click();
        SwitchToWindowContainingAutomationId(AutomationIds.SettingsSectionSelector);
        FindByAutomationId(AutomationIds.SettingsSectionSelector)
            .FindElement(OpenQA.Selenium.By.Name("Workspace")).Click();
        FindByAutomationId(AutomationIds.TelemetryShowDebugWindow).Click();
        SwitchToWindowContainingAutomationId(AutomationIds.TelemetryDebugOutput);
    }
}
