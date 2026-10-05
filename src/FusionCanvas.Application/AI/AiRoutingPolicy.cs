namespace FusionCanvas.Application.AI;

public sealed record AiRoutingPolicy(
    AiRoutingMode Mode,
    string? ProviderId = null,
    string? EndpointId = null)
{
    public static AiRoutingPolicy Automatic { get; } = new(AiRoutingMode.Automatic);

    public static AiRoutingPolicy ForProvider(string providerId) =>
        new(AiRoutingMode.SpecificProvider, Normalize(providerId), null);

    public static AiRoutingPolicy ForEndpoint(string endpointId, string? providerId = null) =>
        new(AiRoutingMode.ExactEndpoint, Normalize(providerId), Normalize(endpointId));

    public bool IsAutomatic => Mode == AiRoutingMode.Automatic;

    public string DisplayName => Mode switch
    {
        AiRoutingMode.Automatic => "Automatic",
        AiRoutingMode.SpecificProvider => "Specific provider",
        AiRoutingMode.ExactEndpoint => "Exact endpoint",
        _ => "Unknown routing"
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
