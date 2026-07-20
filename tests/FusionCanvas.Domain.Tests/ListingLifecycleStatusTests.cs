using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class ListingLifecycleStatusTests
{
    [Fact]
    public void ListingStatuses_ExposeOperationalVocabularyInOrder()
    {
        Assert.Equal(
            [ListingStatus.Draft, ListingStatus.Published, ListingStatus.Paused, ListingStatus.Rejected],
            ListingStatuses.Ordered);
    }

    [Fact]
    public void ListingStatusVocabulary_ExcludesStageLikeAndArchiveValues()
    {
        var names = Enum.GetNames<ListingStatus>();

        Assert.Equal(["Draft", "Published", "Paused", "Rejected"], names);
        Assert.DoesNotContain("Active", names);
        Assert.DoesNotContain("Ready", names);
        Assert.DoesNotContain("Archived", names);
    }

    [Fact]
    public void ListingStatuses_ExposeDisplayNamesWithoutUiFrameworkTypes()
    {
        var labels = ListingStatuses.Ordered.Select(ListingStatuses.GetDisplayName).ToArray();

        Assert.Equal(["Draft", "Published", "Paused", "Rejected"], labels);

        var referencedAssemblies = typeof(ListingStatus).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();
        Assert.DoesNotContain("Avalonia", referencedAssemblies);
    }

    [Fact]
    public void ListingStatuses_DisplayNameRejectsUnknownValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ListingStatuses.GetDisplayName((ListingStatus)99));
    }

    [Fact]
    public void Listing_CarriesStageAndStatusAsIndependentFacts()
    {
        var now = DateTimeOffset.UtcNow;
        var listing = new Listing(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "Morning coffee idea", null,
            ListingStatus.Draft, WorkflowStage.Idea, false, now, now, "{}");

        var advanced = listing with { Stage = WorkflowStage.Concept };

        Assert.Equal(WorkflowStage.Concept, advanced.Stage);
        Assert.Equal(ListingStatus.Draft, advanced.Status);

        var paused = listing with { Status = ListingStatus.Paused };

        Assert.Equal(ListingStatus.Paused, paused.Status);
        Assert.Equal(WorkflowStage.Idea, paused.Stage);
    }
}
