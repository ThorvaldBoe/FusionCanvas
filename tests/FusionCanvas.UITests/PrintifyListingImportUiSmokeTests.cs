using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;
using FusionCanvas.UITests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

namespace FusionCanvas.UITests;

public sealed class PrintifyListingImportUiSmokeTests
{
    [Trait("ScenarioPack", "listing-color-flexibility")]
    [Fact]
    public async Task Listing_more_options_previews_and_adds_multiple_provider_colors_in_disposable_workspace()
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
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.CloseImport")).Click();
        WaitForElement(session.Driver, By.Name("Mocked Printify art")).Click();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Printify.DownloadVariantSetup")).Click();
        var configured = await WaitForSnapshotAsync(repository, snapshot => snapshot.ItemListingConfigurations.Any(value => value.ItemId == itemId));
        var itemConfiguration = Assert.Single(configured.ItemListingConfigurations, value => value.ItemId == itemId);
        var colorOption = Assert.Single(configured.OfferingOptions, option => option.OfferingId == itemConfiguration.OfferingId
            && option.OptionKind == FusionCanvas.Domain.Catalog.OptionKind.Color);
        var colorIds = configured.OfferingOptionValues.Where(value => value.OptionId == colorOption.Id)
            .ToDictionary(value => value.Value, value => value.Id);
        configured = configured with
        {
            OfferingOptionValues = [.. configured.OfferingOptionValues.Select(value => value.Value is "Black" or "White" ? value with { IsFavorite = true } : value)]
        };
        await repository.SaveAsync(configured, TestContext.Current.CancellationToken);

        WaitForElement(session.Driver, By.Name("Listing")).Click();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Listing.MoreOptions")).Click();
        SwitchToWindowContainingAutomationId(session.Driver, "Listing.ApplyMoreOptions");
        WaitForElement(session.Driver, By.Name("Preview Teal color")).Click();
        WaitForElement(session.Driver, By.Name("Viewing Teal"));
        var beforeSelection = await repository.LoadAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(beforeSelection.ListingColorSelections, value => value.ItemId == itemId && value.ColorOptionValueId == colorIds["Teal"]);

        WaitForElement(session.Driver, By.Name("Add Teal color to this listing")).Click();
        WaitForElement(session.Driver, By.Name("Add Heather Gray color to this listing")).Click();
        WaitForElement(session.Driver, MobileBy.AccessibilityId("Listing.ApplyMoreOptions")).Click();

        var persisted = await WaitForSnapshotAsync(new SqliteWorkspaceRepository(session.TestRoot.DatabasePath, useConnectionPooling: false),
            snapshot => snapshot.ListingColorSelections.Count(value => value.ItemId == itemId) == 3);
        Assert.Contains(persisted.ListingColorSelections, value => value.ItemId == itemId && value.ColorOptionValueId == colorIds["Teal"]);
        Assert.Contains(persisted.ListingColorSelections, value => value.ItemId == itemId && value.ColorOptionValueId == colorIds["Heather Gray"]);
        Assert.True(session.TestRoot.Contains(session.TestRoot.DatabasePath));
    }

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

    private static void SwitchToWindowContainingAutomationId(AppiumDriver driver, string automationId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var handle in driver.WindowHandles)
            {
                driver.SwitchTo().Window(handle);
                try
                {
                    if (driver.FindElement(MobileBy.AccessibilityId(automationId)) is not null) return;
                }
                catch (WebDriverException) { }
            }
            Thread.Sleep(100);
        }
        throw new TimeoutException($"Timed out waiting for a window containing automation ID '{automationId}'.");
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
