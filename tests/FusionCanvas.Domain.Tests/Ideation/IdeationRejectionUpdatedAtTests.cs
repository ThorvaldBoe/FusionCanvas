using FusionCanvas.Domain.Ideation;

namespace FusionCanvas.Domain.Tests.Ideation;

public sealed class IdeationRejectionUpdatedAtTests
{
    [Fact]
    public void UpdatedAt_IsNullByDefault()
    {
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Idea",
            null,
            IdeationMode.Basic,
            DateTimeOffset.UtcNow);

        Assert.Null(rejection.UpdatedAt);
    }

    [Fact]
    public void UpdatedAt_CanBeSetExplicitly()
    {
        var createdAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        var updatedAt = createdAt.AddMinutes(5);

        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Idea",
            "Reason",
            IdeationMode.Basic,
            createdAt,
            updatedAt);

        Assert.Equal(createdAt, rejection.CreatedAt);
        Assert.Equal(updatedAt, rejection.UpdatedAt);
    }

    [Fact]
    public void UpdatedAt_BeforeCreatedAtIsRejected()
    {
        var createdAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        var updatedAt = createdAt.AddMinutes(-1);

        Assert.Throws<ArgumentException>(() => new IdeationRejection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Idea",
            null,
            IdeationMode.Basic,
            createdAt,
            updatedAt));
    }
}
