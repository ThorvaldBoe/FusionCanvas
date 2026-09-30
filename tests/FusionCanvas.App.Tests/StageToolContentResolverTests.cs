using FusionCanvas.Application.StageTools;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.App.Tests;

public sealed class StageToolContentResolverTests
{
    [Theory]
    [InlineData("idea-stage-tool", StageToolContentKind.Idea)]
    [InlineData("concept-stage-tool", StageToolContentKind.Concept)]
    [InlineData("design-stage-tool", StageToolContentKind.Design)]
    [InlineData("listing-stage-tool", StageToolContentKind.Listing)]
    public void Resolve_BuiltInDetailViewKeyReturnsOwnedContent(string detailViewKey, StageToolContentKind expectedKind)
    {
        var descriptor = new StageToolDescriptor(
            "tool",
            "Tool",
            "Tool description",
            detailViewKey,
            [WorkflowStage.Idea],
            RequiresSelectedItem: false,
            IsDefault: true);
        var owner = MainWindowViewModelFactory.CreateSample();

        var content = new BuiltInStageToolContentResolver().Resolve(descriptor, owner);

        Assert.NotNull(content);
        Assert.Same(owner, content.Owner);
        Assert.Same(descriptor, content.Descriptor);
        Assert.Equal(expectedKind, content.Kind);
    }

    [Fact]
    public void Resolve_UnknownDetailViewKeyDoesNotPretendContentExists()
    {
        var descriptor = new StageToolDescriptor(
            "contributed-tool",
            "Contributed",
            "Contributed description",
            "idea-stage-tool",
            [WorkflowStage.Idea],
            RequiresSelectedItem: false,
            IsDefault: false,
            SourceKind: StageToolSourceKind.Contributed);

        var content = new BuiltInStageToolContentResolver().Resolve(
            descriptor,
            MainWindowViewModelFactory.CreateSample());

        Assert.Null(content);
    }
}
