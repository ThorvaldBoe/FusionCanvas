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
        Assert.Equal(ProviderCatalogLoadState.Available, viewModel.ProviderCatalogState);
        Assert.Contains("matches the target Design Area", viewModel.ProviderImageSelectionStateMessage, StringComparison.Ordinal);
        Assert.Contains("Local upload", viewModel.ProviderImageSelectionInstructions, StringComparison.Ordinal);
        Assert.Contains("drag/drop", viewModel.ProviderImageSelectionInstructions, StringComparison.Ordinal);
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
        Assert.Contains("Sync", empty.ViewModel.ProviderImageSelectionRecoveryMessage, StringComparison.Ordinal);

        var unavailable = CreateProviderCatalogStateViewModel(new StubProviderCatalog(new ProviderCatalogCandidateDescriptor(
            new OfferingContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), false, "Provider connection is not configured.", new HashSet<ProviderCatalogCombination>(), [])));
        await unavailable.ViewModel.LoadForStoreAsync(unavailable.StoreId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderCatalogLoadState.Unavailable, unavailable.ViewModel.ProviderCatalogState);
        Assert.Contains("not configured", unavailable.ViewModel.ProviderImageSelectionStateMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Configure or sync", unavailable.ViewModel.ProviderImageSelectionRecoveryMessage, StringComparison.Ordinal);

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

    private static async Task<(CatalogSetupViewModel ViewModel, OfferingPlaceholder Area, BlueprintOffering Offering)> CreateCatalogWithDesignAreaAsync(
        bool referencedByTemplate,
        bool storeArchived = false)
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
