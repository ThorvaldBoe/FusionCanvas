namespace FusionCanvas.Application.AI;

public enum AiRoutingAvailability
{
    Ready,
    MissingProvider,
    MissingEndpoint,
    ProviderUnavailable,
    EndpointUnavailable,
    PrivacyIncompatible,
    Invalid
}

public sealed record AiRoutingResolution(
    AiRoutingAvailability Availability,
    AiRoutingPolicy Policy,
    AiModelEndpointDescriptor? Endpoint,
    IReadOnlyList<string> Errors)
{
    public bool IsReady => Availability == AiRoutingAvailability.Ready;

    public static AiRoutingResolution Automatic(AiRoutingPolicy? policy = null) =>
        new(AiRoutingAvailability.Ready, policy ?? AiRoutingPolicy.Automatic, null, []);
}
