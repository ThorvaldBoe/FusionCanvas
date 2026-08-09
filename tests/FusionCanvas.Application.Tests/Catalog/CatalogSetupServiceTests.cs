using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Catalog;

public sealed class CatalogSetupServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatesTypedOfferingGraphAndKeepsStoreIsolation()
    {
        var storeId = Guid.NewGuid();
        var otherStoreId = Guid.NewGuid();
        var repository = new MemoryRepository(new WorkspaceSnapshot(
            [WorkspaceSnapshot.DefaultWorkspace(Now)],
            [NewStore(storeId, "First"), NewStore(otherStoreId, "Second")], [], [], [], [], [], [], [], []));
        var service = new CatalogSetupService(repository, () => Now, Guid.NewGuid);

        var blueprint = await service.CreateBlueprintAsync(new CreateBlueprintRequest(storeId, "T-shirt"), TestContext.Current.CancellationToken);
        var blueprintId = Assert.Single(blueprint.State.Blueprints).Id;
        var provider = await service.CreatePrintProviderAsync(new CreatePrintProviderRequest(storeId, "Printful"), TestContext.Current.CancellationToken);
        var providerId = Assert.Single(provider.State.PrintProviders).Id;
        var offering = await service.CreateOfferingAsync(new CreateOfferingRequest(storeId, blueprintId, "Tee", BlueprintOfferingKind.FixedPrintProvider, providerId), TestContext.Current.CancellationToken);

        Assert.True(offering.Succeeded);
        Assert.Empty((await service.LoadForStoreAsync(otherStoreId, TestContext.Current.CancellationToken)).Blueprints);
    }

    [Fact]
    public async Task RejectsDuplicateVariantCombinationAndCrossOfferingPlaceholder()
    {
        var storeId = Guid.NewGuid();
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [NewStore(storeId, "First")], [], [], [], [], [], [], [], []));
        var service = new CatalogSetupService(repository, () => Now, Guid.NewGuid);
        var blueprint = await service.CreateBlueprintAsync(new CreateBlueprintRequest(storeId, "T-shirt"));
        var offering = await service.CreateOfferingAsync(new CreateOfferingRequest(storeId, blueprint.State.Blueprints[0].Id, "Tee", BlueprintOfferingKind.ProviderNetwork, ProviderNetworkCode: "printify-choice"));
        var option = await service.CreateOptionAsync(new CreateOfferingOptionRequest(offering.State.Offerings[0].Id, OptionKind.Color, "Color"));
        var value = await service.CreateOptionValueAsync(new CreateOptionValueRequest(offering.State.Offerings[0].Id, option.State.Options[0].Id, "Black"));
        var first = await service.CreateVariantAsync(new CreateOfferingVariantRequest(offering.State.Offerings[0].Id, "Black", [value.State.OptionValues[0].Id]));
        var duplicate = await service.CreateVariantAsync(new CreateOfferingVariantRequest(offering.State.Offerings[0].Id, "Black again", [value.State.OptionValues[0].Id]));

        Assert.True(first.Succeeded);
        Assert.False(duplicate.Succeeded);
        Assert.Contains("combination", duplicate.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportsDependenciesBeforeArchivingAndCanRestoreIndependentRecord()
    {
        var storeId = Guid.NewGuid();
        var blueprintId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [NewStore(storeId, "First")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [new Blueprint(blueprintId, storeId, "T-shirt", null, false, Now, Now)],
            PrintProviders = [new PrintProvider(providerId, storeId, "Printful", null, true, Now, Now)],
            BlueprintOfferings = [new BlueprintOffering(offeringId, blueprintId, storeId, "Tee", null, BlueprintOfferingKind.FixedPrintProvider, providerId, null, null, null, false, Now, Now)]
        });
        var service = new CatalogSetupService(repository, () => Now, Guid.NewGuid);

        var blocked = await service.ArchiveAsync(new ArchiveCatalogRecordRequest(storeId, CatalogRecordKind.Blueprint, blueprintId));
        var restored = await service.RestoreAsync(new ArchiveCatalogRecordRequest(storeId, CatalogRecordKind.PrintProvider, providerId));

        Assert.False(blocked.Succeeded);
        Assert.Contains("referenced", blocked.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(restored.Succeeded);
        Assert.False(restored.State.PrintProviders.Single().IsArchived);
    }

    [Fact]
    public async Task UpdatesCatalogRecordsAndRoutesDeleteThroughArchivalPolicy()
    {
        var storeId = Guid.NewGuid();
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [NewStore(storeId, "First")], [], [], [], [], [], [], [], []));
        var service = new CatalogSetupService(repository, () => Now, Guid.NewGuid);
        var created = await service.CreateBlueprintAsync(new CreateBlueprintRequest(storeId, "Old name"));
        var blueprintId = Assert.Single(created.State.Blueprints).Id;

        var updated = await service.UpdateAsync(new UpdateCatalogRecordRequest(storeId, CatalogRecordKind.Blueprint, blueprintId, "New name", "Updated description"));
        var deleted = await service.DeleteAsync(new ArchiveCatalogRecordRequest(storeId, CatalogRecordKind.Blueprint, blueprintId));

        Assert.True(updated.Succeeded);
        Assert.Equal("New name", updated.State.Blueprints.Single().Name);
        Assert.True(deleted.Succeeded);
        Assert.True(deleted.State.Blueprints.Single().IsArchived);
    }

    private static Store NewStore(Guid id, string name) => new(id, name, null, false, Now, Now, "{}");

    private sealed class MemoryRepository(WorkspaceSnapshot? initial = null) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = initial ?? WorkspaceSnapshot.Empty;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { _snapshot = snapshot; return Task.CompletedTask; }
    }
}
