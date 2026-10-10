using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

namespace FusionCanvas.UITests;

public sealed class PrintifyListingImportUiSmokeTests
{
    [Trait("Suite", "UiSmoke")]
    [Fact]
    public async Task Niche_import_downloads_linked_product_variant_setup_in_disposable_workspace()
    {
        using var session = UiApplicationSession.Start(usePrintifyImportFixture: true);
        var niche = WaitForElement(session.Driver, By.Name("UI Test Niche"));
        new Actions(session.Driver).ContextClick(niche).Perform();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.ImportFromNiche")).Click();
        WaitForElement(session.Driver, By.Name("Mocked Printify art"));
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.ProductSelection")).Click();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.ConfirmImport")).Click();

        var repository = new SqliteWorkspaceRepository(session.TestRoot.DatabasePath, useConnectionPooling: false);
        var imported = await WaitForSnapshotAsync(repository, snapshot => snapshot.ExternalListingMappings.Any(mapping => mapping.ProductId == "ui-test-product"));
        var itemId = imported.ExternalListingMappings.Single(mapping => mapping.ProductId == "ui-test-product").ItemId;
        Assert.Contains(imported.AssetLinks, link => link.EntityKind == WorkspaceEntityKind.Item && link.EntityId == itemId);

        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.CloseImport")).Click();
        WaitForElement(session.Driver, By.Name("Mocked Printify art")).Click();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.DownloadVariantSetup")).Click();
        var configured = await WaitForSnapshotAsync(repository, snapshot => snapshot.ItemListingConfigurations.Any(value => value.ItemId == itemId));

        Assert.Single(configured.ItemListingConfigurations.Where(value => value.ItemId == itemId));
        Assert.Single(configured.DesignVariantRows.Where(value => value.ItemId == itemId));
        Assert.Contains(configured.DesignSlotAssignments, assignment => assignment.AssetId is not null);
        Assert.Equal("Mocked Printify art", configured.Items.Single(value => value.Id == itemId).Name);
        Assert.True(session.TestRoot.Contains(session.TestRoot.DatabasePath));
    }

    private static IWebElement WaitForElement(AppiumDriver driver, By by)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            try { return driver.FindElement(by); }
            catch (WebDriverException) { Thread.Sleep(100); }
        }
        throw new TimeoutException($"Timed out waiting for UI element {by}.");
    }

    private static async Task<WorkspaceSnapshot> WaitForSnapshotAsync(SqliteWorkspaceRepository repository, Func<WorkspaceSnapshot, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        WorkspaceSnapshot snapshot = WorkspaceSnapshot.Empty;
        while (DateTime.UtcNow < deadline)
        {
            snapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);
            if (condition(snapshot)) return snapshot;
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        throw new TimeoutException("The Printify listing operation did not persist the expected workspace state.");
    }
}
