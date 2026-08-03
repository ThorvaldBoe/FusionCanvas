using System.Runtime.InteropServices;

namespace FusionCanvas.Application.Versioning;

public static class ApplicationVersionDiagnostics
{
    public static bool TryParse(string? informationalVersion, out ApplicationVersionInfo info)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            info = ApplicationVersionInfo.Unknown;
            return false;
        }

        var informational = informationalVersion.Trim();
        var plusIndex = informational.IndexOf('+');
        if (plusIndex < 0)
        {
            info = new ApplicationVersionInfo(
                ProductVersion: informational,
                InformationalVersion: informational,
                CommitId: ApplicationVersionInfo.UnknownCommitId);
            return true;
        }

        var productVersion = informational[..plusIndex];
        var commitMetadata = informational[(plusIndex + 1)..];
        var commitId = ExtractCommitId(commitMetadata);

        info = new ApplicationVersionInfo(
            ProductVersion: productVersion,
            InformationalVersion: informational,
            CommitId: commitId);
        return true;
    }

    public static string Format(ApplicationVersionInfo info, string platform)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(platform);

        var commit = info.IsCommitKnown ? info.CommitId : ApplicationVersionInfo.UnknownCommitId;
        return $"Version: {info.ProductVersion}{Environment.NewLine}Commit: {commit}{Environment.NewLine}Platform: {platform}";
    }

    public static string BuildPlatformString()
    {
        var os = RuntimeInformation.OSDescription?.Trim();
        var architecture = RuntimeInformation.OSArchitecture;
        var osName = string.IsNullOrWhiteSpace(os) ? "Unknown" : NormalizeOsName(os);
        return $"{osName} {architecture}";
    }

    private static string ExtractCommitId(string commitMetadata)
    {
        if (string.IsNullOrWhiteSpace(commitMetadata))
        {
            return ApplicationVersionInfo.UnknownCommitId;
        }

        var trimmed = commitMetadata.Trim();
        if (trimmed.Length > 1 && trimmed[0] == 'g')
        {
            return trimmed[1..];
        }

        return trimmed;
    }

    private static string NormalizeOsName(string os)
    {
        var lower = os.ToLowerInvariant();
        if (lower.StartsWith("windows"))
        {
            return "Windows";
        }

        if (lower.StartsWith("darwin") || lower.StartsWith("macos") || lower.StartsWith("mac os"))
        {
            return "macOS";
        }

        if (lower.StartsWith("linux"))
        {
            return "Linux";
        }

        if (lower.StartsWith("freebsd"))
        {
            return "FreeBSD";
        }

        return os;
    }
}
