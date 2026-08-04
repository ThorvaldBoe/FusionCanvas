using Avalonia;

namespace FusionCanvas.App;

public static class Program
{
    private const string WorkspaceDatabaseArgument = "--fusioncanvas-workspace-db";
    private const string WorkspaceRootArgument = "--fusioncanvas-workspace-root";
    private const string SettingsPathArgument = "--fusioncanvas-settings-path";
    private const string UiTestModeArgument = "--fusioncanvas-ui-test";
    internal const string UiTestModeEnvironmentVariable = "FUSIONCANVAS_UI_TEST_MODE";

    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(ConfigureRuntimePaths(args));
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    internal static string[] ConfigureRuntimePaths(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var avaloniaArguments = new List<string>(args.Length);
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == UiTestModeArgument)
            {
                Environment.SetEnvironmentVariable(UiTestModeEnvironmentVariable, "1");
                continue;
            }

            if (!TryGetRuntimePathEnvironmentVariable(argument, out var environmentVariable))
            {
                avaloniaArguments.Add(argument);
                continue;
            }

            if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            {
                throw new ArgumentException($"{argument} requires a non-empty path value.", nameof(args));
            }

            Environment.SetEnvironmentVariable(environmentVariable, Path.GetFullPath(args[index]));
        }

        return [.. avaloniaArguments];
    }

    private static bool TryGetRuntimePathEnvironmentVariable(string argument, out string environmentVariable)
    {
        environmentVariable = argument switch
        {
            WorkspaceDatabaseArgument => Workspace.AppWorkspaceFactory.WorkspaceDatabaseEnvironmentVariable,
            WorkspaceRootArgument => Workspace.AppWorkspaceFactory.WorkspaceRootEnvironmentVariable,
            SettingsPathArgument => Settings.AppSettingsFactory.SettingsPathEnvironmentVariable,
            _ => string.Empty
        };

        return environmentVariable.Length > 0;
    }
}
