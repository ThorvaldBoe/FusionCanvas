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
    public async Task RecordAsync_AndCleanupExpiredAsync_UseInjectedClock()
    {
        var workspace = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context, () => now);
        await service.SaveSettingsAsync(workspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });

        await service.RecordAsync(Event("captured at the injected time"));

        store.Entries.Add(new TelemetryEntry(
            Guid.NewGuid(), workspace, now.AddDays(-1).AddTicks(-1), "Workspace", "Expired", "Information",
            "Succeeded", "expired", null, null, null, null, null, null));
        store.Entries.Add(new TelemetryEntry(
            Guid.NewGuid(), workspace, now.AddDays(-1), "Workspace", "Boundary", "Information",
            "Succeeded", "retained at the cutoff", null, null, null, null, null, null));

        await service.CleanupExpiredAsync(workspace);

        Assert.Contains(store.Entries, entry => entry.Message == "captured at the injected time" && entry.OccurredAt == now);
        Assert.DoesNotContain(store.Entries, entry => entry.Message == "expired");
        Assert.Contains(store.Entries, entry => entry.Message == "retained at the cutoff");
        Assert.Equal(now.AddDays(-1), store.LastExpirationCutoff);
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
    public async Task ReadReportWindowAsync_CapsNewestFirstAndReportsTruncation()
    {
        var workspace = Guid.NewGuid();
        var otherWorkspace = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context, () => now);
        store.Entries.Add(Entry(workspace, now.AddMinutes(-3), "old"));
        store.Entries.Add(Entry(workspace, now.AddMinutes(-2), "middle"));
        store.Entries.Add(Entry(workspace, now.AddMinutes(-1), "new"));
        store.Entries.Add(Entry(otherWorkspace, now.AddSeconds(-30), "other workspace"));

        var batch = await service.ReadReportWindowAsync(workspace, now.AddMinutes(-4), now, maxEntries: 2);

        Assert.True(batch.IsTruncated);
        Assert.Equal(["new", "middle"], batch.Entries.Select(entry => entry.Message));
    }

    [Fact]
    public async Task DeleteRangeAsync_DeletesOnlyWorkspaceAndHalfOpenTimeWindow()
    {
        var workspace = Guid.NewGuid();
        var otherWorkspace = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(10);
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context, () => end);
        store.Entries.Add(Entry(workspace, start.AddTicks(-1), "before"));
        store.Entries.Add(Entry(workspace, start, "start"));
        store.Entries.Add(Entry(workspace, end.AddTicks(-1), "inside"));
        store.Entries.Add(Entry(workspace, end, "end"));
        store.Entries.Add(Entry(otherWorkspace, start, "other workspace"));

        var deleted = await service.DeleteRangeAsync(workspace, start, end);

        Assert.Equal(2, deleted);
        Assert.Equal(["before", "end"], store.Entries.Where(entry => entry.WorkspaceId == workspace).Select(entry => entry.Message));
        Assert.Contains(store.Entries, entry => entry.WorkspaceId == otherWorkspace);
    }

    [Fact]
    public async Task Dispose_CancelsScheduledCleanup()
    {
        var workspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore { BlockExpirationCleanup = true };
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context);

        var cleanup = service.RunScheduledCleanupAsync();
        await store.ExpirationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        service.Dispose();

        await store.ExpirationCanceled.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cleanup;
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

    [Fact]
    public async Task ExportAsync_WritesSanitizedRecordsWithoutDeletingThem()
    {
        var workspace = Guid.NewGuid();
        var store = new MemoryTelemetryStore();
        var context = new TestWorkspaceContext(workspace);
        using var service = new WorkspaceTelemetryService(store, context);
        await service.SaveSettingsAsync(workspace, WorkspaceTelemetrySettings.Default with { DebugModeEnabled = true });
        await service.RecordAsync(Event("kept", "{\"api_key\":\"credential-value\",\"message\":\"useful\"}"));

        await using var stream = new MemoryStream();
        await service.ExportAsync(workspace, stream);

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("useful", json);
        Assert.DoesNotContain("credential-value", json);
        Assert.Single(store.Entries);
    }

    private static TelemetryEventRequest Event(string message, string? response = null) =>
        new("Workspace", "TestEvent", "Information", "Succeeded", message, ResponseBody: response);

    private static TelemetryEntry Entry(Guid workspace, DateTimeOffset occurredAt, string message) =>
        new(Guid.NewGuid(), workspace, occurredAt, "Workspace", "ReportTest", "Information", "Succeeded", message, null, null, null, null, null, null);

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
        public bool BlockExpirationCleanup { get; init; }
        public TaskCompletionSource<object?> ExpirationStarted { get; } = new();
        public TaskCompletionSource<object?> ExpirationCanceled { get; } = new();
        public DateTimeOffset? LastExpirationCutoff { get; private set; }

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
        public Task<IReadOnlyList<TelemetryEntry>> SearchAsync(Guid workspaceId, TelemetryQuery query, CancellationToken cancellationToken = default)
        {
            IEnumerable<TelemetryEntry> selected = Entries.Where(entry => entry.WorkspaceId == workspaceId);
            if (query.FromInclusive is { } from) selected = selected.Where(entry => entry.OccurredAt >= from);
            if (query.ToExclusive is { } to) selected = selected.Where(entry => entry.OccurredAt < to);
            if (query.Areas is { Count: > 0 }) selected = selected.Where(entry => query.Areas.Contains(entry.Area));
            return Task.FromResult<IReadOnlyList<TelemetryEntry>>(selected.OrderByDescending(entry => entry.OccurredAt).Skip(query.Offset).Take(query.PageSize).ToArray());
        }
        public Task<IReadOnlyList<TelemetryEntry>> ReadAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TelemetryEntry>>(Entries.Where(entry => entry.WorkspaceId == workspaceId).ToArray());
        public Task<int> DeleteAllAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var count = Entries.RemoveAll(entry => entry.WorkspaceId == workspaceId);
            return Task.FromResult(count);
        }
        public Task<int> DeleteRangeAsync(Guid workspaceId, DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken cancellationToken = default)
        {
            var count = Entries.RemoveAll(entry => entry.WorkspaceId == workspaceId && entry.OccurredAt >= fromInclusive && entry.OccurredAt < toExclusive);
            return Task.FromResult(count);
        }
        public Task<int> DeleteExpiredAsync(Guid workspaceId, DateTimeOffset cutoff, CancellationToken cancellationToken = default)
        {
            if (BlockExpirationCleanup) return WaitForExpirationCancellationAsync(cancellationToken);

            LastExpirationCutoff = cutoff;
            var count = Entries.RemoveAll(entry => entry.WorkspaceId == workspaceId && entry.OccurredAt < cutoff);
            return Task.FromResult(count);
        }

        private async Task<int> WaitForExpirationCancellationAsync(CancellationToken cancellationToken)
        {
            ExpirationStarted.TrySetResult(null);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ExpirationCanceled.TrySetResult(null);
                throw;
            }

            return 0;
        }
    }
}
