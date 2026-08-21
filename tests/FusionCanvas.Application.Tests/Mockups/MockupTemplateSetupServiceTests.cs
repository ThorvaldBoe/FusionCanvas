using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.Mockups;

public sealed class MockupTemplateSetupServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TemplateRequiresSameOfferingPlaceholderAndColorCreatesRevision()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [colorOption], OfferingOptionValues = [color], OfferingPlaceholders = [placeholder]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);

        var template = await service.CreateTemplateAsync(new CreateMockupTemplateRequest(storeId, offering.Id, "Front", placeholder.Id), TestContext.Current.CancellationToken);
        Assert.True(template.Succeeded);
        var added = await service.AddColorAsync(new AddMockupTemplateColorRequest(storeId, template.State.Templates[0].Id, color.Id), TestContext.Current.CancellationToken);

        Assert.True(added.Succeeded);
        Assert.Single(added.State.Colors);
        Assert.Equal(2, added.State.Templates[0].CurrentRevision);
        Assert.Equal(2, added.State.Revisions.Count);
    }

    [Fact]
    public async Task ArchivedStoreIsReadOnly()
    {
        var store = new Store(Guid.NewGuid(), "Store", null, true, Now, Now, "{}");
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [store], [], [], [], [], [], [], [], []));
        var result = await new MockupTemplateSetupService(repository).CreateTemplateAsync(new CreateMockupTemplateRequest(store.Id, Guid.NewGuid(), "Front", Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Contains("read-only", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArchivingTemplateArchivesBindingsAndRestoreReactivatesTemplate()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, placeholder.Id, "Front", null, 1, false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingPlaceholders = [placeholder], MockupTemplates = [template]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);

        var archived = await service.ArchiveTemplateAsync(new ArchiveMockupTemplateRequest(storeId, template.Id));
        var restored = await service.RestoreTemplateAsync(new ArchiveMockupTemplateRequest(storeId, template.Id));

        Assert.True(archived.Succeeded);
        Assert.True(archived.State.Templates.Single().IsArchived);
        Assert.True(restored.Succeeded);
        Assert.False(restored.State.Templates.Single().IsArchived);
    }

    [Fact]
    public async Task DisplayOnlyUpdateDoesNotCreateOutputRevision()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingPlaceholders = [placeholder]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);
        var created = await service.CreateTemplateAsync(new CreateMockupTemplateRequest(storeId, offering.Id, "Front", placeholder.Id), TestContext.Current.CancellationToken);

        var updated = await service.UpdateTemplateAsync(new UpdateMockupTemplateRequest(storeId, created.State.Templates.Single().Id, "Front updated"), TestContext.Current.CancellationToken);

        Assert.True(updated.Succeeded);
        Assert.Equal("Front updated", updated.State.Templates.Single().Name);
        Assert.Equal(1, updated.State.Templates.Single().CurrentRevision);
        Assert.Single(updated.State.Revisions);
    }

    [Fact]
    public async Task ProviderImageMappingUpdateCreatesRevisionAndColorChangesPreserveIt()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [colorOption], OfferingOptionValues = [black], OfferingPlaceholders = [placeholder]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);
        var created = await service.CreateTemplateAsync(new CreateMockupTemplateRequest(storeId, offering.Id, "Front", placeholder.Id), TestContext.Current.CancellationToken);
        var templateId = created.State.Templates.Single().Id;
        var mapping = new MockupImageSpaceMapping(1200, 1200, 300, 250, 500, 600);

        var mapped = await service.UpdateTemplateAsync(new UpdateMockupTemplateRequest(storeId, templateId, ReplaceProviderImage: true, ProviderMockupReference: "front-black", ImageMapping: mapping), TestContext.Current.CancellationToken);
        var colored = await service.AddColorAsync(new AddMockupTemplateColorRequest(storeId, templateId, black.Id), TestContext.Current.CancellationToken);
        var archived = await service.ArchiveColorAsync(new ArchiveMockupTemplateColorRequest(storeId, colored.State.Colors.Single().Id), TestContext.Current.CancellationToken);

        Assert.True(mapped.Succeeded);
        Assert.Equal(mapping, mapped.State.Revisions.Single(value => value.RevisionNumber == 2).ImageMapping);
        Assert.Equal("front-black", colored.State.Revisions.Single(value => value.RevisionNumber == 3).ProviderMockupReference);
        Assert.Equal(mapping, archived.State.Revisions.Single(value => value.RevisionNumber == 4).ImageMapping);
        Assert.Equal(4, archived.State.Templates.Single().CurrentRevision);
    }

    [Fact]
    public async Task IncompleteProviderImageConfigurationIsRejectedWithoutRevision()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingPlaceholders = [placeholder]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);
        var created = await service.CreateTemplateAsync(new CreateMockupTemplateRequest(storeId, offering.Id, "Front", placeholder.Id), TestContext.Current.CancellationToken);

        var result = await service.UpdateTemplateAsync(new UpdateMockupTemplateRequest(storeId, created.State.Templates.Single().Id, ReplaceProviderImage: true, ProviderMockupReference: "front-black"), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Single(result.State.Revisions);
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = initial;
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { _snapshot = snapshot; return Task.CompletedTask; }
    }
}
