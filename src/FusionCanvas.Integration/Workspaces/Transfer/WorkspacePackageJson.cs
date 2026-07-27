using System.Text.Json;

namespace FusionCanvas.Integration.Packages;

internal static class WorkspacePackageJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
