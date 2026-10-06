namespace FusionCanvas.Application.AI;

public sealed record AiGenerationReceipt(
    string? RequestedModel,
    AiRoutingMode RoutingMode,
    string? RequestedProvider,
    string? RequestedEndpoint,
    string? ActualModel,
    string? ActualProvider,
    string? ActualEndpoint,
    string? FinishReason,
    AiTextUsage? Usage,
    decimal? ReportedCost,
    string? GenerationId)
{
    public static AiGenerationReceipt Unavailable { get; } =
        new(null, AiRoutingMode.Automatic, null, null, null, null, null, null, null, null, null);

    public static AiGenerationReceipt ForRequest(
        string? requestedModel,
        AiRoutingPolicy? policy = null) =>
        new(
            requestedModel,
            policy?.Mode ?? AiRoutingMode.Automatic,
            policy?.ProviderId,
            policy?.EndpointId,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public string Summary
    {
        get
        {
            var route = RoutingMode switch
            {
                AiRoutingMode.Automatic => "Automatic (provider may vary)",
                AiRoutingMode.SpecificProvider => $"Provider: {RequestedProvider ?? "Unavailable"}",
                AiRoutingMode.ExactEndpoint => $"Endpoint: {RequestedEndpoint ?? "Unavailable"}",
                _ => "Routing unavailable"
            };
            var actual = ActualProvider is null
                ? "Actual provider: Unavailable"
                : $"Actual provider: {ActualProvider}";
            return $"Model: {RequestedModel ?? "Unavailable"}; {route}; {actual}; Generation: {GenerationId ?? "Unavailable"}";
        }
    }
}
