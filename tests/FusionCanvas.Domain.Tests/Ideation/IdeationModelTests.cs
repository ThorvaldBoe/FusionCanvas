using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Domain.Tests.Ideation;

public sealed class IdeationModelTests
{
    [Fact]
    public void Modes_HaveStableValues()
    {
        Assert.Equal(0, (int)IdeationMode.Basic);
        Assert.Equal(1, (int)IdeationMode.Snowclones);
    }

    [Fact]
    public void Rejection_NormalizesOptionalText()
    {
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  A grumpy pug  ",
            "  Too similar  ",
            IdeationMode.Basic,
            DateTimeOffset.UtcNow);

        Assert.Equal("A grumpy pug", rejection.Text);
        Assert.Equal("Too similar", rejection.Reason);
    }

    [Fact]
    public void Rejection_RejectsMissingIdentityAndText()
    {
        Assert.Throws<ArgumentException>(() => new IdeationRejection(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Idea",
            null,
            IdeationMode.Basic,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            " ",
            null,
            IdeationMode.Basic,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void WorkspaceSnapshot_RetainsRejections()
    {
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Idea",
            null,
            IdeationMode.Snowclones,
            DateTimeOffset.UtcNow);

        var snapshot = WorkspaceSnapshot.Empty with { IdeationRejections = [rejection] };

        Assert.Same(rejection, Assert.Single(snapshot.IdeationRejections));
    }
}
