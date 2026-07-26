using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.Domain.Tests;

public class ItemWorkflowPolicyTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid NicheId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Item Item(ItemStatus status, WorkflowStage stage, bool archived = false, string? name = null) =>
        new(Guid.NewGuid(), StoreId, NicheId, null, name ?? string.Empty, null, status, stage, archived, Now, Now, "{}");

    [Theory]
    [InlineData(WorkflowStage.Idea, WorkflowStage.Concept, true)]
    [InlineData(WorkflowStage.Concept, WorkflowStage.Idea, true)]
    [InlineData(WorkflowStage.Concept, WorkflowStage.Design, true)]
    [InlineData(WorkflowStage.Design, WorkflowStage.Listing, true)]
    [InlineData(WorkflowStage.Idea, WorkflowStage.Design, false)]
    [InlineData(WorkflowStage.Idea, WorkflowStage.Listing, false)]
    [InlineData(WorkflowStage.Listing, WorkflowStage.Idea, false)]
    [InlineData(WorkflowStage.Concept, WorkflowStage.Concept, false)]
    public void CanMoveAdjacent_RespectsAdjacentOnlyBoundary(WorkflowStage current, WorkflowStage destination, bool expected)
    {
        Assert.Equal(expected, ItemWorkflowPolicy.CanMoveAdjacent(current, destination));
    }

    [Fact]
    public void OrderedStages_AndViewableStages_ExposeAllFourStagesInOrder()
    {
        var expected = new[] { WorkflowStage.Idea, WorkflowStage.Concept, WorkflowStage.Design, WorkflowStage.Listing };
        Assert.Equal(expected, ItemWorkflowPolicy.OrderedStages);
        Assert.Equal(expected, ItemWorkflowPolicy.ViewableStages);
    }

    [Theory]
    [InlineData(ItemStatus.Draft, WorkflowStage.Listing, ItemStatus.Published, true, true)]
    [InlineData(ItemStatus.Draft, WorkflowStage.Idea, ItemStatus.Published, false, false)]
    [InlineData(ItemStatus.Draft, WorkflowStage.Concept, ItemStatus.Published, false, false)]
    [InlineData(ItemStatus.Draft, WorkflowStage.Design, ItemStatus.Published, false, false)]
    [InlineData(ItemStatus.Draft, WorkflowStage.Listing, ItemStatus.Rejected, true, true)]
    [InlineData(ItemStatus.Draft, WorkflowStage.Idea, ItemStatus.Rejected, true, true)]
    [InlineData(ItemStatus.Published, WorkflowStage.Listing, ItemStatus.Paused, true, true)]
    [InlineData(ItemStatus.Published, WorkflowStage.Listing, ItemStatus.Rejected, true, true)]
    [InlineData(ItemStatus.Paused, WorkflowStage.Listing, ItemStatus.Published, true, true)]
    [InlineData(ItemStatus.Paused, WorkflowStage.Idea, ItemStatus.Published, false, false)]
    [InlineData(ItemStatus.Paused, WorkflowStage.Listing, ItemStatus.Draft, true, false)]
    [InlineData(ItemStatus.Paused, WorkflowStage.Listing, ItemStatus.Rejected, true, true)]
    [InlineData(ItemStatus.Rejected, WorkflowStage.Listing, ItemStatus.Draft, true, false)]
    public void DecideTransition_ImplementsExactApprovedGraph(ItemStatus current, WorkflowStage stage, ItemStatus target, bool allowed, bool confirmation)
    {
        var decision = ItemWorkflowPolicy.DecideTransition(current, stage, target);

        Assert.Equal(allowed, decision.IsAllowed);
        Assert.Equal(confirmation, decision.RequiresConfirmation);
    }

    [Theory]
    [InlineData(ItemStatus.Draft, ItemStatus.Draft)]
    [InlineData(ItemStatus.Draft, ItemStatus.Paused)]
    [InlineData(ItemStatus.Published, ItemStatus.Draft)]
    [InlineData(ItemStatus.Paused, ItemStatus.Paused)]
    [InlineData(ItemStatus.Rejected, ItemStatus.Published)]
    [InlineData(ItemStatus.Rejected, ItemStatus.Paused)]
    [InlineData(ItemStatus.Rejected, ItemStatus.Rejected)]
    public void DecideTransition_RejectsDisallowedDirectTransitions(ItemStatus current, ItemStatus target)
    {
        var decision = ItemWorkflowPolicy.DecideTransition(current, WorkflowStage.Listing, target);

        Assert.False(decision.IsAllowed);
        Assert.False(decision.RequiresConfirmation);
    }

    [Fact]
    public void DecideTransition_DoesNotInferFromEnumOrdering()
    {
        var draftToRejected = ItemWorkflowPolicy.DecideTransition(ItemStatus.Draft, WorkflowStage.Idea, ItemStatus.Rejected);
        var rejectedToDraft = ItemWorkflowPolicy.DecideTransition(ItemStatus.Rejected, WorkflowStage.Idea, ItemStatus.Draft);

        Assert.True(draftToRejected.IsAllowed);
        Assert.True(rejectedToDraft.IsAllowed);
        Assert.False(rejectedToDraft.RequiresConfirmation);
        Assert.True(draftToRejected.RequiresConfirmation);
    }

    [Theory]
    [InlineData(ItemStatus.Draft, true)]
    [InlineData(ItemStatus.Published, false)]
    [InlineData(ItemStatus.Rejected, false)]
    public void CanEditStage_CurrentStageEditableOnlyForActiveDraft(ItemStatus status, bool editable)
    {
        var item = Item(status, WorkflowStage.Concept);

        var decision = ItemWorkflowPolicy.CanEditStage(item, WorkflowStage.Concept);

        Assert.Equal(editable, decision.IsAllowed);
    }

    [Fact]
    public void CanEditStage_ActiveViewDifferentFromCurrentIsReadOnly()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Design);

        var reviewingEarlier = ItemWorkflowPolicy.CanEditStage(item, WorkflowStage.Concept);

        Assert.False(reviewingEarlier.IsAllowed);
    }

    [Fact]
    public void CanEditStage_ArchivedBlocksEditing()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Idea, archived: true);

        var decision = ItemWorkflowPolicy.CanEditStage(item, WorkflowStage.Idea);

        Assert.False(decision.IsAllowed);
    }

    [Theory]
    [InlineData(ItemOperationKind.StageContent, ItemStatus.Published, false)]
    [InlineData(ItemOperationKind.DesignFile, ItemStatus.Published, false)]
    [InlineData(ItemOperationKind.RelatedAssetLink, ItemStatus.Published, false)]
    [InlineData(ItemOperationKind.StageMovement, ItemStatus.Published, false)]
    [InlineData(ItemOperationKind.WorkingTitle, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.Notes, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.TagLink, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.TopicPlacement, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.Archive, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.StatusRecovery, ItemStatus.Published, true)]
    [InlineData(ItemOperationKind.StageContent, ItemStatus.Rejected, false)]
    [InlineData(ItemOperationKind.DesignFile, ItemStatus.Rejected, false)]
    [InlineData(ItemOperationKind.StageMovement, ItemStatus.Rejected, false)]
    [InlineData(ItemOperationKind.WorkingTitle, ItemStatus.Rejected, true)]
    [InlineData(ItemOperationKind.Notes, ItemStatus.Rejected, true)]
    [InlineData(ItemOperationKind.TagLink, ItemStatus.Rejected, true)]
    [InlineData(ItemOperationKind.StatusRecovery, ItemStatus.Rejected, true)]
    public void CanPerformOperation_ClassifiesProductContentVersusSharedMetadata(ItemOperationKind operation, ItemStatus status, bool allowed)
    {
        var item = Item(status, WorkflowStage.Listing);

        var decision = ItemWorkflowPolicy.CanPerformOperation(item, operation);

        Assert.Equal(allowed, decision.IsAllowed);
    }

    [Theory]
    [InlineData(ItemOperationKind.StageContent)]
    [InlineData(ItemOperationKind.DesignFile)]
    [InlineData(ItemOperationKind.RelatedAssetLink)]
    [InlineData(ItemOperationKind.StageMovement)]
    [InlineData(ItemOperationKind.WorkingTitle)]
    [InlineData(ItemOperationKind.Notes)]
    [InlineData(ItemOperationKind.TagLink)]
    [InlineData(ItemOperationKind.TopicPlacement)]
    public void CanPerformOperation_ArchivedBlocksEverythingExceptRestoreAndRecovery(ItemOperationKind operation)
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Concept, archived: true);

        var decision = ItemWorkflowPolicy.CanPerformOperation(item, operation);

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void CanPerformOperation_ArchivedKeepsArchiveRestoreAndStatusRecoveryAvailable()
    {
        var item = Item(ItemStatus.Rejected, WorkflowStage.Listing, archived: true);

        Assert.True(ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.Archive).IsAllowed);
        Assert.True(ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StatusRecovery).IsAllowed);
    }

    [Fact]
    public void CanPerformOperation_DraftAllowsAllListedOperations()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Concept);

        foreach (var operation in Enum.GetValues<ItemOperationKind>())
        {
            Assert.True(ItemWorkflowPolicy.CanPerformOperation(item, operation).IsAllowed);
        }
    }

    [Fact]
    public void ItemDisplayNameFormatter_ReturnsTrimmedTitleWhenPresent()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Idea, name: "  Coffee Mug  ");

        Assert.Equal("Coffee Mug", ItemDisplayNameFormatter.Format(item));
    }

    [Fact]
    public void ItemDisplayNameFormatter_ReturnsFallbackForEmptyTitle()
    {
        var id = Guid.Parse("aB1234567890deF0aaaaaaaaaaaaaaaa");
        var item = new Item(id, StoreId, NicheId, null, string.Empty, null, ItemStatus.Draft, WorkflowStage.Idea, false, Now, Now, "{}");

        var label = ItemDisplayNameFormatter.Format(item);

        Assert.StartsWith("Untitled item · ", label);
        Assert.Equal("ab123456", label["Untitled item · ".Length..]);
    }

    [Fact]
    public void ItemDisplayNameFormatter_DoesNotMutateItemData()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Idea, name: string.Empty);

        _ = ItemDisplayNameFormatter.Format(item);

        Assert.Equal(string.Empty, item.Name);
    }

    [Fact]
    public void WorkspaceEntityKind_ItemPreservesNumericValueThree()
    {
        Assert.Equal(3, (int)WorkspaceEntityKind.Item);
    }

    [Fact]
    public void CanMoveAdjacent_RegressionPreservesDownstreamData()
    {
        var item = Item(ItemStatus.Draft, WorkflowStage.Design);

        Assert.True(ItemWorkflowPolicy.CanMoveAdjacent(item.Stage, WorkflowStage.Concept));
        Assert.True(ItemWorkflowPolicy.CanMoveAdjacent(WorkflowStage.Concept, WorkflowStage.Design));
    }
}
