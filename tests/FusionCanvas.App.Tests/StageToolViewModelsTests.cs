using FusionCanvas.App.StageTools;
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
    public void ListingTool_ReportsStatusSummaryAndIsAlwaysReadOnly()
    {
        var vm = new ListingStageToolViewModel();

        vm.Load(ItemStatus.Published, canEdit: true);

        Assert.Contains("Published", vm.StatusSummary);
        Assert.True(vm.IsReadOnly);
    }
}
