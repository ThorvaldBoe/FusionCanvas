using System.Collections.Immutable;
using FusionCanvas.Application.AI;
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
        await File.WriteAllTextAsync(path, "{\"version\":5,\"darkMode\":true}", TestContext.Current.CancellationToken);
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
        var workspaceId = Guid.NewGuid();

        var saved = await store.SaveAsync(
            new ApplicationSettings(DarkMode: true, Ai: AiConfigurationSettings.Default, ActiveWorkspaceId: workspaceId),
            TestContext.Current.CancellationToken);
        var reloaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(saved.Saved);
        Assert.Null(saved.Warning);
        Assert.True(reloaded.Value.DarkMode);
        Assert.Equal(workspaceId, reloaded.Value.ActiveWorkspaceId);
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

    [Fact]
    public async Task Version2_RoundTripsCompleteAiSettingsWithoutASecret()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        var store = new JsonApplicationSettingsStore(path);
        var profile = AiProfileSettings.Empty with
        {
            ModelId = "provider/model",
            MaxCompletionTokens = 123,
            Temperature = 0.4,
            TopP = 0.8,
            TopK = 20,
            MinP = 0.1,
            TopA = 0.2,
            FrequencyPenalty = -0.3,
            PresencePenalty = 0.5,
            RepetitionPenalty = 1.1,
            Seed = 42,
            StopSequences = ["END"],
            Reasoning = new AiReasoningSettings(AiReasoningMode.Effort, "high")
        };
        var ai = new AiConfigurationSettings(
            false,
            true,
            profile,
            new AiPurposeProfileSettings(false, true, profile with { ModelId = "idea/model" }),
            new AiPurposeProfileSettings(true, true, profile with { ModelId = "retained/model" }),
            new AiPurposeProfileSettings(true, true, profile with { ModelId = "sll/model" }));

        Assert.True((await store.SaveAsync(
            new ApplicationSettings(true, ai),
            TestContext.Current.CancellationToken)).Saved);
        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);
        var json = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);

        Assert.False(loaded.Value.Ai.RequireZeroDataRetention);
        Assert.True(loaded.Value.Ai.AdvancedMode);
        Assert.Equal("provider/model", loaded.Value.Ai.General.ModelId);
        Assert.Equal(123, loaded.Value.Ai.General.MaxCompletionTokens);
        Assert.Equal("high", loaded.Value.Ai.General.Reasoning.Effort);
        Assert.Equal(["END"], loaded.Value.Ai.General.StopSequences);
        Assert.Equal("idea/model", loaded.Value.Ai.Ideation.CustomProfile.ModelId);
        Assert.Equal("retained/model", loaded.Value.Ai.Concept.CustomProfile.ModelId);
        Assert.Equal("sll/model", loaded.Value.Ai.Sll.CustomProfile.ModelId);
        Assert.Contains("\"version\": 4", json);
        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Version3_RoundTripsWindowLayout()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new JsonApplicationSettingsStore(tempDirectory.GetPath("settings.json"));
        var layout = new WindowLayoutSettings(120, -40, 1400, 900, 380);

        Assert.True((await store.SaveAsync(
            new ApplicationSettings(true, AiConfigurationSettings.Default, layout),
            TestContext.Current.CancellationToken)).Saved);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(layout, loaded.Value.WindowLayout);
        Assert.False(loaded.UsedDefault);
    }

    [Fact]
    public async Task LoadAsync_LegacyVersionDefaultsWindowLayout()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(path, "{\"version\":2,\"darkMode\":true}", TestContext.Current.CancellationToken);

        var loaded = await new JsonApplicationSettingsStore(path)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(loaded.Value.DarkMode);
        Assert.Null(loaded.Value.WindowLayout);
        Assert.False(loaded.UsedDefault);
    }

    [Fact]
    public async Task LoadAsync_InvalidWindowLayoutPreservesReadableSettings()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(
            path,
            "{\"version\":3,\"darkMode\":true,\"windowLayout\":{\"positionX\":10,\"positionY\":20,\"width\":\"NaN\",\"height\":800,\"navigationWidth\":320}}",
            TestContext.Current.CancellationToken);

        var loaded = await new JsonApplicationSettingsStore(path)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(loaded.Value.DarkMode);
        Assert.Null(loaded.Value.WindowLayout);
        Assert.False(loaded.UsedDefault);
        Assert.Contains("window layout", loaded.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_PreSllSettingsJsonDefaultsSllToInheritGeneral()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        var json = """
            {
              "version": 2,
              "darkMode": true,
              "ai": {
                "advancedMode": true
              }
            }
            """;
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);
        var store = new JsonApplicationSettingsStore(path);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.UsedDefault);
        Assert.True(result.Value.Ai.AdvancedMode);
        Assert.True(result.Value.Ai.Sll.UseGeneral);
    }

    [Fact]
    public async Task LoadAsync_MalformedAiSectionPreservesReadableAppearancePreference()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(
            path,
            "{\"version\":2,\"darkMode\":true,\"ai\":\"not-an-object\"}",
            TestContext.Current.CancellationToken);

        var result = await new JsonApplicationSettingsStore(path)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(result.UsedDefault);
        Assert.True(result.Value.DarkMode);
        Assert.Equal(AiConfigurationSettings.Default, result.Value.Ai);
        Assert.Contains("AI settings", result.Warning);
    }

    [Fact]
    public async Task Version4_RoundTripsWindowGeometry()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new JsonApplicationSettingsStore(tempDirectory.GetPath("settings.json"));
        var geometry = new Dictionary<string, WindowGeometrySettings>
        {
            ["settings"] = new(120, 80, 700, 520),
            ["storeEditor"] = new(-200, 40, 1000, 800)
        };

        Assert.True((await store.SaveAsync(
            new ApplicationSettings(
                true,
                AiConfigurationSettings.Default,
                WindowGeometry: geometry.ToImmutableDictionary()),
            TestContext.Current.CancellationToken)).Saved);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(loaded.UsedDefault);
        Assert.Equal(geometry["settings"], loaded.Value.WindowGeometry!["settings"]);
        Assert.Equal(geometry["storeEditor"], loaded.Value.WindowGeometry!["storeEditor"]);
    }

    [Fact]
    public async Task LoadAsync_LegacyVersionDefaultsWindowGeometry()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(
            path,
            "{\"version\":3,\"darkMode\":true,\"windowLayout\":{\"positionX\":10,\"positionY\":20,\"width\":1400,\"height\":900,\"navigationWidth\":320}}",
            TestContext.Current.CancellationToken);

        var loaded = await new JsonApplicationSettingsStore(path)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(loaded.Value.DarkMode);
        Assert.NotNull(loaded.Value.WindowLayout);
        Assert.Empty(loaded.Value.WindowGeometry!);
        Assert.False(loaded.UsedDefault);
        Assert.Null(loaded.Warning);
    }

    [Fact]
    public async Task LoadAsync_InvalidGeometryEntryDiscardsEntryPreservesOthers()
    {
        using var tempDirectory = new TemporaryDirectory();
        var path = tempDirectory.GetPath("settings.json");
        await File.WriteAllTextAsync(
            path,
            "{\"version\":4,\"darkMode\":true,\"windowGeometry\":{\"settings\":{\"positionX\":10,\"positionY\":20,\"width\":700,\"height\":520},\"bad\":{\"positionX\":1,\"positionY\":2,\"width\":\"NaN\",\"height\":400}}}",
            TestContext.Current.CancellationToken);

        var loaded = await new JsonApplicationSettingsStore(path)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(loaded.Value.DarkMode);
        Assert.Single(loaded.Value.WindowGeometry!);
        Assert.Equal(new WindowGeometrySettings(10, 20, 700, 520), loaded.Value.WindowGeometry!["settings"]);
        Assert.False(loaded.Value.WindowGeometry!.ContainsKey("bad"));
        Assert.False(loaded.UsedDefault);
        Assert.Contains("window geometry", loaded.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Version4_RoundTripsWindowLayoutAlongsideWindowGeometry()
    {
        using var tempDirectory = new TemporaryDirectory();
        var store = new JsonApplicationSettingsStore(tempDirectory.GetPath("settings.json"));
        var layout = new WindowLayoutSettings(120, -40, 1400, 900, 380);
        var geometry = ImmutableDictionary.CreateRange(new[]
        {
            KeyValuePair.Create("ideation", new WindowGeometrySettings(60, 90, 800, 600))
        });
        var settings = new ApplicationSettings(
            true,
            AiConfigurationSettings.Default,
            WindowLayout: layout,
            WindowGeometry: geometry);

        Assert.True((await store.SaveAsync(settings, TestContext.Current.CancellationToken)).Saved);

        var loaded = await store.LoadAsync(TestContext.Current.CancellationToken);
        var json = await File.ReadAllTextAsync(store.SettingsPath, TestContext.Current.CancellationToken);

        Assert.Equal(layout, loaded.Value.WindowLayout);
        Assert.Equal(new WindowGeometrySettings(60, 90, 800, 600), loaded.Value.WindowGeometry!["ideation"]);
        Assert.Contains("\"version\": 4", json);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string path) => Path.Combine(_directory.FullName, path);

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
