using FusionCanvas.App.Settings;
using FusionCanvas.App.Versioning;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Versioning;

namespace FusionCanvas.App.Tests.Settings;

public class SettingsAboutTests
{
    [Fact]
    public void About_Section_IsSelectableAndEnumerated()
    {
        var vm = NewViewModel();

        vm.SelectedSection = SettingsSection.About;

        Assert.True(vm.IsAboutSection);
        Assert.Contains(SettingsSection.About, vm.Sections);
    }

    [Fact]
    public void Version_ProviderValue_IsProjectedOntoTheViewModel()
    {
        var info = new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a");
        var vm = new SettingsViewModel(
            new InMemorySettingsStore(),
            new FakeThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            versionProvider: new ConstantVersionProvider(info),
            clipboard: new RecordingClipboard());

        Assert.Equal("0.1.42", vm.Version.ProductVersion);
        Assert.Equal("3f91c2a", vm.Version.CommitId);
        Assert.Contains("Version: 0.1.42", vm.DiagnosticsText);
        Assert.Contains("Commit: 3f91c2a", vm.DiagnosticsText);
    }

    [Fact]
    public void CopyDiagnostics_CopiesTheFormattedBlockToTheClipboard()
    {
        var info = new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a");
        var clipboard = new RecordingClipboard();
        var vm = new SettingsViewModel(
            new InMemorySettingsStore(),
            new FakeThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            versionProvider: new ConstantVersionProvider(info),
            clipboard: clipboard);

        vm.CopyDiagnosticsCommand.Execute(null);
        clipboard.Flush();

        Assert.Single(clipboard.Copied);
        Assert.Equal(vm.DiagnosticsText, clipboard.Copied[0]);
        Assert.Contains("Version: 0.1.42", clipboard.Copied[0]);
        Assert.Contains("Commit: 3f91c2a", clipboard.Copied[0]);
        Assert.Contains("Platform:", clipboard.Copied[0]);
    }

    [Fact]
    public void CopyDiagnostics_ReportsUnknownCommitWhenProviderHasNoCommit()
    {
        var clipboard = new RecordingClipboard();
        var vm = new SettingsViewModel(
            new InMemorySettingsStore(),
            new FakeThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            versionProvider: new ConstantVersionProvider(ApplicationVersionInfo.Unknown),
            clipboard: clipboard);

        vm.CopyDiagnosticsCommand.Execute(null);
        clipboard.Flush();

        Assert.Contains("Commit: unknown", clipboard.Copied[0]);
    }

    private static SettingsViewModel NewViewModel() =>
        new(
            new InMemorySettingsStore(),
            new FakeThemeController(),
            ApplicationSettings.Default,
            loadWarning: null,
            versionProvider: new ConstantVersionProvider(
                new ApplicationVersionInfo("0.1.42", "0.1.42+g3f91c2a", "3f91c2a")),
            clipboard: new RecordingClipboard());

    private sealed class ConstantVersionProvider(ApplicationVersionInfo info) : IApplicationVersionProvider
    {
        public ApplicationVersionInfo GetVersion() => info;
    }

    private sealed class RecordingClipboard : IClipboardService
    {
        private readonly List<string> _copied = new();
        private TaskCompletionSource<object?> _completion = new();

        public IReadOnlyList<string> Copied => _copied;

        public Task SetTextAsync(string text)
        {
            _copied.Add(text);
            _completion.TrySetResult(null);
            return Task.CompletedTask;
        }

        public void Flush()
        {
            if (_copied.Count == 0)
            {
                _completion.Task.Wait(TimeSpan.FromSeconds(2));
            }
        }
    }

    private sealed class FakeThemeController : IApplicationThemeController
    {
        public void ApplyDarkMode(bool darkMode) { }
    }

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
            => Task.FromResult(ApplicationSettingsSaveResult.Success);
    }
}
