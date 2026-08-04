using OpenQA.Selenium.Appium.Windows;

namespace FusionCanvas.UITests.Pages;

internal sealed class StoreEditorPage(WindowsDriver driver) : UiPage(driver)
{
    public void CreateStore(string name)
    {
        FindByAutomationId(AutomationIds.StoreEditorNewStore).Click();

        var nameInput = FindByAutomationId(AutomationIds.StoreEditorName);
        nameInput.Clear();
        nameInput.SendKeys(name);

        var saveButton = FindByAutomationId(AutomationIds.StoreEditorSaveStore);
        Assert.True(saveButton.Enabled, "The primary store creation action should be enabled after keyboard entry.");
        saveButton.Click();

        WaitFor(
            () => FindByAutomationId(AutomationIds.StoreEditorActiveStores).Text.Contains(name, StringComparison.Ordinal),
            $"the created store '{name}' to appear in the active-store list");
    }
}
