using FusionCanvas.Application.AI;
using FusionCanvas.Application.SllGeneration;

namespace FusionCanvas.Application.Tests.SllGeneration;

public sealed class SllAccessStatusTests
{
    [Fact]
    public async Task RefreshAsync_WhenReady_IsAvailable()
    {
        var ai = new CapturingAi { Availability = AiAvailabilityResult.Ready };
        var status = new ConfiguredSllAccessStatus(ai);

        await status.RefreshAsync(TestContext.Current.CancellationToken);

        var availability = status.GetAvailability();
        Assert.True(availability.IsAvailable);
        Assert.Null(availability.UnavailableReason);
    }

    [Fact]
    public async Task RefreshAsync_WhenMissingCredential_IsUnavailableWithReason()
    {
        var ai = new CapturingAi { Availability = new AiAvailabilityResult(AiAvailabilityKind.MissingCredential, "Add an API key.") };
        var status = new ConfiguredSllAccessStatus(ai);

        await status.RefreshAsync(TestContext.Current.CancellationToken);

        var availability = status.GetAvailability();
        Assert.False(availability.IsAvailable);
        Assert.Equal("Add an API key.", availability.UnavailableReason);
    }

    [Fact]
    public async Task RefreshAsync_WhenInvalidConfiguration_IsUnavailable()
    {
        var ai = new CapturingAi { Availability = new AiAvailabilityResult(AiAvailabilityKind.InvalidConfiguration, "Bad config.") };
        var status = new ConfiguredSllAccessStatus(ai);

        await status.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.False(status.GetAvailability().IsAvailable);
    }

    [Fact]
    public async Task RefreshAsync_RaisesEventOnlyWhenAvailabilityChanges()
    {
        var ai = new CapturingAi { Availability = AiAvailabilityResult.Ready };
        var status = new ConfiguredSllAccessStatus(ai);
        var raised = 0;
        status.AvailabilityChanged += (_, _) => raised++;

        await status.RefreshAsync(TestContext.Current.CancellationToken);
        await status.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task RefreshAsync_QueriesSllPurpose()
    {
        var ai = new CapturingAi { Availability = AiAvailabilityResult.Ready };
        var status = new ConfiguredSllAccessStatus(ai);

        await status.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AiRequestPurpose.Sll, ai.LastPurpose);
    }

    private sealed class CapturingAi : IAiTextGenerationService
    {
        public AiAvailabilityResult Availability { get; set; } = AiAvailabilityResult.Ready;
        public AiRequestPurpose? LastPurpose { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            LastPurpose = purpose;
            return Task.FromResult(Availability);
        }

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiTextResult.Success("x", "model"));
    }
}
