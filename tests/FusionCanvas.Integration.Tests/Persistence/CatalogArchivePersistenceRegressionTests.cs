using FusionCanvas.Application.Catalog;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class CatalogArchivePersistenceRegressionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CascadeArchive_ReloadsWithoutResurrection(bool blueprintCascade)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("archive.db");
        var store = new Store(Guid.NewGuid(), "Store", null, false, Now, Now, "{}");
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "Tee", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Offering", null, BlueprintOfferingKind.ProviderNetwork, null, "network", null, null, false, Now, Now);
        var option = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var value = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "Black", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black", [value.Id], false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, [variant.Id], false, Now, Now);
        offering = offering with { PrimaryArtworkDesignAreaId = area.Id };
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front", null, 1, false, Now, Now);
        var asset = new Asset(Guid.NewGuid(), store.Id, "front.png", null, AssetKind.MockupImage, "front.png", null, false, false, Now, Now, "{}");
        var source = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, asset.Id, null, false, Now, Now, 1200, 1400);
        var snapshot = new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [asset], [], [], [], [new AssetLink(asset.Id, WorkspaceEntityKind.Store, store.Id)])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [option], OfferingOptionValues = [value], OfferingVariants = [variant], OfferingPlaceholders = [area], MockupTemplates = [template], MockupTemplateSourceImages = [source]
        };
        var repository = new SqliteWorkspaceRepository(path, useConnectionPooling: false);
        await repository.SaveAsync(snapshot);
        var service = new CatalogSetupService(repository, () => Now, Guid.NewGuid);
        await service.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        var result = blueprintCascade
            ? await service.ArchiveBlueprintWithDependentsAsync(new ArchiveBlueprintWithDependentsRequest(store.Id, blueprint.Id), TestContext.Current.CancellationToken)
            : await service.ArchiveOfferingCascadeAsync(new ArchiveOfferingCascadeRequest(store.Id, offering.Id), TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error);
        var reloadedRepository = new SqliteWorkspaceRepository(path, useConnectionPooling: false);
        var reloadedService = new CatalogSetupService(reloadedRepository, () => Now, Guid.NewGuid);
        await reloadedService.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        await reloadedService.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        var reloaded = await reloadedRepository.LoadAsync(TestContext.Current.CancellationToken);
        var again = CatalogCompatibilitySynchronizer.SynchronizeStore(reloaded, store.Id, () => Now, Guid.NewGuid).Snapshot;
        Assert.True(again.Blueprints.Single().IsArchived == blueprintCascade);
        Assert.True(again.BlueprintOfferings.Single().IsArchived);
        Assert.True(again.OfferingOptions.Single().IsArchived);
        Assert.True(again.OfferingOptionValues.Single().IsArchived);
        Assert.True(again.OfferingVariants.Single().IsArchived);
        Assert.True(again.OfferingPlaceholders.Single().IsArchived);
        Assert.Equal(template.Id, again.MockupTemplates.Single().Id);
        Assert.Equal(area.Id, again.BlueprintOfferings.Single().PrimaryArtworkDesignAreaId);
        Assert.Single(again.MockupTemplateSourceImages);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public string GetPath(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
