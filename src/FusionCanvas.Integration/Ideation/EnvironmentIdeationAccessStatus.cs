using FusionCanvas.Application.Ideation;

namespace FusionCanvas.Integration.Ideation;

public sealed class EnvironmentIdeationAccessStatus : IIdeationAccessStatus
{
    public const string EnvironmentVariableName = "FUSIONCANVAS_AI_API_KEY";
    private readonly Func<string?> _readValue;

    public EnvironmentIdeationAccessStatus()
        : this(() => Environment.GetEnvironmentVariable(EnvironmentVariableName))
    {
    }

    public EnvironmentIdeationAccessStatus(Func<string?> readValue)
    {
        _readValue = readValue ?? throw new ArgumentNullException(nameof(readValue));
    }

    public IdeationAccessAvailability GetAvailability() =>
        string.IsNullOrWhiteSpace(_readValue())
            ? IdeationAccessAvailability.Unavailable(
                $"Set {EnvironmentVariableName} to enable the local placeholder generator.")
            : IdeationAccessAvailability.Available;
}
