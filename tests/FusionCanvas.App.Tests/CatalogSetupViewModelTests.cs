using FusionCanvas.App.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.App.Tests.TestSupport;

namespace FusionCanvas.App.Tests;

public sealed class CatalogSetupViewModelTests
{
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
    public async Task FocusedTemplateDraftRequiresProviderImageDesignAreaColorAndBoundedMapping()
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
            Blueprints = [blueprint], BlueprintOfferings = [offering], OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium], OfferingVariants = [variant], OfferingPlaceholders = [area]
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
        Assert.Equal(1000, viewModel.MappingImageWidth);
        Assert.Equal(1200, viewModel.MappingImageHeight);
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));

        viewModel.MappingWidth = 2000;
        Assert.False(viewModel.CreateTemplateCommand.CanExecute(null));
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
        Assert.False(viewModel.IsAddingVariant);
        Assert.True(viewModel.IsAddingBulkVariants);

        viewModel.CancelBulkVariantsCommand.Execute(null);
        Assert.False(viewModel.IsAddingBulkVariants);
        Assert.False(viewModel.HasActiveDraft);
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

    private static async Task<(CatalogSetupViewModel ViewModel, OfferingPlaceholder Area, BlueprintOffering Offering)> CreateCatalogWithDesignAreaAsync(bool referencedByTemplate)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = SampleWorkspace.Create();
        var store = snapshot.Stores.Single();
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, now, now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Printful tee", null, BlueprintOfferingKind.ProviderNetwork, null, "printful", null, null, false, now, now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var small = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "S", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / S", [black.Id, small.Id], false, now, now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 4500, 5400, [variant.Id], false, now, now);
        var populated = snapshot with
        {
            Blueprints = [blueprint],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, small],
            OfferingVariants = [variant],
            OfferingPlaceholders = [area]
        };
        if (referencedByTemplate)
        {
            var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front black", null, 1, false, now, now);
            populated = populated with { MockupTemplates = [template] };
        }
        var repository = new InMemoryWorkspaceRepository(populated);
        var viewModel = new CatalogSetupViewModel(new CatalogSetupService(repository), new MockupTemplateSetupService(repository));
        await viewModel.LoadForStoreAsync(store.Id, TestContext.Current.CancellationToken);
        viewModel.SelectOffering(offering.Id);
        return (viewModel, area, offering);
    }

    private sealed class StubProviderCatalog(ProviderCatalogCandidateDescriptor descriptor) : IProviderCatalogCandidateSource
    {
        public Task<ProviderCatalogCandidateDescriptor> LoadAsync(OfferingContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(descriptor);
    }
}
