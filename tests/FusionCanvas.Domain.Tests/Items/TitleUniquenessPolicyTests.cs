using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests;

public class TitleUniquenessPolicyTests
{
    private static readonly Guid StoreA = Guid.NewGuid();
    private static readonly Guid StoreB = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Item Item(
        Guid? id = null,
        Guid? storeId = null,
        string? name = null,
        ItemStatus status = ItemStatus.Draft,
        bool archived = false) =>
        new(
            id ?? Guid.NewGuid(),
            storeId ?? StoreA,
            Guid.NewGuid(),
            null,
            name ?? "Title",
            null,
            status,
            WorkflowStage.Idea,
            archived,
            Now,
            Now,
            "{}");

    [Fact]
    public void HasCreativeContent_ReturnsTrueWhenAnySourceHasContent()
    {
        Assert.False(TitleUniquenessPolicy.HasCreativeContent(new Dictionary<string, string>()));
        Assert.False(TitleUniquenessPolicy.HasCreativeContent(
            new Dictionary<string, string> { ["notes"] = "hello" }));

        Assert.True(TitleUniquenessPolicy.HasCreativeContent(
            new Dictionary<string, string> { ["idea"] = "a pug commanding a walk" }));
        Assert.True(TitleUniquenessPolicy.HasCreativeContent(
            new Dictionary<string, string> { ["concept.idea"] = "stubborn pug" }));
        Assert.True(TitleUniquenessPolicy.HasCreativeContent(
            new Dictionary<string, string> { ["phrase"] = "coach hostage" }));
        Assert.True(TitleUniquenessPolicy.HasCreativeContent(
            new Dictionary<string, string> { ["graphicDirection"] = "pug in uniform" }));
    }

    [Fact]
    public void DistinctTitles_ScopesToStoreAndExcludesActiveArchivedAndRejected()
    {
        var active = Item(name: "Pug coach hostage");
        var sameStore = Item(name: "Pug coach hostage", status: ItemStatus.Draft);
        var archived = Item(name: "Pug coach hostage", archived: true);
        var rejected = Item(name: "Pug coach hostage", status: ItemStatus.Rejected);
        var otherStore = Item(storeId: StoreB, name: "Pug coach hostage");
        var blank = Item(name: "   ");

        var titles = TitleUniquenessPolicy.DistinctTitles(
            new[] { active, sameStore, archived, rejected, otherStore, blank },
            StoreA,
            active.Id);

        Assert.Equal(new HashSet<string> { "Pug coach hostage" }, titles, StringComparer.Ordinal);
    }

    [Fact]
    public void DistinctTitles_ComparesCaseInsensitively()
    {
        var one = Item(name: "Coffee Lover");
        var two = Item(name: "coffee lover", status: ItemStatus.Draft);

        var titles = TitleUniquenessPolicy.DistinctTitles(new[] { one, two }, StoreA, Guid.NewGuid());

        Assert.Single(titles);
        Assert.True(titles.Contains("COFFEE LOVER"));
    }

    [Fact]
    public void IsUnique_IgnoresCaseAndWhitespaceAndRejectsEmpty()
    {
        var existing = new HashSet<string> { "Pug Coach" } ; // StringComparer.OrdinalIgnoreCase

        Assert.True(TitleUniquenessPolicy.IsUnique("Dog walker", existing));
        Assert.False(TitleUniquenessPolicy.IsUnique("pug COACH", existing));
        Assert.False(TitleUniquenessPolicy.IsUnique("  Pug coach  ", existing));
        Assert.False(TitleUniquenessPolicy.IsUnique("   ", existing));
    }

    [Fact]
    public void WithNumericSuffix_AppendsSmallestUnusedInteger()
    {
        var existing = new HashSet<string> { "Pug coach hostage 2" };

        Assert.Equal("Pug coach hostage 3", TitleUniquenessPolicy.WithNumericSuffix("Pug coach hostage", existing));
        Assert.Equal("Pug coach hostage 2", TitleUniquenessPolicy.WithNumericSuffix("Pug coach hostage", new HashSet<string>()));
        Assert.Equal("Pug coach hostage 4", TitleUniquenessPolicy.WithNumericSuffix(
            "Pug coach hostage",
            new HashSet<string> { "Pug coach hostage 2", "Pug coach hostage 3" }));
    }

    [Fact]
    public void WithNumericSuffix_ReturnsEmptyForEmptyCandidate()
    {
        Assert.Equal(string.Empty, TitleUniquenessPolicy.WithNumericSuffix("   ", new HashSet<string>()));
    }

    [Fact]
    public void MaximumAttempts_IsBounded()
    {
        Assert.Equal(4, TitleUniquenessPolicy.MaximumAttempts);
    }
}
