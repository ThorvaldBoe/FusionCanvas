using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class ItemLifecycleStatusTests
{
    [Fact]
    public void ItemStatuses_ExposeOperationalVocabularyInOrder()
    {
        Assert.Equal(
            [ItemStatus.Draft, ItemStatus.Published, ItemStatus.Paused, ItemStatus.Rejected],
            ItemStatuses.Ordered);
    }

    [Fact]
    public void ItemStatusVocabulary_ExcludesStageLikeAndArchiveValues()
    {
        var names = Enum.GetNames<ItemStatus>();

        Assert.Equal(["Draft", "Published", "Paused", "Rejected"], names);
        Assert.DoesNotContain("Active", names);
        Assert.DoesNotContain("Ready", names);
        Assert.DoesNotContain("Archived", names);
    }

    [Fact]
    public void ItemStatuses_ExposeDisplayNamesWithoutUiFrameworkTypes()
    {
        var labels = ItemStatuses.Ordered.Select(ItemStatuses.GetDisplayName).ToArray();

        Assert.Equal(["Draft", "Published", "Paused", "Rejected"], labels);

        var referencedAssemblies = typeof(ItemStatus).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
        Assert.DoesNotContain("Avalonia", referencedAssemblies);
    }

    [Fact]
    public void ItemStatuses_DisplayNameRejectsUnknownValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemStatuses.GetDisplayName((ItemStatus)99));
    }

    [Fact]
    public void Item_CarriesStageAndStatusAsIndependentFacts()
    {
        var now = DateTimeOffset.UtcNow;
        var item = new Item(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "Morning coffee idea", null,
            ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

        var advanced = item with { Stage = WorkflowStage.Concept };

        Assert.Equal(WorkflowStage.Concept, advanced.Stage);
        Assert.Equal(ItemStatus.Draft, advanced.Status);

        var paused = item with { Status = ItemStatus.Paused };

        Assert.Equal(ItemStatus.Paused, paused.Status);
        Assert.Equal(WorkflowStage.Idea, paused.Stage);
    }

    [Fact]
    public void Item_AllowsEmptyWorkingTitleWithoutFallbackPersisted()
    {
        var now = DateTimeOffset.UtcNow;
        var item = new Item(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            string.Empty, null,
            ItemStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

        Assert.Equal(string.Empty, item.Name);
    }
}
