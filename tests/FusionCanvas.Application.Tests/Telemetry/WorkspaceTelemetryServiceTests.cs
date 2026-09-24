using FusionCanvas.Application.Telemetry;

namespace FusionCanvas.Application.Tests.Telemetry;

public sealed class WorkspaceTelemetryServiceTests
{
    [Fact]
    public async Task RecordAsync_WhenCaptureIsDisabled_DoesNotWrite()
    {
        var workspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context);
        await service.GetSettingsAsync(workspace);

        await service.RecordAsync(Event("disabled"));

        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task RecordAsync_RedactsSecretsAndUsesActiveWorkspace()
    {
        var firstWorkspace = Guid.NewGuid();
        var secondWorkspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(firstWorkspace);
        using var service = new WorkspaceTelemetryService(store, context);
        await service.SaveSettingsAsync(firstWorkspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        await service.SaveSettingsAsync(secondWorkspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });

        context.SetActiveWorkspace(secondWorkspace);
        await service.GetSettingsAsync(secondWorkspace);
        await service.RecordAsync(Event("useful text api_key=abc123&next=yes", "{\"token\":\"secret-value\",\"message\":\"still useful\"}"));

        var entry = Assert.Single(store.Entries);
        Assert.Equal(secondWorkspace, entry.WorkspaceId);
        Assert.Contains("useful text", entry.Message);
        Assert.DoesNotContain("abc123", entry.Message);
        Assert.Contains("&next=yes", entry.Message);
        Assert.DoesNotContain("secret-value", entry.ResponseBody);
        Assert.Contains("still useful", entry.ResponseBody);
    }

    [Fact]
    public async Task RecordAsync_WhenStoreFails_DoesNotPropagateTelemetryFailure()
    {
        var workspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore { FailWrites = true };
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context);
        await service.SaveSettingsAsync(workspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });

        var exception = await Record.ExceptionAsync(() => service.RecordAsync(Event("operation completed")));

        Assert.Null(exception);
    }

    [Fact]
    public void SecretRedactor_SanitizesStringValuesInArraysAndJsonScalars()
    {
        var array = TelemetrySecretRedactor.Sanitize(Event("safe", "[\"Bearer token-value\",\"api_key=other-value&mode=fast\"]"));
        var scalar = TelemetrySecretRedactor.Sanitize(Event("safe", "\"Bearer token-value\""));

        Assert.DoesNotContain("token-value", array.ResponseBody);
        Assert.DoesNotContain("other-value", array.ResponseBody);
        Assert.Contains("mode=fast", array.ResponseBody);
        Assert.DoesNotContain("token-value", scalar.ResponseBody);
    }

    [Fact]
    public async Task ExportJsonAsync_ExportsSanitizedRecordsWithoutDeletingThem()
    {
        var workspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context);
        await service.SaveSettingsAsync(workspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        await service.RecordAsync(Event("kept", "{\"api_key\":\"credential-value\",\"message\":\"useful\"}"));

        var json = await service.ExportJsonAsync(workspace);

        Assert.Contains("useful", json);
        Assert.DoesNotContain("credential-value", json);
        Assert.Single(store.Entries);
    }

    private static TelemetryEventRequest Event(string message, string? response = null) =>
        new("Workspace", "TestEvent", "Information", "Succeeded", message, ResponseBody: response);

    private sealed class TestWorkspaceContext(Guid? activeWorkspaceId) : ITelemetryWorkspaceContext
    {
        public Guid? ActiveWorkspaceId { get; private set; } = activeWorkspaceId;
        public void SetActiveWorkspace(Guid? workspaceId) => ActiveWorkspaceId = workspaceId;
    }

    private sealed class MemoryTelemetryStore : ITelemetryStore
    {
        private readonly Dictionary<Guid, WorkspaceTelemetrySettings> _settings = [];
        public List<TelemetryEntry> Entries { get; } = [];
        public bool FailWrites { get; init; }

        public Task<WorkspaceTelemetrySettings> ReadSettingsAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_settings.GetValueOrDefault(workspaceId, WorkspaceTelemetrySettings.Default));
        public Task SaveSettingsAsync(Guid workspaceId, WorkspaceTelemetrySettings settings, CancellationToken cancellationToken = default)
        {
            _settings[workspaceId] = settings;
            return Task.CompletedTask;
        }
        public Task AddAsync(TelemetryEntry entry, CancellationToken cancellationToken = default)
        {
            if (FailWrites) throw new IOException("test store failure");
            Entries.Add(entry);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>(Entries.Where(entry => entry.WorkspaceId == workspaceId).ToArray());
        public Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>(Entries.Where(entry => entry.WorkspaceId == workspaceId).ToArray());
        public Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var count = Entries.RemoveAll(entry => entry.WorkspaceId == workspaceId);
            return Task.FromResult(count);
        }
        public Task<int> DeleteExpiredAsync(Guid workspaceId, DateTimeOffset cutoff, CancellationToken cancellationToken = default)
        {
            var count = Entries.RemoveAll(entry => entry.WorkspaceId == workspaceId && entry.OccurredAt < cutoff);
            return Task.FromResult(count);
        }
    }
}
