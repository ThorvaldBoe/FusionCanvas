using FusionCanvas.Application.AI;
using FusionCanvas.Application.Ideation;

namespace FusionCanvas.Application.Tests.Ideation;

public sealed class ConfiguredIdeationAccessStatusTests
{
    [Fact]
    public async Task StartsCheckingThenRefreshesCachedStateAndRaisesChange()
    {
        var ai = new StubAi(new(
            AiAvailabilityKind.MissingCredential,
            "Add a key."));
        var access = new ConfiguredIdeationAccessStatus(ai);
        var changes = 0;
        access.AvailabilityChanged += (_, _) => changes++;

        Assert.False(access.GetAvailability().IsAvailable);
        Assert.Contains("Checking", access.GetAvailability().UnavailableReason, StringComparison.Ordinal);

        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.False(access.GetAvailability().IsAvailable);
        Assert.Equal("Add a key.", access.GetAvailability().UnavailableReason);

        ai.Availability = AiAvailabilityResult.Ready;
        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(access.GetAvailability().IsAvailable);
        Assert.Equal(2, changes);
    }

    private sealed class StubAi(AiAvailabilityResult availability) : IAiTextGenerationService
    {
        public AiAvailabilityResult Availability { get; set; } = availability;

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Availability);

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
