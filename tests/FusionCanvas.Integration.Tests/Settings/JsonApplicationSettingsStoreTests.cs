using FusionCanvas.Application.Settings;
using FusionCanvas.Integration.Settings;

namespace FusionCanvas.Integration.Tests;

public class JsonApplicationSettingsStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFileReturnsDefaultWithNoWarning()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new JsonApplicationSettingsStore(tempDirectory.GetPath("settings.json"));

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.UsedDefault);
        Assert.False(result.Value.DarkMode);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_ValidLightPreferenceReturnsValue()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":1,\"darkMode\":false}", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.UsedDefault);
        Assert.False(result.Value.DarkMode);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_ValidDarkPreferenceReturnsValue()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":1,\"darkMode\":true}", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.UsedDefault);
        Assert.True(result.Value.DarkMode);
    }

    [Fact]
    public async Task LoadAsync_UnknownPropertiesAreIgnored()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(
            path,
            "{\"version\":1,\"darkMode\":true,\"futureField\":\"ignored\",\"palette\":42}",
            TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.UsedDefault);
        Assert.True(result.Value.DarkMode);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_InvalidJsonReturnsDefaultWithWarning()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{not json", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.UsedDefault);
        Assert.False(result.Value.DarkMode);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_WrongShapeReturnsDefaultWithWarning()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":1,\"darkMode\":\"yes\"}", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.UsedDefault);
        Assert.False(result.Value.DarkMode);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_UnsupportedVersionReturnsDefaultWithWarning()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":2,\"darkMode\":true}", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.UsedDefault);
        Assert.False(result.Value.DarkMode);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task SaveAsync_PersistsPreferenceAndReloadsIt()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        var store = new JsonApplicationSettingsStore(path);

        var saved = await store.SaveAsync(new ApplicationSettings(DarkMode: true), TestContext.Current.CancellationToken);
        var reloaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(saved.Saved);
        Assert.Null(saved.Warning);
        Assert.True(reloaded.Value.DarkMode);
        Assert.False(reloaded.UsedDefault);
    }

    [Fact]
    public async Task SaveAsync_CreatesParentDirectory()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath(Path.Combine("nested", "deep", "settings.json"));
        var store = new JsonApplicationSettingsStore(path);

        var saved = await store.SaveAsync(new ApplicationSettings(DarkMode: false), TestContext.Current.CancellationToken);

        Assert.True(saved.Saved);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingPreferenceAtomically()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        var store = new JsonApplicationSettingsStore(path);

        await store.SaveAsync(new ApplicationSettings(DarkMode: true), TestContext.Current.CancellationToken);
        await store.SaveAsync(new ApplicationSettings(DarkMode: false), TestContext.Current.CancellationToken);

        var reloaded = await store.LoadAsync(TestContext.Current.CancellationToken);
        Assert.False(reloaded.Value.DarkMode);

        var tempLeftover = path + ".tmp";
        Assert.False(File.Exists(tempLeftover), "Atomic write left a temporary sibling behind.");
    }

    [Fact]
    public async Task SaveAsync_WriteFailureReturnsFailedWithWarning()
    {
        using var tempDirectory = new TemporaryDirectory();
        var blockerPath = tempDirectory.GetPath("blocker");
        await File.WriteAllTextAsync(blockerPath, "blocks-directory-creation", TestContext.Current.CancellationToken);
        var settingsPath = Path.Combine(blockerPath, "settings.json");
        var store = new JsonApplicationSettingsStore(settingsPath);

        var result = await store.SaveAsync(new ApplicationSettings(DarkMode: true), TestContext.Current.CancellationToken);

        Assert.False(result.Saved);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public async Task LoadAsync_CancelledTokenPropagatesCancellation()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":1,\"darkMode\":true}", TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.LoadAsync(new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task SaveAsync_CancelledTokenPropagatesCancellation()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new JsonApplicationSettingsStore(tempDirectory.GetPath("settings.json"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.SaveAsync(new ApplicationSettings(DarkMode: true), new CancellationToken(canceled: true)));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string path) => Path.Combine(_directory.FullName, path);

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
