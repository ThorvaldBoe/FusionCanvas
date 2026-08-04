using OpenQA.Selenium;
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
        nameInput.SendKeys(Keys.Tab);

        WaitFor(
            () => FindByAutomationId(AutomationIds.StoreEditorSaveStore).Enabled,
            "the primary store creation action to become enabled after keyboard entry");

        var saveButton = FindByAutomationId(AutomationIds.StoreEditorSaveStore);
        saveButton.Click();

        WaitFor(
            () => Driver.FindElements(By.XPath($"//Button[@Name={ToXPathLiteral(name)}]")).Count > 0,
            $"the created store '{name}' to appear in the active-store list");
    }

    private static string ToXPathLiteral(string value) => $"'{value}'";
}
