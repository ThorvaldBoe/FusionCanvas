using System.Collections.ObjectModel;
using System.Net;

namespace FusionCanvas.UITests.Infrastructure;

internal sealed record UiTestConfiguration(
    string ApplicationPath,
    Uri AutomationServerUri,
    string RepositoryRoot)
{
    internal const string ApplicationPathEnvironmentVariable = "FUSIONCANVAS_UI_APP_PATH";
    internal const string AutomationServerUrlEnvironmentVariable = "FUSIONCANVAS_UI_AUTOMATION_SERVER_URL";

    public static UiTestConfiguration Load()
    {
        var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
        var applicationPath = Environment.GetEnvironmentVariable(ApplicationPathEnvironmentVariable);
        applicationPath = string.IsNullOrWhiteSpace(applicationPath)
            ? Path.Combine(repositoryRoot, "src", "FusionCanvas.App", "bin", "Debug", "net10.0", "FusionCanvas.App.exe")
            : Path.GetFullPath(applicationPath);

        var serverUrl = Environment.GetEnvironmentVariable(AutomationServerUrlEnvironmentVariable);
        serverUrl = string.IsNullOrWhiteSpace(serverUrl) ? "http://127.0.0.1:4723" : serverUrl;

        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverUri) ||
            serverUri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(serverUri.Host))
        {
            throw new InvalidOperationException(
                $"{AutomationServerUrlEnvironmentVariable} must be an absolute HTTP(S) URL. Current value: '{serverUrl}'.");
        }

        return new UiTestConfiguration(Path.GetFullPath(applicationPath), serverUri, repositoryRoot);
    }

    public void ValidateForDesktopRun()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("FusionCanvas desktop UI automation currently runs on Windows only.");
        }

        if (!File.Exists(ApplicationPath))
        {
            throw new FileNotFoundException(
                $"The compiled FusionCanvas application was not found at '{ApplicationPath}'. Build FusionCanvas.App or set {ApplicationPathEnvironmentVariable}.",
                ApplicationPath);
        }
    }

    public static string FindRepositoryRoot(string startingDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startingDirectory)); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FusionCanvas.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate FusionCanvas.sln from the current directory.");
    }
}
