using FusionCanvas.App.Commands;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.App.Tests.TestSupport;

namespace FusionCanvas.App.Tests;

public sealed class CatalogSetupViewModelTests
{
    [Fact]
    public async Task MockupTemplateSearchFiltersEveryDisplayedFieldCaseInsensitivelyAndPreservesOrder()
    {
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true);
        viewModel.MockupTemplateCards.Clear();
        var zeta = new MockupTemplateCardViewModel(Guid.NewGuid(), "Zeta", "Back Panel", "Colors: Cyan", "5 compatible Variants", 4, "Ready for use");
        var alpha = new MockupTemplateCardViewModel(Guid.NewGuid(), "Alpha", "Front Panel", "Colors: Matte Black / Gray", "2 compatible Variants", 2, "Draft");
        var beta = new MockupTemplateCardViewModel(Guid.NewGuid(), "Beta", "Sleeve", "Colors: Navy", "1 compatible Variant", 7, "Draft");
        viewModel.MockupTemplateCards.Add(zeta);
        viewModel.MockupTemplateCards.Add(alpha);
        viewModel.MockupTemplateCards.Add(beta);

        viewModel.MockupTemplateSearchText = "bAcK pAnEl";
        Assert.Same(zeta, Assert.Single(viewModel.FilteredMockupTemplateCards));
        viewModel.MockupTemplateSearchText = "mAtTe bLaCk";
        Assert.Same(alpha, Assert.Single(viewModel.FilteredMockupTemplateCards));
        viewModel.MockupTemplateSearchText = "5 COMPATIBLE";
        Assert.Same(zeta, Assert.Single(viewModel.FilteredMockupTemplateCards));
        viewModel.MockupTemplateSearchText = "READY FOR USE";
        Assert.Same(zeta, Assert.Single(viewModel.FilteredMockupTemplateCards));
        viewModel.MockupTemplateSearchText = "compatible variant";
        Assert.Equal([zeta, alpha, beta], viewModel.FilteredMockupTemplateCards);

        viewModel.MockupTemplateSearchText = "no match";
        Assert.Empty(viewModel.FilteredMockupTemplateCards);
        Assert.True(viewModel.HasNoMockupTemplateSearchResults);
        viewModel.MockupTemplateSearchText = "  ";
        Assert.Equal([zeta, alpha, beta], viewModel.FilteredMockupTemplateCards);
        Assert.False(viewModel.HasNoMockupTemplateSearchResults);

