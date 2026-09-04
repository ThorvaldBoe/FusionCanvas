using FusionCanvas.App.StageTools;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Application.Items;

namespace FusionCanvas.App.Tests;

public class StageToolViewModelsTests
{
    [Fact]
    public void IdeaTool_LoadsIdeaAndAppliesReadOnlyReason()
    {
        var vm = new IdeaStageToolViewModel();

        vm.LoadFromMetadata(new ItemInspectorCreativeFields(
            Idea: "original idea",
            Audience: null,
            ConceptIdea: null,
            Phrase: null,
            GraphicDirection: null), canEdit: false);

        Assert.Equal("original idea", vm.Idea);
        Assert.True(vm.IsReadOnly);
        Assert.NotEmpty(vm.ReadOnlyReason);
    }

    [Fact]
    public void IdeaTool_ToStagePayload_CarriesIdeaOnly()
    {
        var vm = new IdeaStageToolViewModel { Idea = "new idea" };

        var payload = vm.ToStagePayload();

        Assert.Equal(WorkflowStage.Idea, payload.Stage);
        Assert.Equal("new idea", payload.Idea);
        Assert.Null(payload.ConceptIdea);
        Assert.Null(payload.Phrase);
        Assert.Null(payload.GraphicDirection);
    }

    [Fact]
    public void ConceptTool_LoadsPhraseAndGraphicDirection()
    {
        var vm = new ConceptStageToolViewModel();

        vm.LoadFromMetadata(new ItemInspectorCreativeFields(
            Idea: null,
            Audience: null,
            ConceptIdea: "concept",
            Phrase: "trimmed phrase",
            GraphicDirection: "direction"), canEdit: true);

        Assert.Equal("trimmed phrase", vm.Phrase);
        Assert.Equal("concept", vm.ConceptIdea);
        Assert.Equal("direction", vm.GraphicDirection);
        Assert.False(vm.IsReadOnly);
    }

    [Fact]
    public void ConceptTool_ToStagePayload_CarriesConceptFieldsOnly()
    {
        var vm = new ConceptStageToolViewModel
        {
            ConceptIdea = "concept idea",
            Phrase = "phrase",
            GraphicDirection = "direction"
        };

        var payload = vm.ToStagePayload();

        Assert.Equal(WorkflowStage.Concept, payload.Stage);
        Assert.Equal("concept idea", payload.ConceptIdea);
        Assert.Equal("phrase", payload.Phrase);
        Assert.Equal("direction", payload.GraphicDirection);
        Assert.Null(payload.Idea);
    }

    [Fact]
    public void ListingTool_ReportsStatusSummaryAndHonorsEditability()
    {
        var vm = new ListingStageToolViewModel();

        vm.Load(ItemStatus.Published, canEdit: false);

        Assert.Contains("Published", vm.StatusSummary);
        Assert.True(vm.IsReadOnly);
    }

    [Fact]
    public async Task ListingTool_ShowsTemplateBlockersWhenNoReadyTemplateExists()
    {
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty, [], null, [], ["Black"],
            "No ready Mockup Templates are available. Complete the requirements shown below in Store settings.",
            null,
            [new MockupTemplateEligibilityDiagnostic(Guid.NewGuid(), "Front image", [
                MockupTemplateReadinessBlocker.MissingImage,
                MockupTemplateReadinessBlocker.MissingMapping])])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(vm.TemplateDiagnostics);
        Assert.Equal("Front image", diagnostic.TemplateName);
        Assert.Contains("Choose a mockup image.", diagnostic.Guidance);
        Assert.Contains("Add a valid design-area placement mapping.", diagnostic.Guidance);
        Assert.True(vm.HasBlockedReason);
        Assert.False(vm.CanApply);
    }

    [Fact]
    public async Task ListingTool_DistinguishesOfferingWithNoTemplates()
    {
        var vm = new ListingStageToolViewModel(new StubMockupGenerationService(new MockupGenerationState(
            Guid.NewGuid(), Guid.NewGuid(), false, string.Empty, [], null, [], ["Black"],
            "No Mockup Templates are configured for this Offering. Add one in Store settings.", null, [])));

        await vm.LoadAsync(Guid.NewGuid(), ItemStatus.Draft, canEdit: true, TestContext.Current.CancellationToken);

        Assert.Contains("No Mockup Templates are configured", vm.BlockedReason);
        Assert.Empty(vm.TemplateDiagnostics);
    }

    private sealed class StubMockupGenerationService(MockupGenerationState state) : IMockupGenerationService
    {
        public Task<MockupGenerationState> LoadAsync(Guid itemId, bool isReadOnly, string readOnlyReason, CancellationToken cancellationToken = default) => Task.FromResult(state);

        public Task<MockupGenerationResult> ApplyAsync(MockupGenerationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MockupGenerationResult.Failure("Not used in this test."));
    }
}
