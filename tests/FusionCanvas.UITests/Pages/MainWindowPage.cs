using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests.Pages;

internal sealed class MainWindowPage(WindowsDriver driver) : UiPage(driver)
{
    public void OpenStoreManagement()
    {
        FindByAutomationId(AutomationIds.StoreManagementOpenEditor).Click();
        SwitchToWindowContainingAutomationId(AutomationIds.StoreEditorNewStore);
    }
}
