namespace FusionCanvas.Application.AI;

public sealed record AiConfigurationResolution(
    AiConfigurationAvailability Availability,
    AiProfileSettings? Profile,
    AiModelDescriptor? Model,
    IReadOnlyList<string> Errors)
{
    public bool IsReady => Availability == AiConfigurationAvailability.Ready;
}
