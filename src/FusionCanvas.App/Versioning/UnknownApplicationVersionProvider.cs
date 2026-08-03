using FusionCanvas.Application.Versioning;

namespace FusionCanvas.App.Versioning;

public sealed class UnknownApplicationVersionProvider : IApplicationVersionProvider
{
    public static UnknownApplicationVersionProvider Instance { get; } = new();

    public ApplicationVersionInfo GetVersion() => ApplicationVersionInfo.Unknown;
}
