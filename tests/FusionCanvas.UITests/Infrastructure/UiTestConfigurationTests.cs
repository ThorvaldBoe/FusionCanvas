namespace FusionCanvas.UITests.Infrastructure;

public sealed class UiTestConfigurationTests
{
    [Fact]
    public void FindRepositoryRoot_FindsSolutionFromNestedDirectory()
    {
        var root = CreateTemporaryRepository();
        try
        {
            var nested = Path.Combine(root, "tests", "FusionCanvas.UITests", "bin");
            Directory.CreateDirectory(nested);

            var result = UiTestConfiguration.FindRepositoryRoot(nested);

            Assert.Equal(root, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DisposableUiTestRoot_ContainsOnlyItsOwnedPathsAndBuildsLaunchArguments()
    {
        using var root = new DisposableUiTestRoot();

        Assert.True(root.Contains(root.DatabasePath));
        Assert.True(root.Contains(root.WorkspaceRootPath));
        Assert.True(root.Contains(root.SettingsPath));
        Assert.False(root.Contains(Path.GetTempPath()));

        var arguments = root.CreateApplicationArguments();
        Assert.Contains("--fusioncanvas-workspace-db", arguments);
        Assert.Contains(root.DatabasePath, arguments);
        Assert.Contains("--fusioncanvas-settings-path", arguments);
    }

    [Fact]
    public void EnsureAutomationServerAvailable_ReportsActionablePrerequisite()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => UiApplicationSession.EnsureAutomationServerAvailable(new Uri("http://127.0.0.1:1")));

        Assert.Contains("WinAppDriver", exception.Message);
        Assert.Contains("README.md", exception.Message);
    }

    private static string CreateTemporaryRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), "FusionCanvas.UITests.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "FusionCanvas.sln"), string.Empty);
        return root;
    }
}
