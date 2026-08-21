using FusionCanvas.App.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
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
}
