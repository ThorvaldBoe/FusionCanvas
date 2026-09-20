using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.DesignFiles;
using System.Text.Json;

namespace FusionCanvas.Application.Tests.DesignFiles;

public class DesignStageServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid OtherStoreId = Guid.NewGuid();

    // --- SelectConfigurationAsync ---

    [Fact]
    public async Task SelectConfigurationAsync_ValidOffering_PersistsAndReturnsState()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemAndConfig(service, repo);

        var result = await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.State);
        Assert.Equal(offeringId, result.State.SelectedOfferingId);
    }

    [Fact]
    public async Task SaveArtworkPreferencesAsync_LegacyDesignArea_RoundTripsAndIsRestored()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        var areaId = repo.Snapshot.DesignAreas.Single(area => area.FulfillmentOfferingId == offeringId).Id;

        var result = await service.SaveArtworkPreferencesAsync(itemId, areaId, transparentBackground: true, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.True(state.HasPersistedArtworkTargetPreference);
        Assert.Equal(areaId, state.PersistedArtworkTargetId);
        Assert.True(state.PersistedTransparentBackground == true);
        Assert.Single(state.AvailablePlaceholders);
    }

    [Fact]
    public async Task SelectConfigurationAsync_ClearsPersistedArtworkPreferences()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        var areaId = repo.Snapshot.DesignAreas.Single(area => area.FulfillmentOfferingId == offeringId).Id;
        await service.SaveArtworkPreferencesAsync(itemId, areaId, transparentBackground: true, TestContext.Current.CancellationToken);

        var result = await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.False(state.HasPersistedArtworkTargetPreference);
        Assert.Null(state.PersistedArtworkTargetId);
        Assert.Null(state.PersistedTransparentBackground);
    }

    [Fact]
    public async Task SelectConfigurationAsync_CrossStoreOffering_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        repo.Snapshot = repo.Snapshot with { Items = [.. repo.Snapshot.Items, item] };

        // Create an offering in another store
        var otherProduct = new StoreProduct(Guid.NewGuid(), OtherStoreId, "Other", null, null, Now, Now, "{}");
        var otherOffering = new FulfillmentOffering(Guid.NewGuid(), otherProduct.Id, "O", null, FulfillmentKind.FixedProvider, "P", null, Now, Now, "{}");
        repo.Snapshot = repo.Snapshot with
        {
            StoreProducts = [.. repo.Snapshot.StoreProducts, otherProduct],
            FulfillmentOfferings = [.. repo.Snapshot.FulfillmentOfferings, otherOffering],
            Stores = [.. repo.Snapshot.Stores, new Store(OtherStoreId, "Other", null, false, Now, Now, "{}")]
        };

        var result = await service.SelectConfigurationAsync(itemId, otherOffering.Id, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("not valid", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectConfigurationAsync_ReadOnlyItem_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Published, WorkflowStage.Design, false, Now, Now, "{}");
        repo.Snapshot = repo.Snapshot with { Items = [.. repo.Snapshot.Items, item] };
        var offeringId = repo.Snapshot.FulfillmentOfferings[0].Id;

        var result = await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("published", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // --- AddSelectedColorAsync / RemoveSelectedColorAsync ---

    [Fact]
    public async Task AddSelectedColorAsync_CreatesDefaultRowAndAddsColor()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);

        var result = await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Contains(state.SelectedColors, c => c.Equals("Black", StringComparison.OrdinalIgnoreCase));
        Assert.Single(state.Rows);
        Assert.True(state.Rows[0].IsDefault);
    }

    [Fact]
    public async Task AddSelectedColorAsync_DuplicateIsIdempotent()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        var result = await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded); // Idempotent
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Single(state.SelectedColors);
    }

    [Fact]
    public async Task RemoveSelectedColorAsync_RemovesFromSelectedAndRow()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        var result = await service.RemoveSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Empty(state.SelectedColors);
        Assert.Empty(state.Rows[0].ColorValues);
    }

    // --- MakeSpecificForColorAsync ---

    [Fact]
    public async Task MakeSpecificForColorAsync_MovesColorToNewRow()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "White", TestContext.Current.CancellationToken);

        var result = await service.MakeSpecificForColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Equal(2, state.Rows.Count);
        var specific = state.Rows.Single(r => !r.IsDefault);
        Assert.Equal(["Black"], specific.ColorValues);
        Assert.Contains("White", state.Rows.Single(r => r.IsDefault).ColorValues);
    }

    [Fact]
    public async Task MakeSpecificForColorAsync_EmptyOldRow_RemovesIt()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        var result = await service.MakeSpecificForColorAsync(itemId, "Black", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        // Default row persists even when empty; new specific row also exists
        Assert.Equal(2, state.Rows.Count);
        var defaultRow = state.Rows.Single(r => r.IsDefault);
        var specificRow = state.Rows.Single(r => !r.IsDefault);
        Assert.Equal(["Black"], specificRow.ColorValues);
        Assert.Empty(defaultRow.ColorValues);
    }

    // --- RemoveSpecificRowAsync ---

    [Fact]
    public async Task RemoveSpecificRowAsync_RevertsColorsToDefaultAndRemovesRow()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var makeResult = await service.MakeSpecificForColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var specificRowId = makeResult!.State!.Rows.Single(r => !r.IsDefault).RowId;

        var result = await service.RemoveSpecificRowAsync(itemId, specificRowId, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Single(state.Rows);
        Assert.True(state.Rows[0].IsDefault);
        Assert.Contains("Black", state.Rows[0].ColorValues);
    }

    [Fact]
    public async Task RemoveSpecificRowAsync_DefaultRow_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        var result = await service.RemoveSpecificRowAsync(itemId, state.Rows[0].RowId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("default", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // --- AssignSlotImageAsync / Replace / Remove ---

    [Fact]
    public async Task AssignSlotImageAsync_NonPng_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        var result = await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, "test.jpg", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("PNG", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignSlotImageAsync_FileNotFound_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        var result = await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, "nonexistent.png", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AssignSlotImageAsync_DefaultRow_Success_FillsSlot()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var sourcePath = fileStore.CreateSourcePng();

        var result = await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, sourcePath, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var slot = reloaded.Rows[0].Slots[0];
        Assert.NotNull(slot.AssetId);
        Assert.True(slot.CanPreview);
    }

    [Fact]
    public async Task AssignSlotImageAsync_SpecificRow_Success_FillsSlot()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "White", TestContext.Current.CancellationToken);
        await service.MakeSpecificForColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var specificRow = state.Rows.Single(r => !r.IsDefault);
        var sourcePath = fileStore.CreateSourcePng();

        var result = await service.AssignSlotImageAsync(itemId, specificRow.RowId, specificRow.Slots[0].DesignAreaId, sourcePath, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var reloadedSpecificRow = reloaded.Rows.Single(r => !r.IsDefault);
        Assert.NotNull(reloadedSpecificRow.Slots[0].AssetId);
    }

    [Fact]
    public async Task AssignSlotImageAsync_MultipleSlots_PersistsIndependentArtwork()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        var secondArea = new DesignArea(
            Guid.NewGuid(), offeringId, "Back", null, "back", "DTG", 3000, 4500, null, Now, Now, "{}");
        repo.Snapshot = repo.Snapshot with { DesignAreas = [.. repo.Snapshot.DesignAreas, secondArea] };
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var row = state.Rows[0];
        Assert.Equal(2, row.Slots.Count);

        await service.AssignSlotImageAsync(itemId, row.RowId, row.Slots[0].DesignAreaId, fileStore.CreateSourcePng(), TestContext.Current.CancellationToken);
        await service.AssignSlotImageAsync(itemId, row.RowId, row.Slots[1].DesignAreaId, fileStore.CreateSourcePng(), TestContext.Current.CancellationToken);

        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var reloadedSlots = reloaded.Rows[0].Slots;
        Assert.All(reloadedSlots, slot => Assert.NotNull(slot.AssetId));
        Assert.NotEqual(reloadedSlots[0].AssetId, reloadedSlots[1].AssetId);
    }

    [Fact]
    public async Task ReplaceSlotImageAsync_ReplacesImageAndCleansUpOld()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var firstSource = fileStore.CreateSourcePng();
        await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, firstSource, TestContext.Current.CancellationToken);

        var secondSource = fileStore.CreateSourcePng();
        var result = await service.ReplaceSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, secondSource, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var slot = reloaded.Rows[0].Slots[0];
        Assert.NotNull(slot.AssetId);
        Assert.True(slot.CanPreview);
    }

    [Fact]
    public async Task AssignSlotImageAsync_FillTwice_ReplacesAndCleansUp()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var firstSource = fileStore.CreateSourcePng();
        await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, firstSource, TestContext.Current.CancellationToken);
        var afterFirst = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var firstAssetId = afterFirst.Rows[0].Slots[0].AssetId;

        var secondSource = fileStore.CreateSourcePng();
        var result = await service.AssignSlotImageAsync(itemId, state.Rows[0].RowId, state.Rows[0].Slots[0].DesignAreaId, secondSource, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var slot = reloaded.Rows[0].Slots[0];
        Assert.NotNull(slot.AssetId);
        Assert.NotEqual(firstAssetId, slot.AssetId);
    }

    [Fact]
    public async Task RemoveSlotImageAsync_ClearsAssignment()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, offeringId) = await AddItemWithConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        await service.AddSelectedColorAsync(itemId, "Black", TestContext.Current.CancellationToken);
        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        // Create a slot with an asset directly in the snapshot to test RemoveSlotImageAsync
        var row = state.Rows[0];
        var slot = row.Slots[0];
        var assetId = Guid.NewGuid();
        var asset = new Asset(assetId, StoreId, "test.png", null, AssetKind.ExportedImage, "designs/test.png", "source.png", false, false, Now, Now, "{}");
        var link = new AssetLink(assetId, WorkspaceEntityKind.Item, itemId);
        repo.Snapshot = repo.Snapshot with
        {
            DesignSlotAssignments = repo.Snapshot.DesignSlotAssignments
                .Where(a => !(a.RowId == row.RowId && a.DesignAreaId == slot.DesignAreaId))
                .Concat([new DesignSlotAssignment(row.RowId, slot.DesignAreaId, assetId)])
                .ToArray(),
            Assets = [.. repo.Snapshot.Assets, asset],
            AssetLinks = [.. repo.Snapshot.AssetLinks, link]
        };

        var removeResult = await service.RemoveSlotImageAsync(itemId, row.RowId, slot.DesignAreaId, TestContext.Current.CancellationToken);

        Assert.True(removeResult.Succeeded);
        var reloaded = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);
        var reloadedSlot = reloaded.Rows[0].Slots[0];
        Assert.Null(reloadedSlot.AssetId);
        // Asset record and link removed atomically
        Assert.DoesNotContain(assetId, repo.Snapshot.Assets.Select(a => a.Id));
    }

    // --- Supporting images ---

    [Fact]
    public async Task ListSupportingImagesAsync_EmptyWhenNoneImported()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var (itemId, _) = await AddItemWithConfig(service, repo);

        var images = await service.ListSupportingImagesAsync(itemId, TestContext.Current.CancellationToken);

        Assert.Empty(images);
    }

    [Fact]
    public async Task ImportSupportingImageAsync_NonExistentSource_Rejected()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, _) = await AddItemWithConfig(service, repo);

        var result = await service.ImportSupportingImageAsync(itemId, "nonexistent.jpg", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportSupportingImageAsync_Success_AppearsInList()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, _) = await AddItemWithConfig(service, repo);
        var sourcePath = fileStore.CreateSourcePng();

        var result = await service.ImportSupportingImageAsync(itemId, sourcePath, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var images = await service.ListSupportingImagesAsync(itemId, TestContext.Current.CancellationToken);
        Assert.NotEmpty(images);
    }

    [Fact]
    public async Task RemoveSupportingImageAsync_RemovesFromList()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var fileStore = new DeterministicFileStore();
        var service = new DesignStageService(repo, fileStore, () => Now, Guid.NewGuid);
        var (itemId, _) = await AddItemWithConfig(service, repo);
        var sourcePath = fileStore.CreateSourcePng();
        await service.ImportSupportingImageAsync(itemId, sourcePath, TestContext.Current.CancellationToken);
        var images = await service.ListSupportingImagesAsync(itemId, TestContext.Current.CancellationToken);
        var assetId = images[0].AssetId!.Value;

        var result = await service.RemoveSupportingImageAsync(itemId, assetId, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var reloaded = await service.ListSupportingImagesAsync(itemId, TestContext.Current.CancellationToken);
        Assert.Empty(reloaded);
    }

    // --- LoadDesignStageStateAsync ---

    [Fact]
    public async Task LoadDesignStageStateAsync_NoConfig_ShowsPromptState()
    {
        var repo = new InMemoryWorkspaceRepository(SeedWithProduct());
        var service = New(repo);
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        repo.Snapshot = repo.Snapshot with { Items = [.. repo.Snapshot.Items, item] };

        var state = await service.LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        Assert.Null(state.SelectedOfferingId);
        Assert.Empty(state.SelectedColors);
        Assert.Empty(state.Rows);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_ExcludesOfferingsMissingFromActiveCatalog()
    {
        var snapshot = SeedWithProduct();
        var product = snapshot.StoreProducts.Single();
        var activeLegacyOffering = snapshot.FulfillmentOfferings.Single();
        var staleLegacyOffering = new FulfillmentOffering(
            Guid.NewGuid(), product.Id, "Retired shirt", null, FulfillmentKind.FixedProvider,
            "Printful", null, Now, Now, "{}");
        var blueprint = new Blueprint(product.Id, StoreId, product.Name, product.Description, false, Now, Now, "{}");
        var activeNormalizedOffering = new BlueprintOffering(
            activeLegacyOffering.Id, blueprint.Id, StoreId, activeLegacyOffering.Name, activeLegacyOffering.Description,
            BlueprintOfferingKind.FixedPrintProvider, Guid.NewGuid(), null, null, null, false, Now, Now, "{}");
        var staleNormalizedOffering = new BlueprintOffering(
            Guid.NewGuid(), blueprint.Id, StoreId, staleLegacyOffering.Name, staleLegacyOffering.Description,
            BlueprintOfferingKind.FixedPrintProvider, Guid.NewGuid(), null, null, null, true, Now, Now, "{}");
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        var repo = new InMemoryWorkspaceRepository(snapshot with
        {
            Items = [item],
            FulfillmentOfferings = [activeLegacyOffering, staleLegacyOffering],
            Blueprints = [blueprint],
            BlueprintOfferings = [activeNormalizedOffering, staleNormalizedOffering]
        });

        var state = await New(repo).LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        var offering = Assert.Single(state.AvailableOfferings);
        Assert.Equal(activeLegacyOffering.Id, offering.Id);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_PreservesStaleConfiguredOfferingAndColorsReadOnly()
    {
        var snapshot = SeedWithProduct();
        var product = snapshot.StoreProducts.Single();
        var offering = snapshot.FulfillmentOfferings.Single();
        var blueprint = new Blueprint(product.Id, StoreId, product.Name, product.Description, false, Now, Now, "{}");
        var archivedOffering = new BlueprintOffering(
            offering.Id, blueprint.Id, StoreId, offering.Name, offering.Description,
            BlueprintOfferingKind.FixedPrintProvider, Guid.NewGuid(), null, null, null, true, Now, Now, "{}");
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        var repo = new InMemoryWorkspaceRepository(snapshot with
        {
            Items = [item],
            Blueprints = [blueprint],
            BlueprintOfferings = [archivedOffering],
            ItemListingConfigurations = [new ItemListingConfiguration(itemId, offering.Id)],
            DesignSelectedColors = [new DesignSelectedColor(itemId, "Black")]
        });

        var state = await New(repo).LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        Assert.Equal(offering.Id, state.SelectedOfferingId);
        Assert.True(state.IsReadOnly);
        Assert.Contains("read-only", state.ReadOnlyReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["Black", "White"], state.AvailableColors);
        Assert.Equal(["Black"], state.SelectedColors);
        Assert.Contains(state.AvailableOfferings, value => value.Id == offering.Id);
        Assert.True(state.HasStaleConfiguration);
        Assert.False(state.CanRecoverStaleConfiguration);
        Assert.Contains("create or restore", state.RecoveryGuidance, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_StaleConfigurationExposesOnlyActiveSameStoreRecoveryCandidates()
    {
        var fixture = CreateRecoveryFixture();
        var state = await New(fixture.Repository).LoadDesignStageStateAsync(
            fixture.ItemId, TestContext.Current.CancellationToken);

        Assert.True(state.IsReadOnly);
        Assert.True(state.HasStaleConfiguration);
        Assert.True(state.CanRecoverStaleConfiguration);
        Assert.Equal(fixture.StaleOfferingName, state.StaleConfigurationDisplayName);
        var candidate = Assert.Single(state.RecoveryOfferings);
        Assert.Equal(fixture.ReplacementOfferingId, candidate.Id);
        Assert.DoesNotContain(state.RecoveryOfferings, value => value.Id == fixture.StaleOfferingId);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_MissingConfiguredOfferingUsesStableFallbackIdentity()
    {
        var fixture = CreateRecoveryFixture();
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            FulfillmentOfferings = [.. fixture.Repository.Snapshot.FulfillmentOfferings.Where(value => value.Id != fixture.StaleOfferingId)],
            BlueprintOfferings = [.. fixture.Repository.Snapshot.BlueprintOfferings.Where(value => value.Id != fixture.StaleOfferingId)]
        };

        var state = await New(fixture.Repository).LoadDesignStageStateAsync(
            fixture.ItemId, TestContext.Current.CancellationToken);

        Assert.True(state.HasStaleConfiguration);
        Assert.Equal(fixture.StaleOfferingId, state.SelectedOfferingId);
        Assert.Contains(fixture.StaleOfferingId.ToString(), state.StaleConfigurationDisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(fixture.ReplacementOfferingId, Assert.Single(state.RecoveryOfferings).Id);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_ProtectedItemDoesNotExposeRecoveryAuthority()
    {
        var fixture = CreateRecoveryFixture();
        var item = fixture.Repository.Snapshot.Items.Single(value => value.Id == fixture.ItemId);
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            Items = [.. fixture.Repository.Snapshot.Items.Where(value => value.Id != fixture.ItemId), item with { Status = ItemStatus.Published }]
        };

        var state = await New(fixture.Repository).LoadDesignStageStateAsync(
            fixture.ItemId, TestContext.Current.CancellationToken);

        Assert.True(state.HasStaleConfiguration);
        Assert.False(state.CanRecoverStaleConfiguration);
        Assert.Contains("published", state.ReadOnlyReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecoverStaleConfigurationAsync_ValidReplacementResetsOnlyOfferingSpecificRelationships()
    {
        var fixture = CreateRecoveryFixture();
        var before = fixture.Repository.Snapshot;
        var service = New(fixture.Repository);

        var result = await service.RecoverStaleConfigurationAsync(
            fixture.ItemId, fixture.ReplacementOfferingId, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var after = fixture.Repository.Snapshot;
        Assert.Equal(fixture.ReplacementOfferingId,
            Assert.Single(after.ItemListingConfigurations, value => value.ItemId == fixture.ItemId).OfferingId);
        Assert.DoesNotContain(after.DesignSelectedColors, value => value.ItemId == fixture.ItemId);
        Assert.DoesNotContain(after.DesignVariantRows, value => value.ItemId == fixture.ItemId);
        Assert.DoesNotContain(after.DesignVariantRowColors, value => value.RowId == fixture.ItemRowId);
        Assert.DoesNotContain(after.DesignSlotAssignments, value => value.RowId == fixture.ItemRowId);
        Assert.Contains(after.DesignVariantRows, value => value.Id == fixture.OtherItemRowId);
        Assert.Contains(after.DesignSlotAssignments, value => value.RowId == fixture.OtherItemRowId);
        Assert.Equal(before.Assets, after.Assets);
        Assert.Equal(before.AssetLinks, after.AssetLinks);
        Assert.Equal(before.MockupTemplates, after.MockupTemplates);

        var updatedItem = after.Items.Single(value => value.Id == fixture.ItemId);
        Assert.Equal(WorkflowStage.Design, updatedItem.Stage);
        Assert.Equal(ItemStatus.Draft, updatedItem.Status);
        using var metadata = JsonDocument.Parse(updatedItem.MetadataJson);
        Assert.Equal("Keep idea", metadata.RootElement.GetProperty("idea").GetString());
        Assert.Equal("Keep SLL", metadata.RootElement.GetProperty("sll").GetString());
        Assert.False(metadata.RootElement.TryGetProperty("design.artworkTargetId", out _));
        Assert.False(metadata.RootElement.TryGetProperty("design.artworkTargetPreference", out _));
        Assert.False(metadata.RootElement.TryGetProperty("design.transparentBackground", out _));

        Assert.NotNull(result.State);
        Assert.False(result.State.HasStaleConfiguration);
        Assert.False(result.State.IsReadOnly);
        Assert.Equal(fixture.ReplacementOfferingId, result.State.SelectedOfferingId);
        Assert.Empty(result.State.SelectedColors);
        Assert.Empty(result.State.Rows);

        var reopened = await service.LoadDesignStageStateAsync(
            fixture.ItemId, TestContext.Current.CancellationToken);
        Assert.False(reopened.HasStaleConfiguration);
        Assert.False(reopened.IsReadOnly);
        Assert.Equal(fixture.ReplacementOfferingId, reopened.SelectedOfferingId);
        Assert.Empty(reopened.SelectedColors);
        Assert.Empty(reopened.Rows);
    }

    [Fact]
    public async Task RecoverStaleConfigurationAsync_InactiveOrCrossStoreReplacementLeavesSnapshotUnchanged()
    {
        var fixture = CreateRecoveryFixture();
        var before = fixture.Repository.Snapshot;
        var archivedReplacement = before.BlueprintOfferings.Single(value => value.Id == fixture.ReplacementOfferingId) with { IsArchived = true };
        fixture.Repository.Snapshot = before with
        {
            BlueprintOfferings = [.. before.BlueprintOfferings.Where(value => value.Id != fixture.ReplacementOfferingId), archivedReplacement]
        };
        var service = New(fixture.Repository);

        var result = await service.RecoverStaleConfigurationAsync(
            fixture.ItemId, fixture.ReplacementOfferingId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(before.ItemListingConfigurations, fixture.Repository.Snapshot.ItemListingConfigurations);
        Assert.Equal(before.DesignSelectedColors, fixture.Repository.Snapshot.DesignSelectedColors);
        Assert.Equal(before.DesignVariantRows, fixture.Repository.Snapshot.DesignVariantRows);
        Assert.Equal(before.DesignSlotAssignments, fixture.Repository.Snapshot.DesignSlotAssignments);
    }

    [Fact]
    public async Task RecoverStaleConfigurationAsync_ProtectedItemLeavesSnapshotUnchanged()
    {
        var fixture = CreateRecoveryFixture();
        var item = fixture.Repository.Snapshot.Items.Single(value => value.Id == fixture.ItemId);
        fixture.Repository.Snapshot = fixture.Repository.Snapshot with
        {
            Items = [.. fixture.Repository.Snapshot.Items.Where(value => value.Id != fixture.ItemId), item with { Status = ItemStatus.Published }]
        };
        var before = fixture.Repository.Snapshot;

        var result = await New(fixture.Repository).RecoverStaleConfigurationAsync(
            fixture.ItemId, fixture.ReplacementOfferingId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("published", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Same(before, fixture.Repository.Snapshot);
    }

    [Fact]
    public async Task RecoverStaleConfigurationAsync_SaveFailureDoesNotReplaceRepositorySnapshot()
    {
        var fixture = CreateRecoveryFixture();
        var before = fixture.Repository.Snapshot;
        fixture.Repository.ThrowOnSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => New(fixture.Repository).RecoverStaleConfigurationAsync(
            fixture.ItemId, fixture.ReplacementOfferingId, TestContext.Current.CancellationToken));

        Assert.Same(before, fixture.Repository.Snapshot);
    }

    [Fact]
    public async Task LoadDesignStageStateAsync_UsesCurrentCatalogColors()
    {
        var itemId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var colorOptionId = Guid.NewGuid();
        var blackId = Guid.NewGuid();
        var navyId = Guid.NewGuid();
        var heatherGrayId = Guid.NewGuid();
        var snapshot = SeedWithProduct();
        var legacyOffering = snapshot.FulfillmentOfferings[0];
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");

        snapshot = snapshot with
        {
            Items = [item],
            ItemListingConfigurations = [new ItemListingConfiguration(itemId, offeringId)],
            FulfillmentOfferings = [legacyOffering with { Id = offeringId }],
            Blueprints = [new Blueprint(blueprintId, StoreId, "Gildan", null, false, Now, Now)],
            BlueprintOfferings = [new BlueprintOffering(offeringId, blueprintId, StoreId, "Gildan 64000", null, BlueprintOfferingKind.FixedPrintProvider, null, null, null, null, false, Now, Now)],
            OfferingOptions = [new OfferingOption(colorOptionId, offeringId, OptionKind.Color, "Colors", 0)],
            OfferingOptionValues =
            [
                new OfferingOptionValue(blackId, colorOptionId, offeringId, "Black", 0),
                new OfferingOptionValue(navyId, colorOptionId, offeringId, "Navy", 1),
                new OfferingOptionValue(heatherGrayId, colorOptionId, offeringId, "Heather Gray", 2)
            ],
            OfferingVariants =
            [
                new OfferingVariant(Guid.NewGuid(), offeringId, "Black", [blackId], false, Now, Now),
                new OfferingVariant(Guid.NewGuid(), offeringId, "Navy", [navyId], false, Now, Now),
                new OfferingVariant(Guid.NewGuid(), offeringId, "Heather Gray", [heatherGrayId], false, Now, Now)
            ]
        };

        var state = await New(new InMemoryWorkspaceRepository(snapshot)).LoadDesignStageStateAsync(itemId, TestContext.Current.CancellationToken);

        Assert.Equal(["Black", "Navy", "Heather Gray"], state.AvailableColors);
    }

    // --- Helpers ---

    private static DesignStageService New(InMemoryWorkspaceRepository repo) =>
        new(repo, new DeterministicFileStore(), () => Now, Guid.NewGuid);

    private static async Task<(Guid itemId, Guid offeringId)> AddItemAndConfig(DesignStageService service, InMemoryWorkspaceRepository repo)
    {
        var itemId = Guid.NewGuid();
        var item = new Item(itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design, false, Now, Now, "{}");
        var offeringId = repo.Snapshot.FulfillmentOfferings[0].Id;
        repo.Snapshot = repo.Snapshot with { Items = [.. repo.Snapshot.Items, item] };
        return (itemId, offeringId);
    }

    private static async Task<(Guid itemId, Guid offeringId)> AddItemWithConfig(DesignStageService service, InMemoryWorkspaceRepository repo)
    {
        var (itemId, offeringId) = await AddItemAndConfig(service, repo);
        await service.SelectConfigurationAsync(itemId, offeringId, TestContext.Current.CancellationToken);
        return (itemId, offeringId);
    }

    private static WorkspaceSnapshot SeedWithProduct()
    {
        var product = new StoreProduct(Guid.NewGuid(), StoreId, "Gildan", null, null, Now, Now, "{}");
        var offering = new FulfillmentOffering(Guid.NewGuid(), product.Id, "Printful", null, FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        var variantBlack = new ProductVariant(Guid.NewGuid(), offering.Id, [new VariantOption("Color", "Black"), new VariantOption("Size", "M")], Now, Now);
        var variantWhite = new ProductVariant(Guid.NewGuid(), offering.Id, [new VariantOption("Color", "White"), new VariantOption("Size", "M")], Now, Now);
        var area = new DesignArea(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, null, Now, Now, "{}");

        return new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [new Store(StoreId, "Test Store", null, false, Now, Now, "{}")],
            [], [], [], [], [], [], [], [])
        {
            StoreProducts = [product],
            FulfillmentOfferings = [offering],
            ProductVariants = [variantBlack, variantWhite],
            DesignAreas = [area]
        };
    }

    private static RecoveryFixture CreateRecoveryFixture()
    {
        var snapshot = SeedWithProduct();
        var product = snapshot.StoreProducts.Single();
        var staleCompatibilityOffering = snapshot.FulfillmentOfferings.Single();
        var replacementOfferingId = Guid.NewGuid();
        var replacementCompatibilityOffering = new FulfillmentOffering(
            replacementOfferingId, product.Id, "Replacement shirt", null,
            FulfillmentKind.FixedProvider, "Printful", null, Now, Now, "{}");
        var blueprint = new Blueprint(product.Id, StoreId, product.Name, product.Description, false, Now, Now, "{}");
        var staleOffering = new BlueprintOffering(
            staleCompatibilityOffering.Id, blueprint.Id, StoreId, staleCompatibilityOffering.Name,
            staleCompatibilityOffering.Description, BlueprintOfferingKind.FixedPrintProvider,
            Guid.NewGuid(), null, null, null, true, Now, Now, "{}");
        var replacementOffering = new BlueprintOffering(
            replacementOfferingId, blueprint.Id, StoreId, replacementCompatibilityOffering.Name,
            replacementCompatibilityOffering.Description, BlueprintOfferingKind.FixedPrintProvider,
            Guid.NewGuid(), null, null, null, false, Now, Now, "{}");
        var replacementArea = new DesignArea(
            Guid.NewGuid(), replacementOfferingId, "Back", null, "back", "DTG",
            2400, 3200, null, Now, Now, "{}");
        var itemId = Guid.NewGuid();
        var itemRowId = Guid.NewGuid();
        var otherItemId = Guid.NewGuid();
        var otherItemRowId = Guid.NewGuid();
        var slotAssetId = Guid.NewGuid();
        var supportingAssetId = Guid.NewGuid();
        var oldArea = snapshot.DesignAreas.Single();
        var item = new Item(
            itemId, StoreId, null, null, "Item", null, ItemStatus.Draft, WorkflowStage.Design,
            false, Now, Now,
            $"{{\"idea\":\"Keep idea\",\"sll\":\"Keep SLL\",\"design.artworkTargetId\":\"{oldArea.Id}\",\"design.artworkTargetPreference\":\"true\",\"design.transparentBackground\":\"true\"}}");
        var otherItem = new Item(
            otherItemId, StoreId, null, null, "Other Item", null, ItemStatus.Draft,
            WorkflowStage.Design, false, Now, Now, "{}");
        var slotAsset = new Asset(
            slotAssetId, StoreId, "slot.png", null, AssetKind.SourceDesign,
            "designs/slot.png", null, false, false, Now, Now, "{}");
        var supportingAsset = new Asset(
            supportingAssetId, StoreId, "reference.png", null, AssetKind.ReferenceImage,
            "supporting/reference.png", null, false, false, Now, Now, "{}");

        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Items = [item, otherItem],
            Assets = [slotAsset, supportingAsset],
            AssetLinks = [new AssetLink(supportingAssetId, WorkspaceEntityKind.Item, itemId)],
            FulfillmentOfferings = [staleCompatibilityOffering, replacementCompatibilityOffering],
            DesignAreas = [oldArea, replacementArea],
            Blueprints = [blueprint],
            BlueprintOfferings = [staleOffering, replacementOffering],
            ItemListingConfigurations =
            [
                new ItemListingConfiguration(itemId, staleCompatibilityOffering.Id),
                new ItemListingConfiguration(otherItemId, replacementOfferingId)
            ],
            DesignSelectedColors = [new DesignSelectedColor(itemId, "Black"), new DesignSelectedColor(otherItemId, "White")],
            DesignVariantRows =
            [
                new DesignVariantRow(itemRowId, itemId, true, 0),
                new DesignVariantRow(otherItemRowId, otherItemId, true, 0)
            ],
            DesignVariantRowColors =
            [
                new DesignVariantRowColor(itemRowId, "Black"),
                new DesignVariantRowColor(otherItemRowId, "White")
            ],
            DesignSlotAssignments =
            [
                new DesignSlotAssignment(itemRowId, oldArea.Id, slotAssetId),
                new DesignSlotAssignment(otherItemRowId, replacementArea.Id, null)
            ]
        });

        return new RecoveryFixture(
            repository,
            itemId,
            itemRowId,
            otherItemRowId,
            staleCompatibilityOffering.Id,
            staleCompatibilityOffering.Name,
            replacementOfferingId);
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot ?? WorkspaceSnapshot.Empty;

        public WorkspaceSnapshot Snapshot
        {
            get => _snapshot;
            set => _snapshot = value;
        }

        public bool ThrowOnSave { get; set; }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("Simulated save failure.");
            }

            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed record RecoveryFixture(
        InMemoryWorkspaceRepository Repository,
        Guid ItemId,
        Guid ItemRowId,
        Guid OtherItemRowId,
        Guid StaleOfferingId,
        string StaleOfferingName,
        Guid ReplacementOfferingId);

    private sealed class DeterministicFileStore : IWorkspaceFileStore
    {
        private int _counter;

        public string WorkspaceRoot => Path.GetTempPath();

        public bool Exists(string workspaceRelativePath) => File.Exists(Path.Combine(WorkspaceRoot, workspaceRelativePath));

        public bool TryDelete(string workspaceRelativePath)
        {
            try
            {
                var fullPath = Path.Combine(WorkspaceRoot, workspaceRelativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string CreateSourcePng()
        {
            var path = Path.Combine(Path.GetTempPath(), $"test_design_{Interlocked.Increment(ref _counter)}.png");
            File.WriteAllBytes(path, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]); // Minimal PNG header
            return path;
        }

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("File not found", sourcePath);

            var relativePath = $"designs/{Guid.NewGuid():N}.png";
            var fullPath = Path.Combine(WorkspaceRoot, relativePath);
            var managed = new ManagedWorkspaceFile(
                Path.GetFileName(sourcePath),
                kind,
                relativePath,
                fullPath,
                sourcePath);
            return Task.FromResult(managed);
        }

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
        {
            var ms = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
            return Task.FromResult<Stream>(ms);
        }

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            File.WriteAllBytes(destinationPath, [0x89, 0x50, 0x4E, 0x47]);
            return Task.CompletedTask;
        }
    }
}
