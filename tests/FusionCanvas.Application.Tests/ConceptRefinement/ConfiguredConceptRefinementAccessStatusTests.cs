using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;

namespace FusionCanvas.Application.Tests.ConceptRefinement;

public sealed class ConfiguredConceptRefinementAccessStatusTests
{
    [Fact]
    public async Task StartsCheckingThenRefreshesCachedStateAndRaisesChange()
    {
        var ai = new StubAi(new(
            AiAvailabilityKind.MissingCredential,
            "Concept AI is not configured."));
        var access = new ConfiguredConceptRefinementAccessStatus(ai);
        var changes = 0;
        access.AvailabilityChanged += (_, _) => changes++;

        Assert.False(access.GetAvailability().IsAvailable);
        Assert.Contains("Checking", access.GetAvailability().UnavailableReason, StringComparison.Ordinal);

        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.False(access.GetAvailability().IsAvailable);
        Assert.Equal("Concept AI is not configured.", access.GetAvailability().UnavailableReason);

        ai.Availability = AiAvailabilityResult.Ready;
        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.True(access.GetAvailability().IsAvailable);
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task RefreshAsync_WhenStateUnchanged_DoesNotRaiseEvent()
    {
        var ai = new StubAi(AiAvailabilityResult.Ready);
        var access = new ConfiguredConceptRefinementAccessStatus(ai);
        var changes = 0;
        access.AvailabilityChanged += (_, _) => changes++;

        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, changes);

        await access.RefreshAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, changes);
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