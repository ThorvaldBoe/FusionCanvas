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
    public async Task DuplicateTemplateCopiesCurrentConfigurationWithNewTemplateScopedIdentities()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [], false, Now, Now);
        var sourceTemplate = new MockupTemplate(Guid.NewGuid(), offering.Id, placeholder.Id, "Front", "Notes", 2, false, Now, Now, "A", null, "{\"kind\":\"copy\"}");
        var sourceRevision = new MockupTemplateRevision(Guid.NewGuid(), sourceTemplate.Id, 2, placeholder.Id, Now, "Current", "provider-front", new MockupImageSpaceMapping(1200, 1200, 100, 100, 700, 800));
        var sourceAssetId = Guid.NewGuid();
        var sourceImage = new MockupTemplateSourceImage(Guid.NewGuid(), sourceTemplate.Id, sourceAssetId, new MockupImageSpaceMapping(1200, 1200, 10, 20, 500, 600), false, Now, Now);
        var sourceCondition = new MockupTemplateSourceImageOptionValue(sourceImage.Id, black.Id);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [colorOption], OfferingOptionValues = [black], OfferingPlaceholders = [placeholder],
            MockupTemplates = [sourceTemplate], MockupTemplateColorVariants = [new(Guid.NewGuid(), sourceTemplate.Id, black.Id, false, Now, Now)],
            MockupTemplateRevisions = [sourceRevision], MockupTemplateSourceImages = [sourceImage], MockupTemplateSourceImageOptionValues = [sourceCondition]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);

        var result = await service.DuplicateTemplateAsync(new DuplicateMockupTemplateRequest(storeId, sourceTemplate.Id), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        var duplicate = result.State.Templates.Single(value => value.Id == result.TemplateId);
        Assert.Equal("Copy of Front", duplicate.Name);
        Assert.Equal(sourceTemplate with { Id = duplicate.Id, Name = duplicate.Name, CurrentRevision = 1, CreatedAt = Now, UpdatedAt = Now }, duplicate);
        var duplicateImage = repository.Snapshot.MockupTemplateSourceImages.Single(value => value.MockupTemplateId == duplicate.Id);
        Assert.Equal(sourceAssetId, duplicateImage.SourceAssetId);
        Assert.NotEqual(sourceImage.Id, duplicateImage.Id);
        Assert.Equal(black.Id, repository.Snapshot.MockupTemplateSourceImageOptionValues.Single(value => value.SourceImageId == duplicateImage.Id).OptionValueId);
        Assert.Equal("provider-front", repository.Snapshot.MockupTemplateRevisions.Single(value => value.MockupTemplateId == duplicate.Id).ProviderMockupReference);
        Assert.Equal(sourceTemplate.Id, repository.Snapshot.MockupTemplateSourceImages.Single(value => value.Id == sourceImage.Id).MockupTemplateId);
    }

    [Fact]
    public async Task DuplicateTemplateUsesNextAvailableCopyNameAndRejectsArchivedSource()
    {
        var storeId = Guid.NewGuid();
        var offering = new BlueprintOffering(Guid.NewGuid(), Guid.NewGuid(), storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, Now, Now);
        var source = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Front", null, 1, false, Now, Now);
        var firstCopy = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Copy of Front", null, 1, false, Now, Now);
        var secondCopy = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Copy of Front (2)", null, 1, false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            BlueprintOfferings = [offering], MockupTemplates = [source, firstCopy, secondCopy]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);

        var result = await service.DuplicateTemplateAsync(new DuplicateMockupTemplateRequest(storeId, source.Id), TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded);
        Assert.Equal("Copy of Front (3)", result.State.Templates.Single(value => value.Id == result.TemplateId).Name);

        repository.SetSnapshot(repository.Snapshot with { MockupTemplates = [source with { IsArchived = true }] });
        var rejected = await service.DuplicateTemplateAsync(new DuplicateMockupTemplateRequest(storeId, source.Id), TestContext.Current.CancellationToken);
        Assert.False(rejected.Succeeded);
        Assert.Contains("not found", rejected.Error, StringComparison.OrdinalIgnoreCase);
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
    public async Task PartialProviderImageConfigurationPersistsAsDraft()
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

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.State.Revisions.Count);
        Assert.Equal(MockupTemplateLifecycle.Draft, result.State.Readiness!.Single().Lifecycle);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingMapping, result.State.Readiness!.Single().Blockers);
    }

    [Fact]
    public async Task NameOnlyTemplateSavesOnceWithoutProviderAndReturnsStableId()
    {
        var storeId = Guid.NewGuid();
        var blueprint = new Blueprint(Guid.NewGuid(), storeId, "T-shirt", null, false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, storeId, "Tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, Now, Now);
        var repository = new MemoryRepository(new WorkspaceSnapshot([WorkspaceSnapshot.DefaultWorkspace(Now)], [new Store(storeId, "Store", null, false, Now, Now, "{}")], [], [], [], [], [], [], [], [])
        {
            Blueprints = [blueprint], BlueprintOfferings = [offering]
        });
        var service = new MockupTemplateSetupService(repository, () => Now, Guid.NewGuid);

        var result = await service.CreateTemplateAsync(new CreateMockupTemplateRequest(storeId, offering.Id, "Manual draft"), TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(result.TemplateId, result.State.Templates.Single().Id);
        Assert.Null(result.State.Templates.Single().TargetPlaceholderId);
        Assert.Equal(MockupTemplateLifecycle.Draft, result.State.Readiness!.Single().Lifecycle);
        Assert.Equal(1, repository.SaveCount);
        var eligibility = await service.GetEligibleTemplatesAsync(storeId, offering.Id, result.TemplateId, TestContext.Current.CancellationToken);
        Assert.False(eligibility.Succeeded);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingImage, eligibility.Blockers);
        Assert.Single(eligibility.CandidateDiagnostics);
        Assert.Equal("Manual draft", eligibility.CandidateDiagnostics[0].TemplateName);
        Assert.Contains(MockupTemplateReadinessBlocker.MissingMapping, eligibility.CandidateDiagnostics[0].Blockers);
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = initial;
        public WorkspaceSnapshot Snapshot => _snapshot;
        public int SaveCount { get; private set; }
        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_snapshot);
        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default) { SaveCount++; _snapshot = snapshot; return Task.CompletedTask; }
        public void SetSnapshot(WorkspaceSnapshot snapshot) => _snapshot = snapshot;
    }
}
