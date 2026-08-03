using System.Reflection;
using FusionCanvas.Application.Versioning;

namespace FusionCanvas.App.Versioning;

public sealed class AssemblyApplicationVersionProvider : IApplicationVersionProvider
{
    private readonly ApplicationVersionInfo _info;

    public AssemblyApplicationVersionProvider()
        : this(typeof(AssemblyApplicationVersionProvider).Assembly)
    {
    }

    public AssemblyApplicationVersionProvider(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _info = ApplicationVersionDiagnostics.TryParse(informational, out var info)
            ? info
            : ApplicationVersionInfo.Unknown;
    }

    public ApplicationVersionInfo GetVersion() => _info;
}
