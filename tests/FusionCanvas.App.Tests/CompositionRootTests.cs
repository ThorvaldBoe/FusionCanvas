namespace FusionCanvas.App.Tests;

public sealed class CompositionRootTests
{
    [Fact]
    public void MainWindowPresentationTypesDoNotConstructIntegrationAdapters()
    {
        var root = FindRepositoryRoot();
        var presentationFiles = new[]
        {
            Path.Combine(root, "src", "FusionCanvas.App", "Views", "MainWindow.axaml.cs"),
            Path.Combine(root, "src", "FusionCanvas.App", "Views", "MainWindowViewModel.cs")
        };

        foreach (var path in presentationFiles)
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("FusionCanvas.Integration", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CompositionFactoriesOwnTheConcreteWorkspaceAndCsvAdapters()
    {
        var root = FindRepositoryRoot();
        var workspaceFactory = File.ReadAllText(Path.Combine(
            root, "src", "FusionCanvas.App", "Workspace", "AppWorkspaceFactory.cs"));
        var servicesFactory = File.ReadAllText(Path.Combine(
            root, "src", "FusionCanvas.App", "AppServicesFactory.cs"));

        Assert.Contains("new LocalWorkspaceFileStore", workspaceFactory, StringComparison.Ordinal);
        Assert.Contains("new WorkspaceTransferService", workspaceFactory, StringComparison.Ordinal);
        Assert.Contains("new RasterImageMetadataReader", workspaceFactory, StringComparison.Ordinal);
        Assert.Contains("new FusionCanvas.Integration.Items.ItemCsvCodec", servicesFactory, StringComparison.Ordinal);
        Assert.Contains("new FusionCanvas.Integration.Items.Import.ItemCsvCodec", servicesFactory, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FusionCanvas.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