        viewModel.MockupTemplateSearchText = "zeta";
        viewModel.SelectOffering(viewModel.Offerings.First(value => value.Id != offering.Id).Id);
        Assert.Equal(string.Empty, viewModel.MockupTemplateSearchText);
        Assert.Empty(viewModel.FilteredMockupTemplateCards);
    }

    [Fact]
    public async Task MockupTemplateArchiveRequiresConfirmationAndCancelPreservesTemplateAndRevision()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true, completeLocalSource: true);
        var card = Assert.Single(viewModel.MockupTemplateCards);
        var revision = Assert.Single(viewModel.TemplateRevisions);
        var requests = 0;
        viewModel.MockupTemplateArchiveConfirmationRequested += (_, _) => requests++;

        viewModel.ArchiveTemplateCommand.Execute(card);

        Assert.Equal(1, requests);
        Assert.True(viewModel.IsMockupTemplateArchiveConfirmationVisible);
        Assert.Equal(card.Id, viewModel.PendingMockupTemplateArchiveId);
        Assert.Contains(card.Name, viewModel.MockupTemplateArchiveConfirmationMessage);
        Assert.Contains("saved revisions will be retained", viewModel.MockupTemplateArchiveConfirmationMessage);
        Assert.False(Assert.Single(viewModel.Templates).IsArchived);
        Assert.Same(revision, Assert.Single(viewModel.TemplateRevisions));

        viewModel.CancelMockupTemplateArchiveCommand.Execute(null);

        Assert.False(viewModel.IsMockupTemplateArchiveConfirmationVisible);
        Assert.False(Assert.Single(viewModel.Templates).IsArchived);
        Assert.Same(revision, Assert.Single(viewModel.TemplateRevisions));

        viewModel.ArchiveTemplateCommand.Execute(card);
        var confirm = Assert.IsType<AsyncRelayCommand>(viewModel.ConfirmMockupTemplateArchiveCommand);
        confirm.Execute(null);
        await confirm.ExecutionTask!.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(viewModel.Templates).IsArchived);
        Assert.Same(revision, Assert.Single(viewModel.TemplateRevisions));
        Assert.Empty(viewModel.MockupTemplateCards);
    }

    [Fact]
    public async Task TemplateColorSearchFiltersCaseInsensitivelyAndRestoresOriginalOrder()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.StartAddTemplateCommand.Execute(null);

        var black = Assert.Single(viewModel.TemplateColorChoices);
        var forestGreen = new OfferingOptionValue(Guid.NewGuid(), black.Value.OptionId, black.Value.OfferingId, "Forest Green", 1);
        var lime = new OfferingOptionValue(Guid.NewGuid(), black.Value.OptionId, black.Value.OfferingId, "Lime", 2);
        viewModel.TemplateColorChoices.Add(new OptionValueChoiceViewModel(forestGreen, "Colors: Forest Green"));
        viewModel.TemplateColorChoices.Add(new OptionValueChoiceViewModel(lime, "Colors: Lime"));

        viewModel.TemplateColorSearchText = "  GREEN ";

        Assert.Equal(["Colors: Forest Green"], viewModel.FilteredTemplateColorChoices.Select(value => value.Label));
        Assert.False(viewModel.HasNoMatchingTemplateColors);

        viewModel.TemplateColorSearchText = "   ";

        Assert.Equal(viewModel.TemplateColorChoices, viewModel.FilteredTemplateColorChoices);
        Assert.False(viewModel.HasNoMatchingTemplateColors);
    }

    [Fact]
    public async Task TemplateColorSearchShowsNoMatchStateAndPreservesHiddenSelection()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.SelectedPlaceholder = area;

        var black = Assert.Single(viewModel.TemplateColorChoices);
        var lime = new OfferingOptionValue(Guid.NewGuid(), black.Value.OptionId, black.Value.OfferingId, "Lime", 1);
        var limeChoice = new OptionValueChoiceViewModel(lime, "Colors: Lime");
        viewModel.TemplateColorChoices.Add(limeChoice);
        black.IsSelected = true;
        limeChoice.IsSelected = true;

        viewModel.TemplateColorSearchText = "not-a-color";

        Assert.Empty(viewModel.FilteredTemplateColorChoices);
        Assert.True(viewModel.HasNoMatchingTemplateColors);
        Assert.Equal(
            [black.Value.Id, limeChoice.Value.Id],
            viewModel.TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id));
        Assert.DoesNotContain("missing Color", string.Join(" ", viewModel.MockupTemplateReadinessMessages), StringComparison.OrdinalIgnoreCase);

        viewModel.TemplateColorSearchText = "lime";
        Assert.Same(limeChoice, Assert.Single(viewModel.FilteredTemplateColorChoices));
        Assert.True(Assert.Single(viewModel.FilteredTemplateColorChoices).IsSelected);

        viewModel.StartAddTemplateCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.TemplateColorSearchText);
        Assert.False(viewModel.HasNoMatchingTemplateColors);
    }

    [Fact]
    public async Task LocalSourceSortingUsesVisibleKeysAndPreservesSelectedDraft()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.StartAddTemplateCommand.Execute(null);
        var zeta = new LocalMockupSourceDraftViewModel("zeta.png", []);
        var alpha = new LocalMockupSourceDraftViewModel("alpha.png", []);
        var beta = new LocalMockupSourceDraftViewModel("beta.png", []);
        alpha.UpdateMetadata([Guid.NewGuid()], new MockupImageSpaceMapping(100, 100, 0, 0, 50, 50), "Navy");
        beta.UpdateMetadata([Guid.NewGuid()], new MockupImageSpaceMapping(100, 100, 0, 0, 50, 50), "Black");
        viewModel.LocalSourceDrafts.Add(zeta);
        viewModel.LocalSourceDrafts.Add(alpha);
        viewModel.LocalSourceDrafts.Add(beta);
        viewModel.SelectLocalSourceCommand.Execute(zeta);

        viewModel.SortLocalSourcesCommand.Execute("File");
        Assert.Equal([zeta, beta, alpha], viewModel.LocalSourceDrafts);
        Assert.Equal("File ↓", viewModel.FileSortLabel);
        viewModel.SortLocalSourcesCommand.Execute("File");
        Assert.Equal([alpha, beta, zeta], viewModel.LocalSourceDrafts);

        viewModel.SortLocalSourcesCommand.Execute("Applicability");
        Assert.Equal([zeta, beta, alpha], viewModel.LocalSourceDrafts);
        Assert.Equal("Applicability ↑", viewModel.ApplicabilitySortLabel);
        viewModel.SortLocalSourcesCommand.Execute("Applicability");
        Assert.Equal([alpha, beta, zeta], viewModel.LocalSourceDrafts);

        viewModel.SortLocalSourcesCommand.Execute("Status");
        Assert.Equal([alpha, beta, zeta], viewModel.LocalSourceDrafts);
        viewModel.SortLocalSourcesCommand.Execute("Status");
        Assert.Equal([zeta, alpha, beta], viewModel.LocalSourceDrafts);
        Assert.Equal("Status, sorted descending", viewModel.StatusSortAccessibleName);
        Assert.Same(zeta, viewModel.SelectedLocalSource);
        Assert.True(zeta.IsSelected);
    }

    [Fact]
    public async Task LocalSourceSelectionSupportsReplaceToggleRangeAndBulkArchive()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.StartAddTemplateCommand.Execute(null);
        var first = new LocalMockupSourceDraftViewModel("01-first.png", []);
        var second = new LocalMockupSourceDraftViewModel("02-second.png", []);
        var third = new LocalMockupSourceDraftViewModel("03-third.png", []);
        var fourth = new LocalMockupSourceDraftViewModel("04-fourth.png", []);
        viewModel.LocalSourceDrafts.Add(first);
        viewModel.LocalSourceDrafts.Add(second);
        viewModel.LocalSourceDrafts.Add(third);
        viewModel.LocalSourceDrafts.Add(fourth);

        viewModel.SelectLocalSourceCommand.Execute(first);
        viewModel.SelectLocalSourceWithModifiers(third, toggle: true, range: false);
        Assert.Equal([first, third], viewModel.SelectedLocalSources);
        Assert.Same(third, viewModel.SelectedLocalSource);

        viewModel.SelectLocalSourceWithModifiers(fourth, toggle: false, range: true);
        Assert.Equal([first, second, third, fourth], viewModel.SelectedLocalSources);
        Assert.Equal("4 source images selected", viewModel.LocalSourceSelectionSummary);
        Assert.Equal("Archive selected (4)", viewModel.LocalSourceArchiveLabel);

        viewModel.SelectLocalSourceWithModifiers(second, toggle: true, range: false);
        Assert.Equal([first, third, fourth], viewModel.SelectedLocalSources);
        viewModel.RemoveLocalSourceCommand.Execute(null);

        Assert.Equal([second], viewModel.LocalSourceDrafts);
        Assert.Same(second, viewModel.SelectedLocalSource);
        Assert.Equal(1, viewModel.SelectedLocalSourceCount);

    }

    [Fact]
    public async Task OfferingReadinessLoadFailureIsVisibleAndDoesNotFabricateBlockers()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            offeringManagement: new ThrowingOfferingManagementService());

        Assert.True(viewModel.HasOfferingReadinessError);
        Assert.Contains("Offering readiness could not be evaluated", viewModel.OfferingReadinessError);
        Assert.Contains("reloading the Offering", viewModel.OfferingReadinessError);
        Assert.False(viewModel.HasOfferingReadinessGuidance);
        Assert.Equal(0, viewModel.ReadyMockupTemplateCount);
    }

    [Fact]
    public async Task BrowseLocalSourceGetsDimensionsFromMetadataService()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var metadata = new FixedRasterImageMetadataReader(new RasterImageInfo(1600, 1200));
        var sourceImages = new RecordingSourceImageService();
        const string selectedPath = "mockup-source.png";
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            sourceImages: sourceImages,
            filePicker: new FixedLocalSourceFilePicker(selectedPath),
            rasterImageMetadataReader: metadata);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.StartAddTemplateCommand.Execute(null);

        viewModel.BrowseLocalSourceCommand.Execute(null);

        var draft = Assert.Single(viewModel.LocalSourceDrafts);
        Assert.Equal(selectedPath, Assert.Single(metadata.ReadPaths));
        Assert.Equal(1600, draft.ImageWidth);
        Assert.Equal(1200, draft.ImageHeight);
    }

    [Fact]
    public async Task BrowseLocalSourcesStagesEachSelectedFileAndSelectsFirstDraft()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var metadata = new FixedRasterImageMetadataReader(new RasterImageInfo(1600, 1200));
        var sourceImages = new RecordingSourceImageService();
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            sourceImages: sourceImages,
            filePicker: new FixedLocalSourceFilesPicker(["first.png", "second.jpg"]),
            rasterImageMetadataReader: metadata);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.StartAddTemplateCommand.Execute(null);

        var command = Assert.IsType<AsyncRelayCommand>(viewModel.BrowseLocalSourceCommand);
        command.Execute(null);
        await command.ExecutionTask!;

        Assert.Equal(["first.png", "second.jpg"], viewModel.LocalSourceDrafts.Select(value => value.Path));
        Assert.Same(viewModel.LocalSourceDrafts[0], viewModel.SelectedLocalSource);
        Assert.All(viewModel.LocalSourceDrafts, draft =>
        {
            Assert.Empty(draft.OptionValueIds);
            Assert.Null(draft.Mapping);
            Assert.Equal(1600, draft.ImageWidth);
            Assert.Equal(1200, draft.ImageHeight);
        });
        Assert.Equal(["first.png", "second.jpg"], metadata.ReadPaths);
    }

    [Fact]
    public async Task CoverageExemplarPrefillsSafeApplicabilityAndMatchingMapping()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            sourceImages: new RecordingSourceImageService(),
            filePicker: new FixedLocalSourceFilePicker("new-source.png"),
            rasterImageMetadataReader: new FixedRasterImageMetadataReader(new RasterImageInfo(1600, 1200)));
        viewModel.StartAddTemplateCommand.Execute(null);
        var color = viewModel.TemplateColorChoices.Single().Value;
        var size = viewModel.TemplateAdditionalOptionChoices.Single().Value;
        var mapping = new MockupImageSpaceMapping(1600, 1200, 100, 120, 900, 700);
        var exemplar = new LocalMockupSourceDraftViewModel("managed-exemplar.png", [color.Id, size.Id], isManaged: true, mapping, 1600, 1200, Guid.NewGuid());
        viewModel.LocalSourceDrafts.Add(exemplar);
        viewModel.SelectedCoverageExemplar = exemplar;
        var requirement = new MockupTemplateCoverageRequirement(
            "black",
            MockupTemplateCoverageStatus.Missing,
            [area.VariantIds.Single()],
            ["Black / S"],
            [new MockupTemplateCoverageOptionValue(color.Id, OptionKind.Color, "Black")],
            [],
            "Missing");
        viewModel.SelectCoverageRequirementCommand.Execute(requirement);
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.BrowseLocalSourceCommand);
        command.Execute(null);
        await command.ExecutionTask!;

        var uploaded = Assert.Single(viewModel.LocalSourceDrafts, value => value.Path == "new-source.png");
        Assert.Equal([color.Id, size.Id], uploaded.OptionValueIds);
        Assert.Equal(1600, uploaded.ImageWidth);
        Assert.Equal(1200, uploaded.ImageHeight);
        Assert.Equal("Mapping reused from exemplar", uploaded.AssistanceStatus);
        Assert.Equal(mapping, uploaded.Mapping);
    }

    [Fact]
    public async Task CoverageExemplarSurfacesMappingReviewWhenDimensionsDiffer()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            sourceImages: new RecordingSourceImageService(),
            filePicker: new FixedLocalSourceFilePicker("new-source.png"),
            rasterImageMetadataReader: new FixedRasterImageMetadataReader(new RasterImageInfo(800, 600)));
        viewModel.StartAddTemplateCommand.Execute(null);
        var color = viewModel.TemplateColorChoices.Single().Value;
        var size = viewModel.TemplateAdditionalOptionChoices.Single().Value;
        var exemplar = new LocalMockupSourceDraftViewModel(
            "managed-exemplar.png",
            [color.Id, size.Id],
            isManaged: true,
            new MockupImageSpaceMapping(1600, 1200, 100, 120, 900, 700),
            1600,
            1200,
            Guid.NewGuid());
        viewModel.LocalSourceDrafts.Add(exemplar);
        viewModel.SelectedCoverageExemplar = exemplar;
        viewModel.SelectCoverageRequirementCommand.Execute(new MockupTemplateCoverageRequirement(
            "black",
            MockupTemplateCoverageStatus.Missing,
            [area.VariantIds.Single()],
            ["Black / S"],
            [new MockupTemplateCoverageOptionValue(color.Id, OptionKind.Color, "Black")],
            [],
            "Missing"));
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.BrowseLocalSourceCommand);
        command.Execute(null);
        await command.ExecutionTask!;

        var uploaded = Assert.Single(viewModel.LocalSourceDrafts, value => value.Path == "new-source.png");
        Assert.Equal([color.Id, size.Id], uploaded.OptionValueIds);
        Assert.Null(uploaded.Mapping);
        Assert.Equal("Needs mapping review", uploaded.AssistanceStatus);
        Assert.Equal("Mapping reuse is offered only for matching 1600 × 1200 pixel images.", viewModel.ExemplarMappingSummary);
    }

    [Fact]
    public async Task BrowseLocalSourcesRetainsFailedMetadataDraftAlongsideValidDrafts()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var metadata = new SelectiveRasterImageMetadataReader("broken.png");
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            sourceImages: new RecordingSourceImageService(),
            filePicker: new FixedLocalSourceFilesPicker(["valid.png", "broken.png"]),
            rasterImageMetadataReader: metadata);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.StartAddTemplateCommand.Execute(null);

        var command = Assert.IsType<AsyncRelayCommand>(viewModel.BrowseLocalSourceCommand);
        command.Execute(null);
        await command.ExecutionTask!;

        var valid = Assert.Single(viewModel.LocalSourceDrafts, value => value.Path == "valid.png");
        var failed = Assert.Single(viewModel.LocalSourceDrafts, value => value.Path == "broken.png");
        Assert.Equal(1600, valid.ImageWidth);
        Assert.Equal(1200, valid.ImageHeight);
        Assert.True(failed.HasPreviewReadError);
        Assert.Contains("Unsupported mockup image format.", failed.PreviewReadError, StringComparison.Ordinal);
        Assert.Same(valid, viewModel.SelectedLocalSource);
    }

    [Fact]
    public async Task BrowseLocalSourceReadFailureRetainsFallbackAndExposesDiagnostic()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var metadata = new FailingRasterImageMetadataReader(new InvalidDataException("Unsupported mockup image format."));
        var sourceImages = new RecordingSourceImageService();
        const string selectedPath = "unsupported-mockup.png";
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            sourceImages: sourceImages,
            filePicker: new FixedLocalSourceFilePicker(selectedPath),
            rasterImageMetadataReader: metadata);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.StartAddTemplateCommand.Execute(null);

        viewModel.BrowseLocalSourceCommand.Execute(null);

        var draft = Assert.Single(viewModel.LocalSourceDrafts);
        Assert.Equal(selectedPath, Assert.Single(metadata.ReadPaths));
        Assert.Equal(0, draft.ImageWidth);
        Assert.Equal(0, draft.ImageHeight);
        Assert.Contains("Unsupported mockup image format.", draft.PreviewReadError ?? string.Empty, StringComparison.Ordinal);
        Assert.False(draft.HasPreviewDimensions);
        Assert.True(draft.HasPreviewReadError);
        Assert.Equal("Needs setup", draft.StatusLabel);
    }

    [Fact]
    public async Task SavingAfterArchivingLastLocalSourceSubmitsArchiveUpdate()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var sourceImages = new RecordingSourceImageService();
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository), sourceImages: sourceImages);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Manual front";
        var draft = new LocalMockupSourceDraftViewModel("C:\\source.png", [], isManaged: true, sourceImageId: sourceImages.SourceImageId);
        viewModel.LocalSourceDrafts.Add(draft);
        viewModel.SelectLocalSourceCommand.Execute(draft);
        viewModel.RemoveLocalSourceCommand.Execute(draft);

        Assert.Empty(viewModel.LocalSourceDrafts);
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));
        viewModel.CreateTemplateCommand.Execute(null);
        for (var attempt = 0; attempt < 100 && viewModel.IsBusy; attempt++) await Task.Delay(1, TestContext.Current.CancellationToken);

        var archive = Assert.Single(sourceImages.Updates);
        Assert.Equal(sourceImages.SourceImageId, archive.SourceImageId);
        Assert.True(archive.Archive);
        Assert.False(viewModel.IsAddingTemplate);
    }

    [Fact]
    public async Task SavingLocalSourcesSummarizesPartialCompletionWhenLaterSourceFails()
    {
        var sourceImages = new PartialSaveMockupTemplateSourceImageService();
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            sourceImages: sourceImages);
        sourceImages.OfferingId = offering.Id;
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Manual front";
        var first = new LocalMockupSourceDraftViewModel("first.png", []);
        var second = new LocalMockupSourceDraftViewModel("second.png", []);
        viewModel.LocalSourceDrafts.Add(first);
        viewModel.LocalSourceDrafts.Add(second);
        viewModel.SelectLocalSourceCommand.Execute(first);

        var command = Assert.IsType<AsyncRelayCommand>(viewModel.CreateTemplateCommand);
        command.Execute(null);
        await command.ExecutionTask!;

        Assert.Equal(2, sourceImages.AddCallCount);
        Assert.Contains("save partially completed", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 of 2 source image changes were saved", viewModel.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("second source image failed", viewModel.ErrorMessage, StringComparison.Ordinal);
        Assert.True(viewModel.IsAddingTemplate);
        Assert.False(viewModel.IsBusy);
    }
    [Fact]
    public async Task LoadsNormalizedSelectionsAndEnablesTypedSetupCommands()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Choice", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, now, now);
        var option = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var color = new OfferingOptionValue(Guid.NewGuid(), option.Id, offering.Id, "Black", 0);
        var placeholder = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 1400, [], false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [option],
            OfferingOptionValues = [color],
            OfferingPlaceholders = [placeholder]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.SelectedOption = option;
        viewModel.SelectedPlaceholder = placeholder;
        viewModel.StartAddOptionCommand.Execute(null);
        viewModel.OptionName = "Size";
        viewModel.StartAddOptionValueCommand.Execute(null);
        viewModel.OptionValue = "M";
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Front mockup";

        Assert.True(viewModel.IsAvailable);
        Assert.True(viewModel.CanEdit);
        Assert.Contains(OptionKind.Color, viewModel.OptionKinds);
        Assert.True(viewModel.CreateOptionCommand.CanExecute(null));
        Assert.True(viewModel.CreateOptionValueCommand.CanExecute(null));
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));
        Assert.True(viewModel.AddTemplateColorCommand.CanExecute(null) == false);
    }

    [Fact]
    public async Task ProviderPickerShowsOneCanonicalEntryAndKeepsOfferingSelectionAfterNormalization()
    {
        var now = DateTimeOffset.UtcNow;
        var store = SampleWorkspace.Create().Stores.Single();
        var snapshot = WorkspaceSnapshot.Empty with
        {
            Workspaces = [WorkspaceSnapshot.DefaultWorkspace(now)],
            Stores = [store]
        };
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var survivor = new PrintProvider(Guid.NewGuid(), store.Id, "SwiftPOD", "9", false, now.AddMinutes(-2), now.AddMinutes(-2));
        var duplicate = new PrintProvider(Guid.NewGuid(), store.Id, " swiftpod ", "23", false, now.AddMinutes(-1), now.AddMinutes(-1));
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Tee", null, BlueprintOfferingKind.FixedPrintProvider, duplicate.Id, null, null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            PrintProviders = [survivor, duplicate],
            BlueprintOfferings = [offering]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);

        Assert.Single(viewModel.AvailablePrintProviders);
        Assert.Equal(survivor.Id, viewModel.SelectedPrintProvider?.Id);
        Assert.Equal(survivor.Id, viewModel.SelectedOffering?.PrintProviderId);
    }

    [Fact]
    public async Task SelectedDesignAreaDefaultsAspectRatioLockAndSynchronizesNumericDimensions()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Choice", null, BlueprintOfferingKind.ProviderNetwork, null, "choice", null, null, false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 1200, 600, [], false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingPlaceholders = [area] });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        viewModel.SelectedPlaceholder = area;

        Assert.Equal(2, viewModel.PlacementAspectRatio, 3);
        Assert.True(viewModel.KeepAspectRatio);
        viewModel.MappingWidthText = "401";
        Assert.Equal("200", viewModel.MappingHeightText);

        viewModel.KeepAspectRatio = false;
        viewModel.MappingHeightText = "333";
        Assert.Equal("401", viewModel.MappingWidthText);
    }

    [Fact]
    public async Task RequestedOfferingIdentityIsAuthoritativeAndNeverFallsBack()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var first = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "First", null, BlueprintOfferingKind.ProviderNetwork, null, "first-network", null, null, false, now, now);
        var requested = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Requested", null, BlueprintOfferingKind.ProviderNetwork, null, "requested-network", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [first, requested]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));

        viewModel.SelectOffering(requested.Id);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);

        Assert.Equal(requested.Id, viewModel.SelectedOfferingId);
        Assert.True(viewModel.HasSelectedOffering);
        Assert.False(viewModel.IsOfferingContextUnavailable);

        viewModel.SelectOffering(Guid.NewGuid());
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);

        Assert.Null(viewModel.SelectedOffering);
        Assert.False(viewModel.HasSelectedOffering);
        Assert.True(viewModel.IsOfferingContextUnavailable);
    }

    [Fact]
    public async Task FocusedTemplateDraftCanSaveWithoutProviderAndValidatesSelectedMapping()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "SwiftPOD", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 4500, 5400, [variant.Id], false, now, now);
        var populated = snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium],
            OfferingVariants = [variant],
            OfferingPlaceholders = [area]
        };
        var repository = new InMemoryWorkspaceRepository(populated);
        var context = new OfferingContext(store.Id, blueprint.Id, offering.Id);
        var source = new StubProviderCatalog(new ProviderCatalogCandidateDescriptor(context, true, null,
            new HashSet<ProviderCatalogCombination> { new(black.Id, medium.Id) },
            [new ProviderMockupCandidateDescriptor("front-black", "Front — Black", 1000, 1200, new HashSet<Guid> { black.Id })]));
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository), new MockupTemplateSetupService(repository),
            new OfferingManagementService(repository, source), source);

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectedPlaceholder = area;
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Front mockup";
        Assert.Single(viewModel.TemplateColorChoices).IsSelected = true;

        Assert.True(viewModel.HasProviderMockupCandidates);
        Assert.Equal(ProviderCatalogLoadState.Available, viewModel.ProviderCatalogState);
        Assert.Contains("optional", viewModel.ProviderImageSelectionStateMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("save a Draft", viewModel.ProviderImageSelectionInstructions, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1000, viewModel.MappingImageWidth);
        Assert.Equal(1200, viewModel.MappingImageHeight);
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));

        viewModel.MappingWidth = 2000;
        Assert.False(viewModel.CreateTemplateCommand.CanExecute(null));
    }

    [Fact]
    public async Task ProviderImageSelection_ClassifiesEmptyUnavailableAndErrorWithRecovery()
    {
        var empty = CreateProviderCatalogStateViewModel(new StubProviderCatalog(new ProviderCatalogCandidateDescriptor(
            new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), true, null, new HashSet<ProviderCatalogCombination>(), [])));
        await empty.ViewModel.LoadForStoreAsync(empty.StoreId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderCatalogLoadState.Empty, empty.ViewModel.ProviderCatalogState);
        Assert.Contains("no mockup images", empty.ViewModel.ProviderImageSelectionStateMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("save", empty.ViewModel.ProviderImageSelectionRecoveryMessage, StringComparison.OrdinalIgnoreCase);

        var unavailable = CreateProviderCatalogStateViewModel(new StubProviderCatalog(new ProviderCatalogCandidateDescriptor(
            new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), false, "Provider connection is not configured.", new HashSet<ProviderCatalogCombination>(), [])));
        await unavailable.ViewModel.LoadForStoreAsync(unavailable.StoreId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderCatalogLoadState.Unavailable, unavailable.ViewModel.ProviderCatalogState);
        Assert.Contains("Draft saving remains available", unavailable.ViewModel.ProviderImageSelectionStateMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("optional", unavailable.ViewModel.ProviderImageSelectionRecoveryMessage, StringComparison.OrdinalIgnoreCase);

        var failed = CreateProviderCatalogStateViewModel(new ThrowingProviderCatalog());
        await failed.ViewModel.LoadForStoreAsync(failed.StoreId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderCatalogLoadState.Error, failed.ViewModel.ProviderCatalogState);
        Assert.Contains("could not be loaded", failed.ViewModel.ProviderImageSelectionStateMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("retry", failed.ViewModel.ProviderImageSelectionRecoveryMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(failed.ViewModel.ProviderMockupCandidates);
    }

    [Fact]
    public async Task ProviderImageSelection_ExposesLoadingBeforePendingSourceCompletes()
    {
        var source = new PendingProviderCatalog();
        var setup = CreateProviderCatalogStateViewModel(source);

        var load = setup.ViewModel.LoadForStoreAsync(setup.StoreId, TestContext.Current.CancellationToken);
        await source.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderCatalogLoadState.Loading, setup.ViewModel.ProviderCatalogState);
        Assert.Contains("Loading", setup.ViewModel.ProviderImageSelectionStateMessage, StringComparison.Ordinal);
        Assert.Contains("provider catalog", setup.ViewModel.ProviderImageSelectionInstructions, StringComparison.OrdinalIgnoreCase);

        source.Complete(new ProviderCatalogCandidateDescriptor(
            new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), true, null, new HashSet<ProviderCatalogCombination>(), []));
        await load;
        Assert.Equal(ProviderCatalogLoadState.Empty, setup.ViewModel.ProviderCatalogState);
    }

    [Fact]
    public async Task NameOnlyTemplateSavesAsDraftWithoutDesignAreaOrProvider()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Manual tee", null, BlueprintOfferingKind.ProviderNetwork, null, "manual", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);

        Assert.True(viewModel.StartAddTemplateCommand.CanExecute(null));
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Manual front";

        Assert.Equal("Draft", viewModel.MockupTemplateLifecycleLabel);
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));
        viewModel.CreateTemplateCommand.Execute(null);
        for (var attempt = 0; attempt < 10 && viewModel.IsAddingTemplate; attempt++) await Task.Yield();

        var saved = Assert.Single((await repository.LoadAsync(TestContext.Current.CancellationToken)).MockupTemplates);
        Assert.Equal("Manual front", saved.Name);
        Assert.Null(saved.TargetPlaceholderId);
        Assert.False(viewModel.IsAddingTemplate);
    }

    [Fact]
    public async Task SelectedProviderMappingRejectsPartialAndFractionalText()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Provider tee", null, BlueprintOfferingKind.ProviderNetwork, null, "provider", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with { Blueprints = [blueprint], BlueprintOfferings = [offering] });
        var context = new OfferingContext(store.Id, blueprint.Id, offering.Id);
        var source = new StubProviderCatalog(new ProviderCatalogCandidateDescriptor(context, true, null, new HashSet<ProviderCatalogCombination>(),
            [new ProviderMockupCandidateDescriptor("front", "Front", 1000, 1000, new HashSet<Guid>())]));
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository), new OfferingManagementService(repository, source), source);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.TemplateName = "Front";

        viewModel.MappingWidthText = "";
        Assert.False(viewModel.CreateTemplateCommand.CanExecute(null));
        viewModel.MappingWidthText = "400.5";
        Assert.False(viewModel.CreateTemplateCommand.CanExecute(null));
        Assert.Contains("whole-number", viewModel.MockupTemplateSaveValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaximumMockupLongEdgeRequiresPositiveWholeNumber()
    {
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(new InMemoryWorkspaceRepository(SampleWorkspace.Create())),
            new MockupTemplateSetupService(new InMemoryWorkspaceRepository(SampleWorkspace.Create())));

        Assert.Equal("2000", viewModel.MaximumMockupLongEdgeText);
        Assert.False(viewModel.HasMaximumMockupLongEdgeValidationMessage);

        viewModel.MaximumMockupLongEdgeText = "1200.5";

        Assert.True(viewModel.HasMaximumMockupLongEdgeValidationMessage);
        Assert.Contains("positive whole number", viewModel.MaximumMockupLongEdgeValidationMessage, StringComparison.OrdinalIgnoreCase);

        viewModel.MaximumMockupLongEdgeText = "1200";

        Assert.False(viewModel.HasMaximumMockupLongEdgeValidationMessage);
    }

    [Fact]
    public void DesignAreaPhysicalSizeIsUnavailableWithoutDpiAndDerivedWhenProvided()
    {
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(new InMemoryWorkspaceRepository(SampleWorkspace.Create())),
            new MockupTemplateSetupService(new InMemoryWorkspaceRepository(SampleWorkspace.Create())));
        viewModel.PlaceholderWidth = "4500";
        viewModel.PlaceholderHeight = "5400";

        Assert.Contains("unavailable", viewModel.PhysicalSizeSummary, StringComparison.OrdinalIgnoreCase);

        viewModel.ArtworkDpi = "300";
        Assert.Contains("15", viewModel.PhysicalSizeSummary, StringComparison.Ordinal);
        Assert.Contains("mm", viewModel.PhysicalSizeSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VariantEditorsAreOnDemandAndMutuallyExclusive()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "SwiftPOD", null, BlueprintOfferingKind.ProviderNetwork, null, "printify-choice", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Shade", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Dimensions", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium],
            OfferingVariants = [variant]
        });
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            new OfferingManagementService(repository));

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);

        Assert.False(viewModel.IsManagingOptionValues);
        Assert.Equal("Black", Assert.Single(viewModel.SellableVariantRows).Color);

        viewModel.ManageOptionCommand.Execute(colorOption);
        Assert.True(viewModel.IsManagingOptionValues);
        Assert.Equal(colorOption.Id, viewModel.SelectedOptionId);
        viewModel.CloseOptionValueManagementCommand.Execute(null);
        Assert.False(viewModel.IsManagingOptionValues);

        viewModel.StartAddVariantCommand.Execute(null);
        Assert.True(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);

        viewModel.StartBulkVariantsCommand.Execute(null);
        Assert.True(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);

        viewModel.CancelAddVariantCommand.Execute(null);
        viewModel.StartBulkVariantsCommand.Execute(null);
        Assert.False(viewModel.IsAddingVariant);
        Assert.True(viewModel.IsAddingBulkVariants);

        viewModel.CancelBulkVariantsCommand.Execute(null);
        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.False(viewModel.HasActiveDraft);
    }

    [Fact]
    public async Task ArchiveVariantCommandArchivesTheSellableVariantRow()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "SwiftPOD", null, BlueprintOfferingKind.ProviderNetwork, null, "swiftpod", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium],
            OfferingVariants = [variant]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));

        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        var row = Assert.Single(viewModel.SellableVariantRows);

        viewModel.ArchiveVariantCommand.Execute(row);
        for (var attempt = 0; attempt < 20 && viewModel.IsBusy; attempt++)
            await Task.Yield();

        Assert.False(viewModel.HasError, viewModel.ErrorMessage);
        Assert.Empty(viewModel.SellableVariantRows);
        var saved = (await repository.LoadAsync(TestContext.Current.CancellationToken)).OfferingVariants.Single(candidate => candidate.Id == variant.Id);
        Assert.True(saved.IsArchived);
    }

    [Fact]
    public async Task ArchiveVariantCommandReportsWhenTheSellableVariantRowIsStale()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        var staleRow = new SellableVariantRowViewModel(Guid.NewGuid(), "Stale", "Black", "M", null, false);

        viewModel.ArchiveVariantCommand.Execute(staleRow);
        for (var attempt = 0; attempt < 20 && viewModel.IsBusy; attempt++)
            await Task.Yield();

        Assert.True(viewModel.HasError);
        Assert.Contains("no longer active", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(viewModel.SellableVariantRows);
    }

    [Fact]
    public async Task StartAddVariantRaisesRequestEvent()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        var requested = 0;
        viewModel.AddVariantRequested += (_, _) => requested++;

        viewModel.StartAddVariantCommand.Execute(null);

        Assert.Equal(1, requested);
        Assert.True(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);
    }

    [Fact]
    public async Task StartBulkVariantsRaisesRequestEvent()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        var requested = 0;
        viewModel.BulkVariantsRequested += (_, _) => requested++;

        viewModel.StartBulkVariantsCommand.Execute(null);

        Assert.Equal(1, requested);
        Assert.True(viewModel.IsAddingBulkVariants);
        Assert.False(viewModel.IsAddingVariant);
    }

    [Fact]
    public async Task SecondVariantCreationRequestKeepsOriginalDialogMode()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        var addRequested = 0;
        var bulkRequested = 0;
        viewModel.AddVariantRequested += (_, _) => addRequested++;
        viewModel.BulkVariantsRequested += (_, _) => bulkRequested++;

        viewModel.StartAddVariantCommand.Execute(null);
        viewModel.StartBulkVariantsCommand.Execute(null);

        Assert.Equal(1, addRequested);
        Assert.Equal(0, bulkRequested);
        Assert.True(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);
    }

    [Fact]
    public async Task WorkspaceLoadClosesVariantCreationAndDiscardsDraft()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        viewModel.StartAddVariantCommand.Execute(null);
        viewModel.VariantName = "Draft";

        await viewModel.LoadForStoreAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.Equal(string.Empty, viewModel.VariantName);
    }

    [Fact]
    public async Task CancelActiveDraftsClosesBulkCreationSession()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        viewModel.StartBulkVariantsCommand.Execute(null);
        viewModel.BulkColor = viewModel.AvailableColors.First();

        viewModel.CancelActiveDrafts();

        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.Null(viewModel.BulkColor);
    }

    [Fact]
    public async Task SuccessfulVariantCreationClosesSessionAndRefreshesList()
    {
        var (viewModel, _, _, _) = await CreateCatalogWithOptionsAsync();
        viewModel.StartAddVariantCommand.Execute(null);
        Assert.True(viewModel.IsAddingVariant);
        viewModel.VariantValueChoices.Single(v => v.Value.Value == "Black").IsSelected = true;

        viewModel.CreateVariantCommand.Execute(null);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.IsAddingVariant);
        Assert.Equal(1, viewModel.AvailableVariantCount);
    }

    [Fact]
    public async Task OfferingSwitchClosesVariantCreationAndDiscardsDrafts()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var first = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "First", null, BlueprintOfferingKind.ProviderNetwork, null, "first", null, null, false, now, now);
        var second = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Second", null, BlueprintOfferingKind.ProviderNetwork, null, "second", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), first.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), first.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, first.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, first.Id, "M", 0);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [first, second],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository), new OfferingManagementService(repository));
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(first.Id);

        viewModel.StartAddVariantCommand.Execute(null);
        viewModel.VariantName = "Draft";
        Assert.True(viewModel.IsAddingVariant);

        viewModel.SelectOffering(second.Id);

        Assert.False(viewModel.IsAddingVariant);
        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.Equal(string.Empty, viewModel.VariantName);

        viewModel.StartBulkVariantsCommand.Execute(null);
        viewModel.BulkColor = viewModel.AvailableColors.FirstOrDefault();
        Assert.True(viewModel.IsAddingBulkVariants);

        viewModel.SelectOffering(first.Id);

        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.False(viewModel.IsAddingVariant);
    }

    [Fact]
    public async Task ManageOptionValuesDialogTitleReflectsSelectedOptionName()
    {
        var (viewModel, colorOption, sizeOption, _) = await CreateCatalogWithOptionsAsync();
        Assert.Equal(colorOption.Id, viewModel.SelectedOption?.Id);
        Assert.Equal("Manage Color values", viewModel.ManageOptionValuesDialogTitle);

        viewModel.ManageOptionCommand.Execute(sizeOption);

        Assert.True(viewModel.IsManagingOptionValues);
        Assert.Equal("Manage Size values", viewModel.ManageOptionValuesDialogTitle);
    }

    [Fact]
    public async Task ManageOptionCommandRequestsDialogAndCloseDiscardsDraft()
    {
        var (viewModel, colorOption, _, _) = await CreateCatalogWithOptionsAsync();
        var requested = 0;
        viewModel.OptionValueManagementRequested += (_, _) => requested++;

        viewModel.ManageOptionCommand.Execute(colorOption);
        Assert.Equal(1, requested);
        Assert.True(viewModel.IsManagingOptionValues);

        viewModel.StartAddOptionValueCommand.Execute(null);
        viewModel.OptionValue = "Navy";
        Assert.True(viewModel.IsAddingOptionValue);

        viewModel.CloseOptionValueManagementCommand.Execute(null);

        Assert.False(viewModel.IsManagingOptionValues);
        Assert.False(viewModel.IsAddingOptionValue);
        Assert.Equal(string.Empty, viewModel.OptionValue);
    }

    [Fact]
    public async Task EditingOptionValueRefreshesSaveCommandCanExecuteState()
    {
        var (viewModel, colorOption, _, _) = await CreateCatalogWithOptionsAsync();
        viewModel.ManageOptionCommand.Execute(colorOption);
        var value = Assert.Single(viewModel.AvailableValues);
        viewModel.EditOptionValueCommand.Execute(value);

        Assert.True(viewModel.IsEditingOptionValue);
        Assert.True(viewModel.SaveOptionValueEditCommand.CanExecute(null));
    }

    [Fact]
    public async Task SecondManageRequestKeepsOriginalStableOptionScope()
    {
        var (viewModel, colorOption, sizeOption, _) = await CreateCatalogWithOptionsAsync();
        var requested = 0;
        viewModel.OptionValueManagementRequested += (_, _) => requested++;

        viewModel.ManageOptionCommand.Execute(colorOption);
        viewModel.ManageOptionCommand.Execute(sizeOption);

        Assert.Equal(1, requested);
        Assert.Equal(colorOption.Id, viewModel.SelectedOptionId);
        Assert.Equal("Manage Color values", viewModel.ManageOptionValuesDialogTitle);
    }

    [Fact]
    public async Task ManageRequestRejectsOptionOutsideCurrentOffering()
    {
        var (viewModel, colorOption, _, offering) = await CreateCatalogWithOptionsAsync();
        var staleOption = colorOption with { Id = Guid.NewGuid(), OfferingId = Guid.NewGuid() };
        var requested = 0;
        viewModel.OptionValueManagementRequested += (_, _) => requested++;

        viewModel.ManageOptionCommand.Execute(staleOption);

        Assert.Equal(0, requested);
        Assert.False(viewModel.IsManagingOptionValues);
        Assert.Equal(offering.Id, viewModel.SelectedOfferingId);
        Assert.Equal(colorOption.Id, viewModel.SelectedOptionId);
    }

    [Fact]
    public async Task WorkspaceLoadClosesOptionValueManagementAndDiscardsDraft()
    {
        var (viewModel, colorOption, _, _) = await CreateCatalogWithOptionsAsync();
        viewModel.ManageOptionCommand.Execute(colorOption);
        viewModel.StartAddOptionValueCommand.Execute(null);
        viewModel.OptionValue = "Navy";

        await viewModel.LoadForStoreAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(viewModel.IsManagingOptionValues);
        Assert.False(viewModel.IsAddingOptionValue);
        Assert.Equal(string.Empty, viewModel.OptionValue);
    }

    [Fact]
    public async Task OfferingSwitchClosesOptionValueManagementAndDiscardsDraft()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var first = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "First", null, BlueprintOfferingKind.ProviderNetwork, null, "first", null, null, false, now, now);
        var second = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Second", null, BlueprintOfferingKind.ProviderNetwork, null, "second", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), first.Id, OptionKind.Color, "Color", 0);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [first, second],
            OfferingOptions = [colorOption]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(first.Id);
        viewModel.ManageOptionCommand.Execute(colorOption);
        Assert.True(viewModel.IsManagingOptionValues);
        viewModel.StartAddOptionValueCommand.Execute(null);
        viewModel.OptionValue = "Navy";
        Assert.True(viewModel.IsAddingOptionValue);

        viewModel.SelectOffering(second.Id);

        Assert.False(viewModel.IsManagingOptionValues);
        Assert.False(viewModel.IsAddingOptionValue);
        Assert.Equal(string.Empty, viewModel.OptionValue);
    }

    private static async Task<(CatalogSetupViewModel ViewModel, OfferingOption ColorOption, OfferingOption SizeOption, BlueprintOffering Offering)> CreateCatalogWithOptionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Printful tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printful", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black]
        });
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository), new OfferingManagementService(repository));
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        return (viewModel, colorOption, sizeOption, offering);
    }

    [Fact]
    public async Task RequestDesignAreaArchive_OpensConfirmationWithoutMutatingData()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        var card = Assert.Single(viewModel.DesignAreaCards);

        viewModel.ArchivePlaceholderCommand.Execute(card);

        Assert.True(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Equal(area.Id, viewModel.PendingDesignAreaArchiveId);
        Assert.Equal("Front", viewModel.PendingDesignAreaArchiveName);
        Assert.Contains("Front", viewModel.DesignAreaArchiveConfirmationMessage, StringComparison.Ordinal);
        Assert.Single(viewModel.DesignAreaCards);
        Assert.False(viewModel.HasError);
        Assert.True(viewModel.ConfirmDesignAreaArchiveCommand.CanExecute(null));
        Assert.True(viewModel.CancelDesignAreaArchiveCommand.CanExecute(null));
    }

    [Fact]
    public async Task DesignAreaDraft_AddAndEditModesTrackMeaningfulChangesAndDiscardChoices()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        var requests = 0;
        viewModel.DesignAreaEditorRequested += (_, _) => requests++;

        viewModel.StartAddPlaceholderCommand.Execute(null);

        Assert.Equal(1, requests);
        Assert.True(viewModel.IsAddingPlaceholder);
        Assert.False(viewModel.IsEditingDesignArea);
        Assert.Equal("Add Design Area", viewModel.DesignAreaEditorDialogTitle);
        Assert.False(viewModel.HasMeaningfulDesignAreaDraft);

        viewModel.PlaceholderName = "Sleeve";
        Assert.True(viewModel.HasMeaningfulDesignAreaDraft);
        viewModel.RequestCancelDesignAreaCommand.Execute(null);
        Assert.True(viewModel.IsDesignAreaDiscardConfirmationVisible);
        Assert.True(viewModel.IsAddingPlaceholder);

        viewModel.KeepEditingDesignAreaCommand.Execute(null);
        Assert.False(viewModel.IsDesignAreaDiscardConfirmationVisible);
        Assert.Equal("Sleeve", viewModel.PlaceholderName);

        viewModel.RequestCancelDesignAreaCommand.Execute(null);
        viewModel.ConfirmDiscardDesignAreaCommand.Execute(null);
        Assert.False(viewModel.IsAddingPlaceholder);
        Assert.False(viewModel.HasMeaningfulDesignAreaDraft);

        viewModel.EditPlaceholderCommand.Execute(Assert.Single(viewModel.DesignAreaCards));

        Assert.Equal(2, requests);
        Assert.True(viewModel.IsEditingDesignArea);
        Assert.Equal("Edit Design Area", viewModel.DesignAreaEditorDialogTitle);
        Assert.Equal(area.Id, viewModel.SelectedPlaceholderId);
        Assert.Equal(area.Name, viewModel.PlaceholderName);
        Assert.False(viewModel.HasMeaningfulDesignAreaDraft);

        Assert.Single(viewModel.PlaceholderVariantChoices).IsSelected = false;
        Assert.True(viewModel.HasMeaningfulDesignAreaDraft);
    }

    [Fact]
    public async Task DesignAreaDraft_InvalidSaveStaysOpenAndOfferingSwitchEndsStaleDraft()
    {
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.EditPlaceholderCommand.Execute(Assert.Single(viewModel.DesignAreaCards));
        viewModel.PlaceholderWidth = "0";

        Assert.False(viewModel.CreatePlaceholderCommand.CanExecute(null));
        viewModel.CreatePlaceholderCommand.Execute(null);
        Assert.True(viewModel.IsAddingPlaceholder);
        Assert.Equal("0", viewModel.PlaceholderWidth);
        Assert.True(viewModel.HasMeaningfulDesignAreaDraft);

        var otherOffering = viewModel.Offerings.First(candidate => candidate.Id != offering.Id);
        viewModel.SelectOffering(otherOffering.Id);

        Assert.False(viewModel.IsAddingPlaceholder);
        Assert.False(viewModel.IsDesignAreaDiscardConfirmationVisible);
        Assert.False(viewModel.HasMeaningfulDesignAreaDraft);
        Assert.Equal(otherOffering.Id, viewModel.SelectedOfferingId);
    }

    [Fact]
    public async Task CancelDesignAreaArchive_HidesConfirmationAndPreservesData()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.ArchivePlaceholderCommand.Execute(Assert.Single(viewModel.DesignAreaCards));

        viewModel.CancelDesignAreaArchiveCommand.Execute(null);

        Assert.False(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Null(viewModel.PendingDesignAreaArchiveId);
        Assert.Single(viewModel.DesignAreaCards);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.ConfirmDesignAreaArchiveCommand.CanExecute(null));
        Assert.False(viewModel.CancelDesignAreaArchiveCommand.CanExecute(null));
    }

    [Fact]
    public async Task ConfirmDesignAreaArchive_ArchivesUnreferencedAreaOnceAndCloses()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        viewModel.ArchivePlaceholderCommand.Execute(Assert.Single(viewModel.DesignAreaCards));

        viewModel.ConfirmDesignAreaArchiveCommand.Execute(null);

        Assert.False(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Empty(viewModel.DesignAreaCards);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.ConfirmDesignAreaArchiveCommand.CanExecute(null));

        viewModel.ConfirmDesignAreaArchiveCommand.Execute(null);
        Assert.Empty(viewModel.DesignAreaCards);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ConfirmDesignAreaArchive_BlockedReferencedAreaDisplaysRecoverableGuidance()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true);
        viewModel.ArchivePlaceholderCommand.Execute(Assert.Single(viewModel.DesignAreaCards));

        viewModel.ConfirmDesignAreaArchiveCommand.Execute(null);

        Assert.False(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.True(viewModel.HasError);
        Assert.Contains("referenced", viewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(viewModel.DesignAreaCards);
    }

    [Fact]
    public async Task RepeatedArchiveRequestKeepsOriginalTargetAndSingleConfirmation()
    {
        var (viewModel, area, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        var card = Assert.Single(viewModel.DesignAreaCards);
        viewModel.ArchivePlaceholderCommand.Execute(card);

        viewModel.ArchivePlaceholderCommand.Execute(card);

        Assert.True(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Equal(area.Id, viewModel.PendingDesignAreaArchiveId);
        Assert.Equal("Front", viewModel.PendingDesignAreaArchiveName);
        Assert.Single(viewModel.DesignAreaCards);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task OfferingSwitch_CancelsPendingDesignAreaArchiveAndRejectsStaleCard()
    {
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        var staleCard = Assert.Single(viewModel.DesignAreaCards);
        viewModel.ArchivePlaceholderCommand.Execute(staleCard);

        var otherOffering = viewModel.Offerings.First(candidate => candidate.Id != offering.Id);
        viewModel.SelectOffering(otherOffering.Id);

        Assert.False(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Null(viewModel.PendingDesignAreaArchiveId);

        viewModel.ArchivePlaceholderCommand.Execute(staleCard);

        Assert.False(viewModel.IsDesignAreaArchiveConfirmationVisible);
        Assert.Null(viewModel.PendingDesignAreaArchiveId);
    }

    [Fact]
    public async Task MockupTemplateDraft_AddModeTracksMeaningfulChangesAndDiscardChoices()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: false);
        var requests = 0;
        viewModel.MockupTemplateEditorRequested += (_, _) => requests++;

        viewModel.StartAddTemplateCommand.Execute(null);

        Assert.Equal(1, requests);
        Assert.True(viewModel.IsAddingTemplate);
        Assert.False(viewModel.IsEditingMockupTemplate);
        Assert.Equal("Add Mockup Template", viewModel.MockupTemplateEditorDialogTitle);
        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);

        viewModel.TemplateName = "Front navy";
        Assert.True(viewModel.HasMeaningfulMockupTemplateDraft);
        viewModel.RequestCancelMockupTemplateCommand.Execute(null);
        Assert.True(viewModel.IsMockupTemplateDiscardConfirmationVisible);
        Assert.True(viewModel.IsAddingTemplate);

        viewModel.KeepEditingMockupTemplateCommand.Execute(null);
        Assert.False(viewModel.IsMockupTemplateDiscardConfirmationVisible);
        Assert.Equal("Front navy", viewModel.TemplateName);

        viewModel.RequestCancelMockupTemplateCommand.Execute(null);
        viewModel.ConfirmDiscardMockupTemplateCommand.Execute(null);
        Assert.False(viewModel.IsAddingTemplate);
        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);
        Assert.Equal(string.Empty, viewModel.TemplateName);
    }

    [Fact]
    public async Task MockupMetadataAssistanceAppliesResultsToDraftWithoutSaving()
    {
        var assistance = new RecordingMockupMetadataAssistanceService();
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            assistance: assistance);
        viewModel.StartAddTemplateCommand.Execute(null);
        assistance.ColorId = viewModel.TemplateColorChoices.Single().Value.Id;
        assistance.SizeId = viewModel.TemplateAdditionalOptionChoices.Single().Value.Id;
        var draft = new LocalMockupSourceDraftViewModel("front-black.png", [], imageWidth: 100, imageHeight: 100);
        viewModel.LocalSourceDrafts.Add(draft);
        viewModel.SelectLocalSourceCommand.Execute(draft);

        Assert.True(viewModel.AssistMockupSourceMetadataCommand.CanExecute(null));
        viewModel.AssistMockupSourceMetadataCommand.Execute(null);
        var command = Assert.IsType<AsyncRelayCommand>(viewModel.AssistMockupSourceMetadataCommand);
        await command.ExecutionTask!.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(assistance.WasCalled);
        Assert.Equal([assistance.ColorId, assistance.SizeId], draft.OptionValueIds);
        Assert.Equal(new MockupImageSpaceMapping(100, 100, 5, 6, 70, 80), draft.Mapping);
        Assert.Equal("Complete", draft.StatusLabel);
        Assert.True(viewModel.HasMeaningfulMockupTemplateDraft);
    }

    [Fact]
    public async Task MockupTemplateDraft_EditModePreservesInvalidDraftAndOfferingSwitchEndsIt()
    {
        var (viewModel, area, offering) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true);
        var template = Assert.Single(viewModel.MockupTemplateCards);
        var requests = 0;
        viewModel.MockupTemplateEditorRequested += (_, _) => requests++;

        viewModel.EditTemplateCommand.Execute(template);

        Assert.Equal(1, requests);
        Assert.True(viewModel.IsEditingMockupTemplate);
        Assert.Equal("Edit Mockup Template", viewModel.MockupTemplateEditorDialogTitle);
        Assert.Equal(template.Id, viewModel.SelectedTemplateId);
        Assert.Equal(area.Id, viewModel.SelectedPlaceholderId);
        Assert.Equal("Front black", viewModel.TemplateName);
        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);

        viewModel.TemplateName = string.Empty;
        Assert.True(viewModel.HasMeaningfulMockupTemplateDraft);
        Assert.False(viewModel.CreateTemplateCommand.CanExecute(null));
        Assert.True(viewModel.IsAddingTemplate);
        Assert.Single(viewModel.MockupTemplateCards);

        var otherOffering = viewModel.Offerings.First(candidate => candidate.Id != offering.Id);
        viewModel.SelectOffering(otherOffering.Id);

        Assert.False(viewModel.IsAddingTemplate);
        Assert.False(viewModel.IsMockupTemplateDiscardConfirmationVisible);
        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);
        Assert.Equal(string.Empty, viewModel.TemplateName);
    }

    [Fact]
    public async Task MockupTemplateDraft_LoadedSourceImagesBecomeBaselineAndRevertingMappingIsClean()
    {
        var sourceImage = new MockupTemplateSourceImageSummary(
            Guid.NewGuid(), Guid.NewGuid(), "Front", "assets/front.png", new RasterImageInfo(1200, 1200),
            new MockupImageSpaceMapping(1200, 1200, 250, 200, 600, 700), []);
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: true,
            sourceImages: new FixedMockupTemplateSourceImageService(new([sourceImage], [], false)));

        viewModel.EditTemplateCommand.Execute(Assert.Single(viewModel.MockupTemplateCards));
        await Task.Yield();

        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);
        viewModel.MappingXText = "251";
        Assert.True(viewModel.HasMeaningfulMockupTemplateDraft);
        viewModel.MappingXText = "250";
        Assert.False(viewModel.HasMeaningfulMockupTemplateDraft);
        viewModel.RequestCancelMockupTemplateCommand.Execute(null);
        Assert.False(viewModel.IsMockupTemplateDiscardConfirmationVisible);
    }

    [Fact]
    public async Task MockupTemplateDraft_SourceLoadFailureIsObservedAndReported()
    {
        const string failureMessage = "local source catalog unavailable";
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: true,
            sourceImages: new FailingMockupTemplateSourceImageService(new IOException(failureMessage)));
        var error = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.ErrorMessage))
                error.TrySetResult(viewModel.ErrorMessage);
        };

        viewModel.EditTemplateCommand.Execute(Assert.Single(viewModel.MockupTemplateCards));

        Assert.Equal(failureMessage, await error.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        Assert.True(viewModel.IsAddingTemplate);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(MockupTemplateCoverageViewState.Error, viewModel.CoverageState);
        Assert.Equal(failureMessage, viewModel.CoverageError);
        Assert.Contains(failureMessage, viewModel.CoverageStateHelp);
    }

    [Fact]
    public async Task MockupTemplateDraft_NoTargetDesignAreaIsExplicitCoverageState()
    {
        var state = new MockupTemplateSourceState([], [], false, CoveragePlan: new(
            Guid.NewGuid(), null, MockupTemplateCoverageGroupingStrategy.ColorFirst, "context", [], [], false));
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: true,
            sourceImages: new FixedMockupTemplateSourceImageService(state));

        viewModel.EditTemplateCommand.Execute(Assert.Single(viewModel.MockupTemplateCards));
        await Task.Yield();

        Assert.Equal(MockupTemplateCoverageViewState.NoTargetDesignArea, viewModel.CoverageState);
        Assert.Equal("No target Design Area", viewModel.CoverageStateLabel);
        Assert.Contains("Choose an active target Design Area", viewModel.CoverageStateHelp);
        Assert.True(viewModel.HasCoveragePanel);
    }

    [Fact]
    public async Task MockupTemplateDraft_StaleCoveragePlanRequiresExplicitRefresh()
    {
        var state = new MockupTemplateSourceState([], [], false, CoveragePlan: new(
            Guid.NewGuid(), Guid.NewGuid(), MockupTemplateCoverageGroupingStrategy.ColorFirst, "stale-context",
            [new("black", MockupTemplateCoverageStatus.Missing, [Guid.NewGuid()], ["Black / S"], [], [], "Assign a source image.")], [], true));
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: true,
            sourceImages: new FixedMockupTemplateSourceImageService(state));

        viewModel.EditTemplateCommand.Execute(Assert.Single(viewModel.MockupTemplateCards));
        await Task.Yield();

        Assert.Equal(MockupTemplateCoverageViewState.Stale, viewModel.CoverageState);
        Assert.True(viewModel.IsCoveragePlanStale);
        Assert.Contains("Refresh", viewModel.CoverageStateHelp);
        Assert.False(viewModel.AddCoverageRequirementImageCommand.CanExecute(viewModel.CoverageRequirements.Single()));
    }

    [Fact]
    public async Task MockupTemplateDraft_ArchivedStoreCannotOpenAddOrEdit()
    {
        var (viewModel, _, _) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true, storeArchived: true);
        var card = Assert.Single(viewModel.MockupTemplateCards);
        var requests = 0;
        viewModel.MockupTemplateEditorRequested += (_, _) => requests++;

        Assert.True(viewModel.IsReadOnly);
        Assert.False(viewModel.CanEdit);
        Assert.False(viewModel.StartAddTemplateCommand.CanExecute(null));
        Assert.False(viewModel.EditTemplateCommand.CanExecute(card));

        viewModel.StartAddTemplateCommand.Execute(null);
        viewModel.EditTemplateCommand.Execute(card);

        Assert.Equal(0, requests);
        Assert.False(viewModel.IsAddingTemplate);
    }

    [Fact]
    public async Task LocalMockupTemplateCardUsesSourceImageReadinessAfterReload()
    {
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(referencedByTemplate: true, completeLocalSource: true);

        var card = Assert.Single(viewModel.MockupTemplateCards);
        Assert.Equal("Black", card.ColorSummary);
        Assert.Equal("1 compatible Variants", card.VariantSummary);
        Assert.Equal("Ready for use", card.Status);
        viewModel.EditTemplateCommand.Execute(card);
        Assert.Equal("Ready for use", viewModel.MockupTemplateLifecycleLabel);
        await viewModel.LoadForStoreAsync(offering.StoreId, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        Assert.Equal("Ready for use", Assert.Single(viewModel.MockupTemplateCards).Status);
    }

    [Fact]
    public async Task RefreshingSameOfferingClearsStaleReadinessUntilLoadCompletes()
    {
        DeferredOfferingManagementService? offeringManagement = null;
        var (viewModel, _, offering) = await CreateCatalogWithDesignAreaAsync(
            referencedByTemplate: false,
            offeringManagementFactory: repository => offeringManagement = new(repository),
            selectOffering: false);
        Assert.NotNull(offeringManagement);

        Assert.Equal("Setup incomplete", viewModel.OfferingReadinessStatus);

        viewModel.SelectOffering(offering.Id);
        await offeringManagement!.SecondLoadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Catalog readiness is loading", viewModel.OfferingReadinessStatus);
        Assert.Equal(0, viewModel.ReadyMockupTemplateCount);
        Assert.Empty(viewModel.OfferingReadinessGuidance);

        offeringManagement.Complete();
    }

    private sealed class RecordingMockupMetadataAssistanceService : IMockupSourceMetadataAssistanceService
    {
        public Guid ColorId { get; set; }
        public Guid SizeId { get; set; }
        public bool WasCalled { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiAvailabilityResult(AiAvailabilityKind.Ready, "ready", false));

        public Task<MockupSourceMetadataAssistanceResult> AssistAsync(
            MockupSourceMetadataAssistanceRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            var image = Assert.Single(request.Images);
            return Task.FromResult(new MockupSourceMetadataAssistanceResult(
                true,
                "done",
                [new MockupSourceMetadataAssistanceItem(
                    image.Token,
                    true,
                    [ColorId, SizeId],
                    new MockupImageSpaceMapping(100, 100, 5, 6, 70, 80),
                    0.95m,
                    "Applied",
                    null,
                    false)]));
        }
    }

    private static async Task<(CatalogSetupViewModel ViewModel, OfferingPlaceholder Area, BlueprintOffering Offering)> CreateCatalogWithDesignAreaAsync(
        bool referencedByTemplate,
        bool storeArchived = false,
        IMockupTemplateSourceImageService? sourceImages = null,
        bool completeLocalSource = false,
        IOfferingManagementService? offeringManagement = null,
        Func<IWorkspaceRepository, IOfferingManagementService>? offeringManagementFactory = null,
        bool selectOffering = true,
        IMockupSourceMetadataAssistanceService? assistance = null,
        IAssetFilePicker? filePicker = null,
        IRasterImageMetadataReader? rasterImageMetadataReader = null)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single() with { IsArchived = storeArchived };
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Printful tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printful", null, null, false, now, now);
        var otherOffering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Other tee", null, BlueprintOfferingKind.ProviderNetwork, null, "other", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var small = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "S", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / S", [black.Id, small.Id], false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 4500, 5400, [variant.Id], false, now, now);
        var populated = snapshot with
        {
            Stores = [store],
            Blueprints = [blueprint],
            BlueprintOfferings = [offering, otherOffering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, small],
            OfferingVariants = [variant],
            OfferingPlaceholders = [area]
        };
        if (referencedByTemplate)
        {
            var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front black", null, 1, false, now, now);
            populated = populated with { MockupTemplates = [template] };
            if (completeLocalSource)
            {
                var image = new MockupTemplateSourceImage(Guid.NewGuid(), template.Id, Guid.NewGuid(), new(100, 100, 0, 0, 100, 100), false, now, now);
                populated = populated with
                {
                    MockupTemplateRevisions = [new(Guid.NewGuid(), template.Id, 1, area.Id, now)],
                    MockupTemplateSourceImages = [image],
                    MockupTemplateSourceImageOptionValues = [new(image.Id, black.Id)]
                };
            }
        }
        var repository = new InMemoryWorkspaceRepository(populated);
        var selectedOfferingManagement = offeringManagement
            ?? offeringManagementFactory?.Invoke(repository)
            ?? new OfferingManagementService(repository);
        var viewModel = new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            selectedOfferingManagement,
            sourceImages: sourceImages,
            filePicker: filePicker,
            rasterImageMetadataReader: rasterImageMetadataReader,
            mockupSourceMetadataAssistance: assistance);
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        if (selectOffering) viewModel.SelectOffering(offering.Id);
        return (viewModel, area, offering);
    }

    private sealed class DeferredOfferingManagementService : IOfferingManagementService
    {
        private readonly IOfferingManagementService _inner;
        private readonly TaskCompletionSource<OfferingManagementState> _secondLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private OfferingManagementState? _initialState;

        public DeferredOfferingManagementService(IWorkspaceRepository repository) => _inner = new OfferingManagementService(repository);
        public TaskCompletionSource SecondLoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<OfferingManagementState> LoadOfferingAsync(OfferingContext context, CancellationToken cancellationToken = default)
        {
            if (_initialState is null)
            {
                _initialState = await _inner.LoadOfferingAsync(context, cancellationToken);
                return _initialState;
            }

            SecondLoadStarted.TrySetResult();
            return await _secondLoad.Task.WaitAsync(cancellationToken);
        }

        public void Complete() => _secondLoad.TrySetResult(_initialState!);
        public Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, bool includeArchived = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkVariantPreview> PreviewBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkVariantResult> ConfirmBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FocusedCommandResult> CreateVariantAsync(CreateFocusedVariantRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FocusedCommandResult> CreateDesignAreaAsync(CreateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FocusedCommandResult> UpdateDesignAreaAsync(UpdateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FocusedCommandResult> CreateMockupTemplateAsync(CreateFocusedMockupTemplateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubProviderCatalog(ProviderCatalogCandidateDescriptor descriptor) : IProviderCatalogCandidateSource
    {
        public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(descriptor);
    }

    private sealed class FixedRasterImageMetadataReader(RasterImageInfo dimensions) : IRasterImageMetadataReader
    {
        public List<string> ReadPaths { get; } = [];

        public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            ReadPaths.Add(sourcePath);
            return Task.FromResult(dimensions);
        }
    }

    private sealed class FailingRasterImageMetadataReader(Exception failure) : IRasterImageMetadataReader
    {
        public List<string> ReadPaths { get; } = [];

        public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            ReadPaths.Add(sourcePath);
            return Task.FromException<RasterImageInfo>(failure);
        }
    }

    private sealed class FixedLocalSourceFilePicker(string path) : IAssetFilePicker
    {
        public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(path);

        public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([path]);
    }

    private sealed class FixedLocalSourceFilesPicker(IReadOnlyList<string> paths) : IAssetFilePicker
    {
        public Task<string?> PickImportFileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(paths.FirstOrDefault());

        public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(paths);
    }

    private sealed class SelectiveRasterImageMetadataReader(string failingPath) : IRasterImageMetadataReader
    {
        public Task<RasterImageInfo> ReadAsync(string sourcePath, CancellationToken cancellationToken = default) =>
            sourcePath == failingPath
                ? Task.FromException<RasterImageInfo>(new InvalidDataException("Unsupported mockup image format."))
                : Task.FromResult(new RasterImageInfo(1600, 1200));
    }

    private sealed class FixedMockupTemplateSourceImageService(MockupTemplateSourceState state) : IMockupTemplateSourceImageService
    {
        public Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default) => Task.FromResult(state);

        public Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FailingMockupTemplateSourceImageService(Exception failure) : IMockupTemplateSourceImageService
    {
        public Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default) =>
            Task.FromException<MockupTemplateSourceState>(failure);

        public Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSourceImageService : IMockupTemplateSourceImageService
    {
        public Guid SourceImageId { get; } = Guid.NewGuid();
        public List<UpdateLocalMockupTemplateSourceRequest> Updates { get; } = [];

        public Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MockupTemplateSourceState([], [], false));

        public Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default)
        {
            Updates.Add(request);
            return Task.FromResult(MockupTemplateSetupResult.Success(new MockupTemplateSetupState(request.StoreId, false, [], [], [])));
        }
    }

    private sealed class PartialSaveMockupTemplateSourceImageService : IMockupTemplateSourceImageService
    {
        public int AddCallCount { get; private set; }
        public Guid OfferingId { get; set; }

        public Task<MockupTemplateSourceState> LoadAsync(Guid storeId, Guid templateId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MockupTemplateSourceState([], [], false));

        public Task<MockupTemplateSetupResult> AddAsync(AddLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            var state = new MockupTemplateSetupState(request.StoreId, false,
                [new MockupTemplate(request.TemplateId, OfferingId, null, "Manual front", null, 1, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)],
                [], []);
            return Task.FromResult(AddCallCount == 1
                ? MockupTemplateSetupResult.Success(state, request.TemplateId)
                : MockupTemplateSetupResult.Failure("The second source image failed.", state));
        }

        public Task<MockupTemplateSetupResult> UpdateAsync(UpdateLocalMockupTemplateSourceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static (CatalogSetupViewModel ViewModel, Guid StoreId) CreateProviderCatalogStateViewModel(IProviderCatalogCandidateSource? source)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Provider tee", null, BlueprintOfferingKind.ProviderNetwork, null, "provider", null, null, false, now, now);
        var repository = new InMemoryWorkspaceRepository(snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering]
        });
        return (new CatalogSetupViewModel(
            new CatalogSetupService(repository),
            new MockupTemplateSetupService(repository),
            new OfferingManagementService(repository, source),
            source), store.Id);
    }

    private sealed class ThrowingProviderCatalog : IProviderCatalogCandidateSource
    {
        public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Provider service timed out.");
    }

    private sealed class ThrowingOfferingManagementService : IOfferingManagementService
    {
        private static InvalidOperationException Failure() => new("Workspace context is unavailable.");

        public Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<BlueprintOfferingSetupSummary>>(Failure());
        public Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, bool includeArchived = false, CancellationToken cancellationToken = default) => Task.FromException<IReadOnlyList<BlueprintOfferingSetupSummary>>(Failure());
        public Task<OfferingManagementState> LoadOfferingAsync(OfferingContext context, CancellationToken cancellationToken = default) => Task.FromException<OfferingManagementState>(Failure());
        public Task<BulkVariantPreview> PreviewBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default) => Task.FromException<BulkVariantPreview>(Failure());
        public Task<BulkVariantResult> ConfirmBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default) => Task.FromException<BulkVariantResult>(Failure());
        public Task<FocusedCommandResult> CreateVariantAsync(CreateFocusedVariantRequest request, CancellationToken cancellationToken = default) => Task.FromException<FocusedCommandResult>(Failure());
        public Task<FocusedCommandResult> CreateDesignAreaAsync(CreateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default) => Task.FromException<FocusedCommandResult>(Failure());
        public Task<FocusedCommandResult> UpdateDesignAreaAsync(UpdateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default) => Task.FromException<FocusedCommandResult>(Failure());
        public Task<FocusedCommandResult> CreateMockupTemplateAsync(CreateFocusedMockupTemplateRequest request, CancellationToken cancellationToken = default) => Task.FromException<FocusedCommandResult>(Failure());
    }

    private sealed class PendingProviderCatalog : IProviderCatalogCandidateSource
    {
        private readonly TaskCompletionSource<ProviderCatalogCandidateDescriptor> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _completion.Task;
        }

        public void Complete(ProviderCatalogCandidateDescriptor descriptor) => _completion.TrySetResult(descriptor);
    }
}
