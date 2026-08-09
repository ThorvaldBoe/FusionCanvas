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
        viewModel.SelectedOffering = offering;
        viewModel.SelectedOption = option;
        viewModel.SelectedPlaceholder = placeholder;
        viewModel.OptionName = "Size";
        viewModel.OptionValue = "M";
        viewModel.TemplateName = "Front mockup";

        Assert.True(viewModel.IsAvailable);
        Assert.True(viewModel.CanEdit);
        Assert.Contains(OptionKind.Color, viewModel.OptionKinds);
        Assert.True(viewModel.CreateOptionCommand.CanExecute(null));
        Assert.True(viewModel.CreateOptionValueCommand.CanExecute(null));
        Assert.True(viewModel.CreateTemplateCommand.CanExecute(null));
        Assert.True(viewModel.AddTemplateColorCommand.CanExecute(null) == false);
    }
}
